using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MHGR.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Seo;
using Nop.Services.Vendors;

namespace Nop.Web.Controllers;

/// <summary>
/// Read-only vendor (seller) storefront API for the buyer app: browse sellers and open a seller's storefront.
/// A vendor's products are served by GET /api/products?vendorId= (route api/vendors/{id}/products). Returns shared
/// MHGR.Api.Contracts DTOs. [Mahjar mobile-api]
/// </summary>
public partial class VendorsApiController : Controller
{
    #region Fields

    protected readonly ICustomerService _customerService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IPictureService _pictureService;
    protected readonly IProductService _productService;
    protected readonly IStoreContext _storeContext;
    protected readonly IUrlRecordService _urlRecordService;
    protected readonly IVendorService _vendorService;

    #endregion

    #region Ctor

    public VendorsApiController(ICustomerService customerService,
        ILocalizationService localizationService,
        IPictureService pictureService,
        IProductService productService,
        IStoreContext storeContext,
        IUrlRecordService urlRecordService,
        IVendorService vendorService)
    {
        _customerService = customerService;
        _localizationService = localizationService;
        _pictureService = pictureService;
        _productService = productService;
        _storeContext = storeContext;
        _urlRecordService = urlRecordService;
        _vendorService = vendorService;
    }

    #endregion

    #region Methods

    // GET /api/vendors?page=&pageSize=
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<VendorDto>), 200)]
    public virtual async Task<IActionResult> List(int page = 0, int pageSize = 20)
    {
        var vendors = await _vendorService.GetAllVendorsAsync(pageIndex: page, pageSize: Math.Clamp(pageSize, 1, 100));

        var items = new List<VendorDto>();
        foreach (var v in vendors)
            items.Add(new VendorDto
            {
                Id = v.Id,
                Name = await _localizationService.GetLocalizedAsync(v, x => x.Name),
                SeName = await _urlRecordService.GetSeNameAsync(v),
                ImageUrl = await _pictureService.GetPictureUrlAsync(v.PictureId)
            });

        return Json(new PagedResultDto<VendorDto>
        {
            Page = vendors.PageIndex,
            PageSize = vendors.PageSize,
            TotalItems = vendors.TotalCount,
            TotalPages = vendors.TotalPages,
            Items = items
        });
    }

    // GET /api/vendors/{id}
    [HttpGet]
    [ProducesResponseType(typeof(VendorDto), 200)]
    public virtual async Task<IActionResult> Detail(int id)
    {
        var vendor = await _vendorService.GetVendorByIdAsync(id);
        if (vendor == null || vendor.Deleted || !vendor.Active)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.vendornotfound") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var products = await _productService.SearchProductsAsync(
            pageIndex: 0, pageSize: 1, vendorId: vendor.Id, storeId: store.Id, visibleIndividuallyOnly: true);

        return Json(new VendorDto
        {
            Id = vendor.Id,
            Name = await _localizationService.GetLocalizedAsync(vendor, x => x.Name),
            Description = await _localizationService.GetLocalizedAsync(vendor, x => x.Description),
            SeName = await _urlRecordService.GetSeNameAsync(vendor),
            ImageUrl = await _pictureService.GetPictureUrlAsync(vendor.PictureId),
            ProductCount = products.TotalCount
        });
    }

    // GET /api/vendors/{id}/reviews?page=&pageSize=
    // "Vendor reviews" = the seller's product reviews aggregated (no separate vendor-review entity). [Mahjar mobile-api]
    [HttpGet]
    [ProducesResponseType(typeof(ReviewsResultDto), 200)]
    public virtual async Task<IActionResult> Reviews(int id, int page = 0, int pageSize = 20)
    {
        var vendor = await _vendorService.GetVendorByIdAsync(id);
        if (vendor == null || vendor.Deleted)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.vendornotfound") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var reviews = await _productService.GetAllProductReviewsAsync(approved: true, storeId: store.Id,
            vendorId: id, pageIndex: page, pageSize: Math.Clamp(pageSize, 1, 100));

        var items = new List<ReviewDto>();
        foreach (var r in reviews)
        {
            var c = await _customerService.GetCustomerByIdAsync(r.CustomerId);
            var name = c == null || string.IsNullOrWhiteSpace(c.FirstName) ? await _localizationService.GetResourceAsync("mahjar.api.common.customerfallback") : $"{c.FirstName} {c.LastName}".Trim();
            var product = await _productService.GetProductByIdAsync(r.ProductId);
            items.Add(new ReviewDto
            {
                Id = r.Id,
                Title = r.Title,
                ReviewText = r.ReviewText,
                Rating = r.Rating,
                CustomerName = name,
                CreatedOn = r.CreatedOnUtc.ToString("yyyy-MM-dd"),
                HelpfulYes = r.HelpfulYesTotal,
                HelpfulNo = r.HelpfulNoTotal,
                ProductId = r.ProductId,
                ProductName = product == null ? null : await _localizationService.GetLocalizedAsync(product, x => x.Name)
            });
        }

        return Json(new ReviewsResultDto
        {
            AverageRating = items.Count > 0 ? Math.Round(items.Average(x => x.Rating), 1) : 0,
            TotalReviews = reviews.TotalCount,
            Page = reviews.PageIndex,
            PageSize = reviews.PageSize,
            TotalItems = reviews.TotalCount,
            TotalPages = reviews.TotalPages,
            Items = items
        });
    }

    #endregion
}
