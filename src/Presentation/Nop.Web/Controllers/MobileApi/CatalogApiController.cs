using System;
using System.Collections.Generic;
using System.Globalization; //[Mahjar item-status] invariant "yyyy-MM-dd" (an ar thread culture would emit Arabic-Indic digits)
using System.Linq;
using System.Threading.Tasks;
using MHGR.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Mahjar; //[Mahjar item-status]
using Nop.Core.Domain.Media; //[Mahjar item-status] thumb size for the condition-photo strip
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Mahjar.ItemStatus; //[Mahjar item-status]
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Seo;
using Nop.Services.Vendors;
using Nop.Web.Factories; //[Mahjar app-home]

namespace Nop.Web.Controllers;

/// <summary>
/// Read-only catalog REST API for mobile / headless clients. Returns JSON (shared MHGR.Api.Contracts DTOs). No auth
/// needed for the public catalog. Covers discovery (list/search/filter/sort, brands, featured), product detail
/// (gallery, rating, stock, rental deposit + availability calendar, condition report [Mahjar item-status]) and
/// per-vendor browsing. [Mahjar mobile-api]
/// </summary>
public partial class CatalogApiController : Controller
{
    #region Fields

    protected readonly ItemStatusSettings _itemStatusSettings; //[Mahjar item-status]
    protected readonly MediaSettings _mediaSettings; //[Mahjar item-status]
    protected readonly ICategoryService _categoryService;
    protected readonly IHomeCategoryModelFactory _homeCategoryModelFactory; //[Mahjar app-home]
    protected readonly IItemStatusService _itemStatusService; //[Mahjar item-status]
    protected readonly ILocalizationService _localizationService;
    protected readonly IManufacturerService _manufacturerService;
    protected readonly IPictureService _pictureService;
    protected readonly IProductAttributeService _productAttributeService;
    protected readonly IProductService _productService;
    protected readonly IRentalAvailabilityService _rentalAvailabilityService;
    protected readonly IStoreContext _storeContext;
    protected readonly IUrlRecordService _urlRecordService;
    protected readonly IVendorService _vendorService;
    protected readonly IWebHelper _webHelper; //[Mahjar item-status] absolute url for an uploaded video, off the request host
    protected readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public CatalogApiController(ItemStatusSettings itemStatusSettings, //[Mahjar item-status]
        MediaSettings mediaSettings, //[Mahjar item-status]
        ICategoryService categoryService,
        IHomeCategoryModelFactory homeCategoryModelFactory, //[Mahjar app-home]
        IItemStatusService itemStatusService, //[Mahjar item-status]
        ILocalizationService localizationService,
        IManufacturerService manufacturerService,
        IPictureService pictureService,
        IProductAttributeService productAttributeService,
        IProductService productService,
        IRentalAvailabilityService rentalAvailabilityService,
        IStoreContext storeContext,
        IUrlRecordService urlRecordService,
        IVendorService vendorService,
        IWebHelper webHelper, //[Mahjar item-status]
        IWorkContext workContext)
    {
        _itemStatusSettings = itemStatusSettings; //[Mahjar item-status]
        _mediaSettings = mediaSettings; //[Mahjar item-status]
        _categoryService = categoryService;
        _homeCategoryModelFactory = homeCategoryModelFactory; //[Mahjar app-home]
        _itemStatusService = itemStatusService; //[Mahjar item-status]
        _localizationService = localizationService;
        _manufacturerService = manufacturerService;
        _pictureService = pictureService;
        _productAttributeService = productAttributeService;
        _productService = productService;
        _rentalAvailabilityService = rentalAvailabilityService;
        _storeContext = storeContext;
        _urlRecordService = urlRecordService;
        _vendorService = vendorService;
        _webHelper = webHelper; //[Mahjar item-status]
        _workContext = workContext;
    }

    #endregion

    #region Utilities

    protected static ProductSortingEnum ParseSort(string orderBy)
    {
        return (orderBy ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "price-asc" or "priceasc" or "price" => ProductSortingEnum.PriceAsc,
            "price-desc" or "pricedesc" => ProductSortingEnum.PriceDesc,
            "name-asc" or "nameasc" or "name" => ProductSortingEnum.NameAsc,
            "name-desc" or "namedesc" => ProductSortingEnum.NameDesc,
            "newest" or "created" or "createdon" => ProductSortingEnum.CreatedOn,
            _ => ProductSortingEnum.Position
        };
    }

    protected virtual async Task<string> GetCurrencyCodeAsync()
    {
        return (await _workContext.GetWorkingCurrencyAsync()).CurrencyCode;
    }

    protected virtual async Task<string> GetImageUrlAsync(Product product)
    {
        var pictures = await _productService.GetProductPicturesByProductIdAsync(product.Id);
        var first = pictures.FirstOrDefault();
        return await _pictureService.GetPictureUrlAsync(first?.PictureId ?? 0);
    }

    /// <summary>
    /// [Mahjar item-status] Builds the condition report ("حالة القطعة") carried by the product DETAIL payload:
    /// the overall grade, the summary, the real photos of this exact piece, one recorded video and the notes.
    /// Returns null — and the app hides its whole section — when the feature is off, the product has no report,
    /// the partner hid it, or the report is empty. Every label is localized here (Accept-Language is already
    /// resolved for /api requests) so the app renders it verbatim.
    /// </summary>
    /// <param name="product">Product</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the condition report, or null when there is nothing to show
    /// </returns>
    protected virtual async Task<ItemStatusDto> PrepareItemStatusAsync(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (!_itemStatusSettings.Enabled)
            return null;

        var itemStatus = await _itemStatusService.GetItemStatusByProductIdAsync(product.Id);
        if (itemStatus == null || !itemStatus.ShowOnProductPage)
            return null;

        var dto = new ItemStatusDto
        {
            GradeId = itemStatus.ConditionGradeId,
            //left null (rather than the localized "غير محدد") so the app can just hide the grade chip
            GradeText = itemStatus.ConditionGrade == ItemConditionGrade.NotSpecified
                ? null
                : await _localizationService.GetLocalizedEnumAsync(itemStatus.ConditionGrade),
            Summary = string.IsNullOrWhiteSpace(itemStatus.Summary) ? null : itemStatus.Summary,
            InspectedOn = itemStatus.InspectedOnUtc?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Photos = new List<ItemStatusPhotoDto>(),
            Notes = new List<ItemStatusNoteDto>()
        };

        //video: an UPLOADED file always wins over an external url. The uploaded path is site-relative, so make it
        //absolute off the REQUEST host (never Store.Url) — that is what makes the Android emulator (10.0.2.2),
        //the LAN hosts and prod all resolve. MAUI cannot play a relative uri.
        if (!string.IsNullOrWhiteSpace(itemStatus.VideoFile))
        {
            dto.VideoUrl = _webHelper.GetStoreLocation().TrimEnd('/') + itemStatus.VideoFile;
            dto.VideoIsFile = true;
        }
        else if (ItemStatusVideoUrlHelper.IsAllowed(itemStatus.VideoUrl))
        {
            //defence in depth, same rule as the admin write path and the storefront: only an absolute http(s)
            //url is handed to the app, so a row poisoned directly in the database cannot reach a WebView
            dto.VideoUrl = itemStatus.VideoUrl.Trim();
            dto.VideoIsFile = false;
        }

        //real photos of this exact piece; picture urls are already absolute (mediasettings.useabsoluteimagepath)
        var pictures = await _itemStatusService.GetItemStatusPicturesByProductIdAsync(product.Id);
        foreach (var picture in pictures)
            dto.Photos.Add(new ItemStatusPhotoDto
            {
                Url = await _pictureService.GetPictureUrlAsync(picture.PictureId),
                ThumbUrl = await _pictureService.GetPictureUrlAsync(picture.PictureId,
                    _mediaSettings.ProductThumbPictureSizeOnProductDetailsPage),
                Caption = string.IsNullOrWhiteSpace(picture.Caption) ? null : picture.Caption
            });

        var notes = await _itemStatusService.GetItemStatusNotesByProductIdAsync(product.Id);
        foreach (var note in notes)
            dto.Notes.Add(new ItemStatusNoteDto
            {
                SeverityId = note.SeverityId,
                SeverityText = await _localizationService.GetLocalizedEnumAsync(note.Severity),
                TypeText = await _localizationService.GetLocalizedEnumAsync(note.NoteType),
                Text = note.Text
            });

        //a saved-but-empty report is not worth a section. NOTE: the inspection date counts as content, exactly as
        //it does in the storefront's HasAny and in the admin create-guard — all three have to agree
        if (dto.GradeText == null && dto.Summary == null && dto.VideoUrl == null && dto.InspectedOn == null &&
            dto.Photos.Count == 0 && dto.Notes.Count == 0)
            return null;

        return dto;
    }

    protected virtual async Task<ProductDto> MapProductAsync(Product product, bool detail)
    {
        string vendorName = null;
        if (product.VendorId > 0)
            vendorName = (await _vendorService.GetVendorByIdAsync(product.VendorId))?.Name;

        var dto = new ProductDto
        {
            Id = product.Id,
            Name = await _localizationService.GetLocalizedAsync(product, x => x.Name),
            Sku = product.Sku,
            SeName = await _urlRecordService.GetSeNameAsync(product),
            ImageUrl = await GetImageUrlAsync(product),
            Currency = await GetCurrencyCodeAsync(),
            //buy/rent model [Mahjar]
            AvailableForPurchase = product.AvailableForPurchase,
            Price = product.Price,
            IsRental = product.IsRental,
            RentalPrice = product.IsRental ? (product.RentalPrice > decimal.Zero ? product.RentalPrice : product.Price) : (decimal?)null,
            RentalPricePeriod = product.IsRental ? product.RentalPricePeriod.ToString() : null,
            RentalPriceLength = product.IsRental ? product.RentalPriceLength : (int?)null,
            //refundable rental deposit (per unit) [Mahjar rental-deposit]
            RentalSecurityDeposit = product.IsRental && product.RentalSecurityDeposit > decimal.Zero ? product.RentalSecurityDeposit : (decimal?)null,
            //rating summary
            AverageRating = product.ApprovedTotalReviews > 0 ? Math.Round((double)product.ApprovedRatingSum / product.ApprovedTotalReviews, 1) : 0,
            ReviewCount = product.ApprovedTotalReviews,
            VendorId = product.VendorId > 0 ? product.VendorId : (int?)null,
            VendorName = vendorName
        };

        if (!detail)
            return dto;

        dto.ShortDescription = await _localizationService.GetLocalizedAsync(product, x => x.ShortDescription);
        dto.FullDescription = await _localizationService.GetLocalizedAsync(product, x => x.FullDescription);

        //stock
        dto.StockQuantity = product.StockQuantity;
        dto.InStock = product.ManageInventoryMethodId == 0 || product.StockQuantity > 0; //0 = DontManageStock

        //full image gallery
        var pictures = await _productService.GetProductPicturesByProductIdAsync(product.Id);
        var images = new List<string>();
        foreach (var pp in pictures)
            images.Add(await _pictureService.GetPictureUrlAsync(pp.PictureId));
        dto.Images = images;

        //condition report: photos/video/notes of THIS exact piece; null when there is nothing to show [Mahjar item-status]
        dto.ItemStatus = await PrepareItemStatusAsync(product);

        //rental availability: dates that are fully booked (for the calendar) [Mahjar]
        if (product.IsRental)
        {
            var from = DateTime.UtcNow.Date;
            var ranges = await _rentalAvailabilityService.GetUnavailableRangesAsync(product, from, from.AddMonths(12));
            dto.RentalUnavailableRanges = ranges
                .Select(r => new DateRangeDto { From = r.Start.ToString("yyyy-MM-dd"), To = r.End.ToString("yyyy-MM-dd") })
                .ToList();

            //[Mahjar rental-occupancy] rented out or booked: it cannot be bought before this day has passed
            if (product.AvailableForPurchase)
                dto.PurchaseBlockedUntil = (await _rentalAvailabilityService.GetPurchaseBlockedUntilAsync(product))?
                    .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        }

        return dto;
    }

    protected virtual async Task<IActionResult> ListProductsAsync(int? categoryId, int? vendorId, int? manufacturerId,
        string keyword, decimal? minPrice, decimal? maxPrice, string orderBy, int page, int pageSize)
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var categoryIds = categoryId is > 0 ? new List<int> { categoryId.Value } : null;
        var manufacturerIds = manufacturerId is > 0 ? new List<int> { manufacturerId.Value } : null;

        // [Mahjar mobile-api] Product names are English in the base column and Arabic as LOCALIZED
        // values (ar-EG). SearchProductsAsync only searches localized names when a languageId is
        // supplied (searchLocalizedValue). Without it, Arabic keyword search returned nothing.
        // Pass the working language (ar-first store ⇒ ar) so Arabic search matches, like the storefront.
        var workingLanguage = await _workContext.GetWorkingLanguageAsync();

        var products = await _productService.SearchProductsAsync(
            pageIndex: page,
            pageSize: Math.Clamp(pageSize, 1, 100),
            categoryIds: categoryIds,
            manufacturerIds: manufacturerIds,
            storeId: store.Id,
            vendorId: vendorId is > 0 ? vendorId.Value : 0,
            keywords: string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim(),
            languageId: workingLanguage.Id,
            priceMin: minPrice,
            priceMax: maxPrice,
            orderBy: ParseSort(orderBy),
            visibleIndividuallyOnly: true);

        var items = new List<ProductDto>();
        foreach (var product in products)
            items.Add(await MapProductAsync(product, detail: false));

        return Json(new PagedResultDto<ProductDto>
        {
            Page = products.PageIndex,
            PageSize = products.PageSize,
            TotalItems = products.TotalCount,
            TotalPages = products.TotalPages,
            Items = items
        });
    }

    #endregion

    #region Methods

    // GET /api/products?categoryId=&vendorId=&manufacturerId=&keyword=&minPrice=&maxPrice=&orderBy=&page=&pageSize=
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ProductDto>), 200)]
    public virtual Task<IActionResult> Products(int? categoryId, int? vendorId, int? manufacturerId,
        string keyword, decimal? minPrice, decimal? maxPrice, string orderBy, int page = 0, int pageSize = 20)
    {
        return ListProductsAsync(categoryId, vendorId, manufacturerId, keyword, minPrice, maxPrice, orderBy, page, pageSize);
    }

    // GET /api/search?q=&page=&pageSize=
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ProductDto>), 200)]
    public virtual Task<IActionResult> Search(string q, int page = 0, int pageSize = 20)
    {
        return ListProductsAsync(null, null, null, q, null, null, null, page, pageSize);
    }

    // GET /api/products/featured
    [HttpGet]
    [ProducesResponseType(typeof(ListResultDto<ProductDto>), 200)]
    public virtual async Task<IActionResult> Featured()
    {
        var products = await _productService.GetAllProductsDisplayedOnHomepageAsync();
        var items = new List<ProductDto>();
        foreach (var product in products.Where(p => p.Published && !p.Deleted && p.VisibleIndividually))
            items.Add(await MapProductAsync(product, detail: false));

        return Json(new ListResultDto<ProductDto> { Items = items });
    }

    // GET /api/products/{id}
    [HttpGet]
    [ProducesResponseType(typeof(ProductDto), 200)]
    public virtual async Task<IActionResult> Product(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null || product.Deleted || !product.Published)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.productnotfound") });

        return Json(await MapProductAsync(product, detail: true));
    }

    // GET /api/products/{id}/attributes   (selectable options to choose before add-to-cart) [Mahjar mobile-api]
    [HttpGet]
    [ProducesResponseType(typeof(ListResultDto<ProductAttributeDto>), 200)]
    public virtual async Task<IActionResult> Attributes(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null || product.Deleted || !product.Published)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.productnotfound") });

        var mappings = await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(id);
        var items = new List<ProductAttributeDto>();
        foreach (var m in mappings)
        {
            var attribute = await _productAttributeService.GetProductAttributeByIdAsync(m.ProductAttributeId);
            var dto = new ProductAttributeDto
            {
                Id = m.Id,
                Name = attribute == null ? null : await _localizationService.GetLocalizedAsync(attribute, x => x.Name),
                TextPrompt = await _localizationService.GetLocalizedAsync(m, x => x.TextPrompt),
                IsRequired = m.IsRequired,
                ControlType = m.AttributeControlTypeId
            };
            foreach (var v in await _productAttributeService.GetProductAttributeValuesAsync(m.Id))
                dto.Values.Add(new ProductAttributeValueDto
                {
                    Id = v.Id,
                    Name = await _localizationService.GetLocalizedAsync(v, x => x.Name),
                    PriceAdjustment = v.PriceAdjustment,
                    PriceAdjustmentUsePercentage = v.PriceAdjustmentUsePercentage,
                    IsPreSelected = v.IsPreSelected,
                    ColorSquaresRgb = v.ColorSquaresRgb
                });
            items.Add(dto);
        }

        return Json(new ListResultDto<ProductAttributeDto> { Items = items });
    }

    // GET /api/categories
    [HttpGet]
    [ProducesResponseType(typeof(ListResultDto<CategoryDto>), 200)]
    public virtual async Task<IActionResult> Categories()
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var categories = await _categoryService.GetAllCategoriesAsync(store.Id);

        var items = new List<CategoryDto>();
        foreach (var category in categories)
        {
            //[Mahjar app-home] the Browse category banner: the same watch as the home card + the website's count
            var (imageUrl, productCount) = await _homeCategoryModelFactory.PrepareAppCategoryPictureAndCountAsync(category);
            items.Add(new CategoryDto
            {
                Id = category.Id,
                ParentId = category.ParentCategoryId,
                Name = await _localizationService.GetLocalizedAsync(category, x => x.Name),
                SeName = await _urlRecordService.GetSeNameAsync(category),
                ImageUrl = imageUrl, //[Mahjar app-home]
                ProductCount = productCount //[Mahjar app-home]
            });
        }

        return Json(new ListResultDto<CategoryDto> { Items = items });
    }

    // GET /api/manufacturers   (brands)
    [HttpGet]
    [ProducesResponseType(typeof(ListResultDto<ManufacturerDto>), 200)]
    public virtual async Task<IActionResult> Manufacturers()
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var manufacturers = await _manufacturerService.GetAllManufacturersAsync(storeId: store.Id);

        var items = new List<ManufacturerDto>();
        foreach (var m in manufacturers)
            items.Add(new ManufacturerDto
            {
                Id = m.Id,
                Name = await _localizationService.GetLocalizedAsync(m, x => x.Name),
                SeName = await _urlRecordService.GetSeNameAsync(m)
            });

        return Json(new ListResultDto<ManufacturerDto> { Items = items });
    }

    #endregion
}
