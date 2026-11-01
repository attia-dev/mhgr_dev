// [Mahjar home-motion]
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Mahjar;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Mahjar.HomeSlides;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Web.Factories;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Mvc.Routing;
using Nop.Web.Models.Catalog;

namespace Nop.Web.Components;

/// <summary>
/// The home "Own or Rent" editorial band: two large tiles, each staging a REAL home-page watch — one that can be
/// bought, one that can be rented — with its true price; the tile leads to that watch, and the category listing it
/// belongs to is offered as a secondary link (no fake filter).
/// Watches already staged by the Hero slides are avoided when another candidate exists, so the page does not
/// repeat itself. Renders nothing when neither tile has a candidate. [Mahjar home-motion]
/// </summary>
public partial class HomeOwnRentViewComponent : NopViewComponent
{
    #region Constants

    /// <summary>Longest side (px) of the watch render staged on a tile plate</summary>
    protected const int TILE_PICTURE_SIZE = 700;

    #endregion

    #region Fields

    protected readonly IAclService _aclService;
    protected readonly ICategoryService _categoryService;
    protected readonly IHomeSlideService _homeSlideService;
    protected readonly ILocalizationService _localizationService;
    protected readonly INopUrlHelper _nopUrlHelper;
    protected readonly IPriceFormatter _priceFormatter;
    protected readonly IProductModelFactory _productModelFactory;
    protected readonly IProductService _productService;
    protected readonly IStoreMappingService _storeMappingService;
    protected readonly IUrlRecordService _urlRecordService;

    #endregion

    #region Ctor

    public HomeOwnRentViewComponent(IAclService aclService,
        ICategoryService categoryService,
        IHomeSlideService homeSlideService,
        ILocalizationService localizationService,
        INopUrlHelper nopUrlHelper,
        IPriceFormatter priceFormatter,
        IProductModelFactory productModelFactory,
        IProductService productService,
        IStoreMappingService storeMappingService,
        IUrlRecordService urlRecordService)
    {
        _aclService = aclService;
        _categoryService = categoryService;
        _homeSlideService = homeSlideService;
        _localizationService = localizationService;
        _nopUrlHelper = nopUrlHelper;
        _priceFormatter = priceFormatter;
        _productModelFactory = productModelFactory;
        _productService = productService;
        _storeMappingService = storeMappingService;
        _urlRecordService = urlRecordService;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// The rent tile's rate, said simply: "120 SAR / day" when the product rents by the single day (the usual case),
    /// else the stock "… per N day(s) / week(s)" string of the product box
    /// </summary>
    protected virtual async Task<string> PrepareRentalRateTextAsync(Product product, ProductPriceModel price)
    {
        if (price == null)
            return null;

        if (product.RentalPricePeriod == RentalPricePeriod.Days && product.RentalPriceLength == 1 && price.RentalPriceValue.HasValue)
        {
            return string.Format(await _localizationService.GetResourceAsync("Mahjar.Home.OwnRent.PerDay"),
                await _priceFormatter.FormatPriceAsync(price.RentalPriceValue.Value));
        }

        return string.IsNullOrEmpty(price.RentalPrice) ? price.Price : price.RentalPrice;
    }

    /// <summary>Builds a tile for the product: its overview (name, picture, price) and its first visible category</summary>
    protected virtual async Task<HomeOwnRentModel.TileModel> PrepareTileAsync(Product product, bool rent)
    {
        if (product == null)
            return null;

        var overview = (await _productModelFactory.PrepareProductOverviewModelsAsync(new[] { product },
            preparePriceModel: true, preparePictureModel: true, productThumbPictureSize: TILE_PICTURE_SIZE)).FirstOrDefault();
        if (overview == null)
            return null;

        var tile = new HomeOwnRentModel.TileModel
        {
            ProductName = overview.Name,
            //[Mahjar home-slides] the Rent tile ("Rent the …") opens the watch with Rent already chosen (?mode=rent)
            ProductUrl = rent
                ? await _nopUrlHelper.RouteGenericUrlAsync<Product>(new RouteValueDictionary
                {
                    ["SeName"] = overview.SeName,
                    [NopHomeSlideDefaults.RentModeQueryName] = NopHomeSlideDefaults.RentModeTargetValue
                })
                : await _nopUrlHelper.RouteGenericUrlAsync<Product>(new { SeName = overview.SeName }),
            PictureUrl = overview.PictureModels.FirstOrDefault()?.ImageUrl,
            PriceText = rent
                ? await PrepareRentalRateTextAsync(product, overview.ProductPrice)
                : overview.ProductPrice?.Price,
            HasDeposit = rent && product.RentalSecurityDeposit > decimal.Zero
        };

        //the listing the tile leads to: the product's first visible category
        foreach (var productCategory in await _categoryService.GetProductCategoriesByProductIdAsync(product.Id))
        {
            var category = await _categoryService.GetCategoryByIdAsync(productCategory.CategoryId);
            if (category == null || category.Deleted || !category.Published)
                continue;

            tile.CategoryName = await _localizationService.GetLocalizedAsync(category, x => x.Name);
            tile.CategoryUrl = await _nopUrlHelper.RouteGenericUrlAsync<Category>(new { SeName = await _urlRecordService.GetSeNameAsync(category) });
            break;
        }

        return tile;
    }

    #endregion

    #region Methods

    public async Task<IViewComponentResult> InvokeAsync()
    {
        //the same candidates as the featured products (ACL, store, availability, visible individually)
        var products = await (await _productService.GetAllProductsDisplayedOnHomepageAsync())
            .WhereAwait(async p => await _aclService.AuthorizeAsync(p) && await _storeMappingService.AuthorizeAsync(p))
            .Where(p => _productService.ProductIsAvailable(p))
            .Where(p => p.VisibleIndividually)
            .ToListAsync();

        if (!products.Any())
            return Content(string.Empty);

        //watches the Hero already stages (by product target) go last
        var heroSlides = await _homeSlideService.GetActiveHomeSlidesAsync(HomeSlidePlacement.Hero, HomeSlideChannel.Web);
        var heroProductIds = heroSlides
            .Where(s => s.TargetType == HomeSlideTargetType.Product)
            .Select(s => s.TargetId)
            .ToHashSet();
        var ordered = products.OrderBy(p => heroProductIds.Contains(p.Id) ? 1 : 0).ToList();

        //Own: a buy-only watch first (it reads unambiguously as "to own"), else any purchasable one
        var own = ordered.FirstOrDefault(p => p.AvailableForPurchase && !p.IsRental)
            ?? ordered.FirstOrDefault(p => p.AvailableForPurchase);

        //Rent: a rentable watch other than the Own one — one carrying a refundable deposit first
        var rentable = ordered.Where(p => p.IsRental && p.Id != own?.Id).ToList();
        var rent = rentable.FirstOrDefault(p => p.RentalSecurityDeposit > decimal.Zero && !heroProductIds.Contains(p.Id))
            ?? rentable.FirstOrDefault();

        var model = new HomeOwnRentModel
        {
            Own = await PrepareTileAsync(own, rent: false),
            Rent = await PrepareTileAsync(rent, rent: true)
        };

        if (model.Own == null && model.Rent == null)
            return Content(string.Empty);

        return View(model);
    }

    #endregion
}
