using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MHGR.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Services.Authentication;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Media;

namespace Nop.Web.Controllers;

/// <summary>
/// Back-in-stock notifications API (bearer-token authenticated): subscribe to / unsubscribe from a product and
/// list the buyer's active subscriptions. Returns shared MHGR.Api.Contracts DTOs. [Mahjar mobile-api]
/// </summary>
[IgnoreAntiforgeryToken]
public partial class BackInStockApiController : Controller
{
    #region Fields

    protected readonly IApiTokenService _apiTokenService;
    protected readonly IBackInStockSubscriptionService _backInStockSubscriptionService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IPictureService _pictureService;
    protected readonly IProductService _productService;
    protected readonly IStoreContext _storeContext;

    #endregion

    #region Ctor

    public BackInStockApiController(IApiTokenService apiTokenService,
        IBackInStockSubscriptionService backInStockSubscriptionService,
        ILocalizationService localizationService,
        IPictureService pictureService,
        IProductService productService,
        IStoreContext storeContext)
    {
        _apiTokenService = apiTokenService;
        _backInStockSubscriptionService = backInStockSubscriptionService;
        _localizationService = localizationService;
        _pictureService = pictureService;
        _productService = productService;
        _storeContext = storeContext;
    }

    #endregion

    #region Utilities

    protected virtual async Task<Customer> GetTokenCustomerAsync()
    {
        return await _apiTokenService.GetAuthenticatedCustomerAsync(Request.Headers["Authorization"].ToString());
    }

    #endregion

    #region Methods

    // POST /api/products/{id}/back-in-stock   (subscribe)
    [HttpPost]
    [ProducesResponseType(typeof(MessageResponseDto), 200)]
    public virtual async Task<IActionResult> Subscribe(int id)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var product = await _productService.GetProductByIdAsync(id);
        if (product == null || product.Deleted || !product.Published)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.productnotfound") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var existing = await _backInStockSubscriptionService.FindSubscriptionAsync(customer.Id, product.Id, store.Id);
        if (existing == null)
            await _backInStockSubscriptionService.InsertSubscriptionAsync(new BackInStockSubscription
            {
                CustomerId = customer.Id,
                ProductId = product.Id,
                StoreId = store.Id,
                CreatedOnUtc = DateTime.UtcNow
            });

        return Json(new MessageResponseDto { Success = true, Message = await _localizationService.GetResourceAsync("mahjar.api.backinstock.subscribed") });
    }

    // DELETE /api/products/{id}/back-in-stock   (unsubscribe)
    [HttpDelete]
    [ProducesResponseType(typeof(MessageResponseDto), 200)]
    public virtual async Task<IActionResult> Unsubscribe(int id)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var existing = await _backInStockSubscriptionService.FindSubscriptionAsync(customer.Id, id, store.Id);
        if (existing != null)
            await _backInStockSubscriptionService.DeleteSubscriptionAsync(existing);

        return Json(new MessageResponseDto { Success = true, Message = await _localizationService.GetResourceAsync("mahjar.api.backinstock.unsubscribed") });
    }

    // GET /api/back-in-stock?page=&pageSize=
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<BackInStockSubscriptionDto>), 200)]
    public virtual async Task<IActionResult> List(int page = 0, int pageSize = 20)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var subscriptions = await _backInStockSubscriptionService.GetAllSubscriptionsByCustomerIdAsync(
            customer.Id, store.Id, page, Math.Clamp(pageSize, 1, 100));

        var items = new List<BackInStockSubscriptionDto>();
        foreach (var s in subscriptions)
        {
            var product = await _productService.GetProductByIdAsync(s.ProductId);
            if (product == null || product.Deleted)
                continue;
            var pictureId = (await _productService.GetProductPicturesByProductIdAsync(product.Id)).FirstOrDefault()?.PictureId ?? 0;
            items.Add(new BackInStockSubscriptionDto
            {
                Id = s.Id,
                ProductId = s.ProductId,
                ProductName = await _localizationService.GetLocalizedAsync(product, x => x.Name),
                ImageUrl = await _pictureService.GetPictureUrlAsync(pictureId)
            });
        }

        return Json(new PagedResultDto<BackInStockSubscriptionDto>
        {
            Page = subscriptions.PageIndex,
            PageSize = subscriptions.PageSize,
            TotalItems = subscriptions.TotalCount,
            TotalPages = subscriptions.TotalPages,
            Items = items
        });
    }

    #endregion
}
