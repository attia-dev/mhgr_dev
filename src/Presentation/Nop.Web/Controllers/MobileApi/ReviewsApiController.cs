using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MHGR.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Services.Authentication;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Localization;

namespace Nop.Web.Controllers;

/// <summary>
/// Product reviews API for the mobile app: list approved reviews (with the product's aggregate rating) and submit a
/// review. New reviews follow the store's approval setting. Returns shared MHGR.Api.Contracts DTOs. [Mahjar mobile-api]
/// </summary>
[IgnoreAntiforgeryToken]
public partial class ReviewsApiController : Controller
{
    #region Fields

    protected readonly CatalogSettings _catalogSettings;
    protected readonly IApiTokenService _apiTokenService;
    protected readonly ICustomerService _customerService;
    protected readonly ILocalizationService _localizationService; //[Mahjar i18n]
    protected readonly IProductService _productService;
    protected readonly IStoreContext _storeContext;

    #endregion

    #region Ctor

    public ReviewsApiController(CatalogSettings catalogSettings,
        IApiTokenService apiTokenService,
        ICustomerService customerService,
        ILocalizationService localizationService,
        IProductService productService,
        IStoreContext storeContext)
    {
        _catalogSettings = catalogSettings;
        _apiTokenService = apiTokenService;
        _customerService = customerService;
        _localizationService = localizationService;
        _productService = productService;
        _storeContext = storeContext;
    }

    #endregion

    #region Utilities

    protected virtual async Task<Nop.Core.Domain.Customers.Customer> GetTokenCustomerAsync()
    {
        return await _apiTokenService.GetAuthenticatedCustomerAsync(Request.Headers["Authorization"].ToString());
    }

    #endregion

    #region Methods

    // GET /api/products/{id}/reviews?page=&pageSize=
    [HttpGet]
    [ProducesResponseType(typeof(ReviewsResultDto), 200)]
    public virtual async Task<IActionResult> List(int id, int page = 0, int pageSize = 20)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null || product.Deleted || !product.Published)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.productnotfound") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var reviews = await _productService.GetAllProductReviewsAsync(productId: id, approved: true,
            storeId: store.Id, pageIndex: page, pageSize: Math.Clamp(pageSize, 1, 100));

        var customerFallback = await _localizationService.GetResourceAsync("mahjar.api.common.customerfallback");
        var items = new List<ReviewDto>();
        foreach (var r in reviews)
        {
            var c = await _customerService.GetCustomerByIdAsync(r.CustomerId);
            var name = c == null ? customerFallback : (string.IsNullOrWhiteSpace(c.FirstName) ? customerFallback : $"{c.FirstName} {c.LastName}".Trim());
            items.Add(new ReviewDto
            {
                Id = r.Id,
                Title = r.Title,
                ReviewText = r.ReviewText,
                Rating = r.Rating,
                CustomerName = name,
                CreatedOn = r.CreatedOnUtc.ToString("yyyy-MM-dd"),
                HelpfulYes = r.HelpfulYesTotal,
                HelpfulNo = r.HelpfulNoTotal
            });
        }

        return Json(new ReviewsResultDto
        {
            AverageRating = product.ApprovedTotalReviews > 0 ? Math.Round((double)product.ApprovedRatingSum / product.ApprovedTotalReviews, 1) : 0,
            TotalReviews = product.ApprovedTotalReviews,
            Page = reviews.PageIndex,
            PageSize = reviews.PageSize,
            TotalItems = reviews.TotalCount,
            TotalPages = reviews.TotalPages,
            Items = items
        });
    }

    // POST /api/products/{id}/reviews   { title, reviewText, rating }
    [HttpPost]
    [ProducesResponseType(typeof(StatusResultDto), 200)]
    public virtual async Task<IActionResult> Add(int id, [FromBody] ProductReviewApiRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });
        if (request == null || request.Rating < 1 || request.Rating > 5)
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.reviews.ratingrequired") });
        if (string.IsNullOrWhiteSpace(request.ReviewText))
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.reviews.textrequired") });

        var product = await _productService.GetProductByIdAsync(id);
        if (product == null || product.Deleted || !product.Published)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.productnotfound") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var isApproved = !_catalogSettings.ProductReviewsMustBeApproved;
        var review = new ProductReview
        {
            CustomerId = customer.Id,
            ProductId = product.Id,
            StoreId = store.Id,
            Title = request.Title,
            ReviewText = request.ReviewText,
            Rating = request.Rating,
            HelpfulYesTotal = 0,
            HelpfulNoTotal = 0,
            IsApproved = isApproved,
            CreatedOnUtc = DateTime.UtcNow
        };
        await _productService.InsertProductReviewAsync(review);

        return Json(new StatusResultDto { Success = true, Status = isApproved ? "approved" : "pendingApproval" });
    }

    #endregion
}
