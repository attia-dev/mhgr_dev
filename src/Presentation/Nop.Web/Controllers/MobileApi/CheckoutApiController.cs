using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MHGR.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Authentication;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Catalog;
using Nop.Services.Payments;
using Nop.Services.Shipping;

namespace Nop.Web.Controllers;

/// <summary>
/// Mobile/headless checkout API: turns the authenticated customer's cart into an order using an offline payment
/// method (Check / Money Order). Returns shared MHGR.Api.Contracts DTOs. [Mahjar mobile-api]
/// </summary>
[IgnoreAntiforgeryToken]
public partial class CheckoutApiController : Controller
{
    #region Fields

    protected readonly IAddressService _addressService;
    protected readonly IApiTokenService _apiTokenService;
    protected readonly ICustomerService _customerService;
    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly ILocalizationService _localizationService; //[Mahjar i18n]
    protected readonly IOrderProcessingService _orderProcessingService;
    protected readonly IOrderTotalCalculationService _orderTotalCalculationService;
    protected readonly IPaymentPluginManager _paymentPluginManager;
    protected readonly IProductService _productService;
    protected readonly IShippingService _shippingService;
    protected readonly IShoppingCartService _shoppingCartService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public CheckoutApiController(IAddressService addressService,
        IApiTokenService apiTokenService,
        ICustomerService customerService,
        IGenericAttributeService genericAttributeService,
        ILocalizationService localizationService, //[Mahjar i18n]
        IOrderProcessingService orderProcessingService,
        IOrderTotalCalculationService orderTotalCalculationService,
        IPaymentPluginManager paymentPluginManager,
        IProductService productService,
        IShippingService shippingService,
        IShoppingCartService shoppingCartService,
        IStoreContext storeContext,
        IWorkContext workContext)
    {
        _addressService = addressService;
        _apiTokenService = apiTokenService;
        _customerService = customerService;
        _genericAttributeService = genericAttributeService;
        _localizationService = localizationService;
        _orderProcessingService = orderProcessingService;
        _orderTotalCalculationService = orderTotalCalculationService;
        _paymentPluginManager = paymentPluginManager;
        _productService = productService;
        _shippingService = shippingService;
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

    #endregion

    #region Methods

    // POST /api/checkout
    // { "address": { "firstName","lastName","email","countryId","city","address1","zipPostalCode","phoneNumber" } }
    [HttpPost]
    [ProducesResponseType(typeof(PlacedOrderDto), 200)]
    public virtual async Task<IActionResult> Place([FromBody] CheckoutApiRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        if (request?.Address == null)
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.checkout.addressrequired") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        if (!cart.Any())
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.checkout.cartempty") });

        //re-validate the whole cart (buy/rent + no-double-booking run here too)
        var warnings = (await _shoppingCartService.GetShoppingCartWarningsAsync(cart, string.Empty, false)).ToList();
        //[Mahjar rental-occupancy] ...and every line, as order placement will, BEFORE anything is saved: a refused checkout
        //(e.g. an item rented out since it was added) must not leave a stray address on the customer
        foreach (var sci in cart)
        {
            var product = await _productService.GetProductByIdAsync(sci.ProductId);
            if (product == null)
                continue;

            warnings.AddRange(await _shoppingCartService.GetShoppingCartItemWarningsAsync(customer, sci.ShoppingCartType, product,
                store.Id, sci.AttributesXml, sci.CustomerEnteredPrice, sci.RentalStartDateUtc, sci.RentalEndDateUtc,
                sci.Quantity, false, sci.Id));
        }
        if (warnings.Any())
            return BadRequest(new { success = false, warnings = warnings.Distinct().ToList() });

        //create the billing address and attach it to the customer
        var input = request.Address;
        var address = new Address
        {
            FirstName = input.FirstName,
            LastName = input.LastName,
            Email = string.IsNullOrEmpty(input.Email) ? customer.Email : input.Email,
            CountryId = input.CountryId > 0 ? input.CountryId : null,
            City = input.City,
            Address1 = input.Address1,
            ZipPostalCode = input.ZipPostalCode,
            PhoneNumber = input.PhoneNumber,
            CreatedOnUtc = DateTime.UtcNow
        };
        await _addressService.InsertAddressAsync(address);
        await _customerService.InsertCustomerAddressAsync(customer, address);
        customer.BillingAddressId = address.Id;

        //shipping: pick the first available option if the cart needs shipping
        if (await _shoppingCartService.ShoppingCartRequiresShippingAsync(cart))
        {
            customer.ShippingAddressId = address.Id;
            await _customerService.UpdateCustomerAsync(customer);

            var response = await _shippingService.GetShippingOptionsAsync(cart, address, customer, storeId: store.Id);
            var option = response?.ShippingOptions?.FirstOrDefault();
            if (option == null)
                return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.checkout.noshippingoptions"), details = response?.Errors });

            await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.SelectedShippingOptionAttribute, option, store.Id);
        }
        else
        {
            await _customerService.UpdateCustomerAsync(customer);
        }

        //offline payment method
        const string paymentMethodSystemName = "Payments.CheckMoneyOrder";
        await _genericAttributeService.SaveAttributeAsync<string>(customer, NopCustomerDefaults.SelectedPaymentMethodAttribute, paymentMethodSystemName, store.Id);

        //place the order
        var processPaymentRequest = new ProcessPaymentRequest
        {
            StoreId = store.Id,
            CustomerId = customer.Id,
            PaymentMethodSystemName = paymentMethodSystemName,
            OrderGuid = Guid.NewGuid()
        };

        PlaceOrderResult result;
        try
        {
            result = await _orderProcessingService.PlaceOrderAsync(processPaymentRequest);
        }
        catch (Exception ex)
        {
            //PreparePlaceOrderDetailsAsync throws NopException for validation problems (e.g. required checkout
            //attributes, address/shipping/payment) — surface them as a clean 400 instead of a 500. [Mahjar mobile-api]
            //the cart validation joins its warnings as "a;b;" — show them as lines, without the trailing separator
            var details = string.Join("\n", (ex.Message ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            return BadRequest(new { success = false, error = await _localizationService.GetResourceAsync("mahjar.api.checkout.placeorderfailed"), details });
        }

        if (!result.Success)
            return BadRequest(new { success = false, error = await _localizationService.GetResourceAsync("mahjar.api.checkout.placeorderfailed"), errors = result.Errors });

        var order = result.PlacedOrder;
        return Json(new PlacedOrderDto
        {
            Id = order.Id,
            OrderNumber = order.CustomOrderNumber,
            OrderGuid = order.OrderGuid.ToString(),
            OrderTotal = order.OrderTotal,
            Currency = await GetCurrencyCodeAsync(),
            PaymentMethod = paymentMethodSystemName,
            OrderStatus = order.OrderStatus.ToString(),
            PaymentStatus = order.PaymentStatus.ToString()
        });
    }

    // POST /api/checkout/totals   (preview totals incl. refundable deposit, before placing)
    [HttpPost]
    [ProducesResponseType(typeof(CheckoutTotalsDto), 200)]
    public virtual async Task<IActionResult> Totals()
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        if (!cart.Any())
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.checkout.cartempty") });

        var (_, _, _, subTotal, _) = await _orderTotalCalculationService.GetShoppingCartSubTotalAsync(cart, true);
        var (cartTotal, _, _, _, _, _) = await _orderTotalCalculationService.GetShoppingCartTotalAsync(cart);

        //refundable deposit is baked into rental line prices; surface it for transparency [Mahjar rental-deposit]
        var deposit = decimal.Zero;
        foreach (var item in cart.Where(x => x.RentalStartDateUtc.HasValue))
        {
            var product = await _productService.GetProductByIdAsync(item.ProductId);
            if (product != null)
                deposit += product.RentalSecurityDeposit * item.Quantity;
        }

        return Json(new CheckoutTotalsDto
        {
            SubTotal = subTotal,
            Total = cartTotal,
            RefundableDeposit = deposit,
            Currency = await GetCurrencyCodeAsync()
        });
    }

    // POST /api/checkout/shipping-options   { addressId }
    [HttpPost]
    [ProducesResponseType(typeof(ShippingOptionsResultDto), 200)]
    public virtual async Task<IActionResult> ShippingOptions([FromBody] ShippingOptionsApiRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        if (!cart.Any())
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.checkout.cartempty") });

        var address = request?.AddressId > 0
            ? (await _customerService.GetAddressesByCustomerIdAsync(customer.Id)).FirstOrDefault(a => a.Id == request.AddressId)
            : null;
        if (address == null)
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.checkout.addressidrequired") });

        var response = await _shippingService.GetShippingOptionsAsync(cart, address, customer, storeId: store.Id);
        var currency = await GetCurrencyCodeAsync();
        var options = response?.ShippingOptions?
            .Select(o => new ShippingOptionDto { Name = o.Name, Description = o.Description, Rate = o.Rate, Currency = currency })
            .ToList() ?? new List<ShippingOptionDto>();

        return Json(new ShippingOptionsResultDto { Items = options, Errors = response?.Errors?.ToList() });
    }

    // GET /api/checkout/payment-methods
    [HttpGet]
    [ProducesResponseType(typeof(ListResultDto<PaymentMethodDto>), 200)]
    public virtual async Task<IActionResult> PaymentMethods()
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var methods = await _paymentPluginManager.LoadActivePluginsAsync(customer, store.Id);
        return Json(new ListResultDto<PaymentMethodDto>
        {
            Items = methods.Select(m => new PaymentMethodDto
            {
                SystemName = m.PluginDescriptor.SystemName,
                Name = m.PluginDescriptor.FriendlyName
            }).ToList()
        });
    }

    // POST /api/cart/estimate-shipping   { countryId, stateProvinceId?, city?, zipPostalCode? }
    // Estimate shipping against an ad-hoc (not persisted) address so a buyer can preview before checkout. [Mahjar mobile-api]
    [HttpPost]
    [ProducesResponseType(typeof(ShippingOptionsResultDto), 200)]
    public virtual async Task<IActionResult> EstimateShipping([FromBody] EstimateShippingApiRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });
        if (request == null || request.CountryId <= 0)
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.checkout.countryrequired") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        if (!cart.Any())
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.checkout.cartempty") });

        var address = new Address
        {
            CountryId = request.CountryId,
            StateProvinceId = request.StateProvinceId,
            City = request.City,
            ZipPostalCode = request.ZipPostalCode
        };

        var response = await _shippingService.GetShippingOptionsAsync(cart, address, customer, storeId: store.Id);
        var currency = await GetCurrencyCodeAsync();
        var options = response?.ShippingOptions?
            .Select(o => new ShippingOptionDto { Name = o.Name, Description = o.Description, Rate = o.Rate, Currency = currency })
            .ToList() ?? new List<ShippingOptionDto>();

        return Json(new ShippingOptionsResultDto { Items = options, Errors = response?.Errors?.ToList() });
    }

    #endregion
}
