using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MHGR.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Authentication;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Orders;

namespace Nop.Web.Controllers;

/// <summary>
/// Mobile wishlist API (bearer-token authenticated). Save watches for later (buy or rent), then move them to the
/// cart. Reuses the cart engine with ShoppingCartType.Wishlist. Returns shared MHGR.Api.Contracts DTOs. [Mahjar mobile-api]
/// </summary>
[IgnoreAntiforgeryToken]
public partial class WishlistApiController : Controller
{
    #region Fields

    protected readonly IApiTokenService _apiTokenService;
    protected readonly ICustomerService _customerService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IPictureService _pictureService;
    protected readonly IPriceFormatter _priceFormatter;
    protected readonly IProductAttributeFormatter _productAttributeFormatter;
    protected readonly IProductAttributeParser _productAttributeParser;
    protected readonly IProductAttributeService _productAttributeService;
    protected readonly IProductService _productService;
    protected readonly IShoppingCartService _shoppingCartService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public WishlistApiController(IApiTokenService apiTokenService,
        ICustomerService customerService,
        ILocalizationService localizationService,
        IPictureService pictureService,
        IPriceFormatter priceFormatter,
        IProductAttributeFormatter productAttributeFormatter,
        IProductAttributeParser productAttributeParser,
        IProductAttributeService productAttributeService,
        IProductService productService,
        IShoppingCartService shoppingCartService,
        IStoreContext storeContext,
        IWorkContext workContext)
    {
        _apiTokenService = apiTokenService;
        _customerService = customerService;
        _localizationService = localizationService;
        _pictureService = pictureService;
        _priceFormatter = priceFormatter;
        _productAttributeFormatter = productAttributeFormatter;
        _productAttributeParser = productAttributeParser;
        _productAttributeService = productAttributeService;
        _productService = productService;
        _shoppingCartService = shoppingCartService;
        _storeContext = storeContext;
        _workContext = workContext;
    }

    #endregion

    #region Utilities

    protected virtual async Task<Customer> GetTokenCustomerAsync()
    {
        return await _apiTokenService.GetAuthenticatedCustomerAsync(Request.Headers["Authorization"].ToString());
    }

    protected virtual async Task<string> GetCurrencyCodeAsync()
    {
        return (await _workContext.GetWorkingCurrencyAsync()).CurrencyCode;
    }

    protected virtual async Task<WishlistDto> BuildWishlistAsync(Customer customer)
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var wishlist = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.Wishlist, store.Id);

        var items = new List<CartItemDto>();
        foreach (var item in wishlist)
        {
            var product = await _productService.GetProductByIdAsync(item.ProductId);
            var imageUrl = product == null ? null
                : await _pictureService.GetPictureUrlAsync(
                    (await _productService.GetProductPicturesByProductIdAsync(product.Id)).FirstOrDefault()?.PictureId ?? 0);
            var (lineSubTotal, _, _, _) = await _shoppingCartService.GetSubTotalAsync(item, true);
            var attributes = product != null && !string.IsNullOrEmpty(item.AttributesXml)
                ? await _productAttributeFormatter.FormatAttributesAsync(product, item.AttributesXml, customer, store,
                    separator: "، ", htmlEncode: false, renderPrices: false)
                : null;
            items.Add(new CartItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                Name = product == null ? null : await _localizationService.GetLocalizedAsync(product, x => x.Name),
                ImageUrl = imageUrl,
                Quantity = item.Quantity,
                IsRental = item.RentalStartDateUtc.HasValue,
                RentalStartDate = item.RentalStartDateUtc?.ToString("yyyy-MM-dd"),
                RentalEndDate = item.RentalEndDateUtc?.ToString("yyyy-MM-dd"),
                LineSubTotal = lineSubTotal,
                LineSubTotalFormatted = await _priceFormatter.FormatPriceAsync(lineSubTotal),
                Attributes = attributes
            });
        }

        return new WishlistDto
        {
            Items = items,
            TotalItems = wishlist.Sum(x => x.Quantity),
            Currency = await GetCurrencyCodeAsync()
        };
    }

    #endregion

    #region Methods

    // GET /api/wishlist
    [HttpGet]
    [ProducesResponseType(typeof(WishlistDto), 200)]
    public virtual async Task<IActionResult> Get()
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        return Json(await BuildWishlistAsync(customer));
    }

    // POST /api/wishlist/add   { productId, quantity, rentalStartDate?, rentalEndDate? }
    [HttpPost]
    [ProducesResponseType(typeof(WishlistDto), 200)]
    public virtual async Task<IActionResult> Add([FromBody] AddToCartApiRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });
        if (request == null || request.ProductId <= 0)
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.productrequired") });

        var product = await _productService.GetProductByIdAsync(request.ProductId);
        if (product == null || product.Deleted || !product.Published)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.productnotfound") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var attributesXml = await ApiAttributeHelper.BuildAttributesXmlAsync(
            _productAttributeService, _productAttributeParser, product, request.Attributes);
        var warnings = await _shoppingCartService.AddToCartAsync(customer, product, ShoppingCartType.Wishlist, store.Id,
            attributesXml: attributesXml,
            rentalStartDate: request.RentalStartDate, rentalEndDate: request.RentalEndDate,
            quantity: Math.Max(request.Quantity, 1));

        if (warnings.Any())
            return BadRequest(new { success = false, warnings });

        return Json(await BuildWishlistAsync(customer));
    }

    // DELETE /api/wishlist/{itemId}
    [HttpDelete]
    [ProducesResponseType(typeof(WishlistDto), 200)]
    public virtual async Task<IActionResult> Remove(int itemId)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var wishlist = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.Wishlist, store.Id);
        var item = wishlist.FirstOrDefault(x => x.Id == itemId);
        if (item == null)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.wishlist.itemnotfound") });

        await _shoppingCartService.DeleteShoppingCartItemAsync(item);
        return Json(await BuildWishlistAsync(customer));
    }

    // POST /api/wishlist/move-to-cart   { itemId }
    [HttpPost]
    [ProducesResponseType(typeof(WishlistDto), 200)]
    public virtual async Task<IActionResult> MoveToCart([FromBody] MoveToCartApiRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });
        if (request == null || request.ItemId <= 0)
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidrequest") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var wishlist = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.Wishlist, store.Id);
        var item = wishlist.FirstOrDefault(x => x.Id == request.ItemId);
        if (item == null)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.wishlist.itemnotfound") });

        var product = await _productService.GetProductByIdAsync(item.ProductId);
        if (product == null)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.productnotfound") });

        var warnings = await _shoppingCartService.AddToCartAsync(customer, product, ShoppingCartType.ShoppingCart, store.Id,
            attributesXml: item.AttributesXml,
            rentalStartDate: item.RentalStartDateUtc, rentalEndDate: item.RentalEndDateUtc, quantity: item.Quantity);

        if (warnings.Any())
            return BadRequest(new { success = false, warnings });

        await _shoppingCartService.DeleteShoppingCartItemAsync(item);
        return Json(await BuildWishlistAsync(customer));
    }

    #endregion
}
