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
/// Mobile/headless cart API. Bearer-token authenticated; the buy/rent + no-double-booking rules run automatically
/// (shared cart validation), so the API enforces the same business rules as the website. Returns the shared
/// MHGR.Api.Contracts CartDto (every mutating call returns the updated cart). [Mahjar mobile-api]
/// </summary>
[IgnoreAntiforgeryToken]
public partial class CartApiController : Controller
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

    public CartApiController(IApiTokenService apiTokenService,
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

    protected virtual async Task<CartDto> BuildCartAsync(Customer customer)
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

        var items = new List<CartItemDto>();
        var subTotal = decimal.Zero;
        foreach (var item in cart)
        {
            var product = await _productService.GetProductByIdAsync(item.ProductId);
            var imageUrl = product == null ? null
                : await _pictureService.GetPictureUrlAsync(
                    (await _productService.GetProductPicturesByProductIdAsync(product.Id)).FirstOrDefault()?.PictureId ?? 0);
            var (lineSubTotal, _, _, _) = await _shoppingCartService.GetSubTotalAsync(item, true);
            subTotal += lineSubTotal;
            var attributes = product != null && !string.IsNullOrEmpty(item.AttributesXml)
                ? await _productAttributeFormatter.FormatAttributesAsync(product, item.AttributesXml, customer, store,
                    separator: "، ", htmlEncode: false, renderPrices: false)
                : null;
            //[Mahjar rental-occupancy] the checkout validation of this line, as the web cart shows it — e.g. the item was
            //rented out after it was added, or its rental dates got booked meanwhile
            var warnings = product == null
                ? new List<string>()
                : (await _shoppingCartService.GetShoppingCartItemWarningsAsync(customer, ShoppingCartType.ShoppingCart, product,
                    store.Id, item.AttributesXml, item.CustomerEnteredPrice, item.RentalStartDateUtc, item.RentalEndDateUtc,
                    item.Quantity, false, item.Id)).ToList();
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
                Attributes = attributes,
                Warnings = warnings
            });
        }

        return new CartDto
        {
            Items = items,
            TotalItems = cart.Sum(x => x.Quantity),
            SubTotal = subTotal,
            SubTotalFormatted = await _priceFormatter.FormatPriceAsync(subTotal),
            Currency = await GetCurrencyCodeAsync()
        };
    }

    #endregion

    #region Methods

    // GET /api/cart
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), 200)]
    public virtual async Task<IActionResult> Get()
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        return Json(await BuildCartAsync(customer));
    }

    // POST /api/cart/add   { "productId": 3, "quantity": 1, "rentalStartDate": "2026-06-20", "rentalEndDate": "2026-06-23" }
    [HttpPost]
    [ProducesResponseType(typeof(CartDto), 200)]
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
        var warnings = await _shoppingCartService.AddToCartAsync(customer, product, ShoppingCartType.ShoppingCart, store.Id,
            attributesXml: attributesXml,
            rentalStartDate: request.RentalStartDate, rentalEndDate: request.RentalEndDate,
            quantity: Math.Max(request.Quantity, 1));

        if (warnings.Any())
            return BadRequest(new { success = false, warnings });

        return Json(await BuildCartAsync(customer));
    }

    // DELETE /api/cart/{itemId}
    [HttpDelete]
    [ProducesResponseType(typeof(CartDto), 200)]
    public virtual async Task<IActionResult> Remove(int itemId)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        var item = cart.FirstOrDefault(x => x.Id == itemId);
        if (item == null)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.cart.itemnotfound") });

        await _shoppingCartService.DeleteShoppingCartItemAsync(item);
        return Json(await BuildCartAsync(customer));
    }

    // PUT /api/cart/{itemId}   { quantity, rentalStartDate?, rentalEndDate? }
    [HttpPut]
    [ProducesResponseType(typeof(CartDto), 200)]
    public virtual async Task<IActionResult> Update(int itemId, [FromBody] UpdateCartApiRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });
        if (request == null)
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidrequest") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        var item = cart.FirstOrDefault(x => x.Id == itemId);
        if (item == null)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.cart.itemnotfound") });

        // rebuild attributes only when the client sends a new selection; otherwise keep the existing XML
        var attributesXml = request.Attributes is { Count: > 0 }
            ? await ApiAttributeHelper.BuildAttributesXmlAsync(_productAttributeService, _productAttributeParser,
                await _productService.GetProductByIdAsync(item.ProductId), request.Attributes)
            : item.AttributesXml;

        var warnings = await _shoppingCartService.UpdateShoppingCartItemAsync(customer, item.Id,
            attributesXml, item.CustomerEnteredPrice,
            rentalStartDate: request.RentalStartDate ?? item.RentalStartDateUtc,
            rentalEndDate: request.RentalEndDate ?? item.RentalEndDateUtc,
            quantity: Math.Max(request.Quantity, 1));

        if (warnings.Any())
            return BadRequest(new { success = false, warnings });

        return Json(await BuildCartAsync(customer));
    }

    // DELETE /api/cart   (empty the whole cart)
    [HttpDelete]
    [ProducesResponseType(typeof(CartDto), 200)]
    public virtual async Task<IActionResult> Clear()
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        foreach (var item in cart.ToList())
            await _shoppingCartService.DeleteShoppingCartItemAsync(item);

        return Json(await BuildCartAsync(customer));
    }

    // POST /api/cart/coupon   { code }
    [HttpPost]
    [ProducesResponseType(typeof(CartDto), 200)]
    public virtual async Task<IActionResult> ApplyCoupon([FromBody] CouponApiRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });
        if (request == null || string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.coderequired") });

        await _customerService.ApplyDiscountCouponCodeAsync(customer, request.Code.Trim());
        return Json(await BuildCartAsync(customer));
    }

    // POST /api/cart/gift-card   { code }
    [HttpPost]
    [ProducesResponseType(typeof(CartDto), 200)]
    public virtual async Task<IActionResult> ApplyGiftCard([FromBody] CouponApiRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });
        if (request == null || string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.coderequired") });

        await _customerService.ApplyGiftCardCouponCodeAsync(customer, request.Code.Trim());
        return Json(await BuildCartAsync(customer));
    }

    #endregion
}
