using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MHGR.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Services.Authentication;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Mahjar.Contracts;
using Nop.Services.Mahjar.Handover;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Vendors;

namespace Nop.Web.Controllers;

/// <summary>
/// Customer order-history API (bearer-token authenticated). Buyers can list and open their own orders — including
/// rental dates, the refundable deposit per line, and the return / deposit-refund status — cancel an order, and
/// request a rental return. Returns shared MHGR.Api.Contracts DTOs. [Mahjar mobile-api]
/// </summary>
[IgnoreAntiforgeryToken]
public partial class OrdersApiController : Controller
{
    #region Constants

    //per-order-item generic attributes that track the rental return / deposit lifecycle [Mahjar rental-deposit]
    public const string RentalReturnStatusAttribute = "MahjarRentalReturnStatus";   // ReturnRequested / Returned / Refunded
    public const string RentalReturnDateAttribute = "MahjarRentalReturnRequestedOn";

    //[Mahjar handover] return states after which the customer has nothing more to announce: a desk return must never be
    //turned back into "return requested"
    protected static readonly string[] ClosedReturnStates = { "ReturnRequested", "Returned", "Refunded" };

    #endregion

    #region Fields

    protected readonly IApiTokenService _apiTokenService;
    protected readonly IConditionCheckService _conditionCheckService; //[Mahjar handover]
    protected readonly IContractService _contractService; //[Mahjar handover]
    protected readonly ICustomerService _customerService;
    protected readonly IDateTimeHelper _dateTimeHelper; //[Mahjar handover]
    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IOrderProcessingService _orderProcessingService;
    protected readonly IOrderService _orderService;
    protected readonly IPictureService _pictureService;
    protected readonly IProductAttributeFormatter _productAttributeFormatter;
    protected readonly IProductService _productService;
    protected readonly IStoreContext _storeContext;
    protected readonly IVendorService _vendorService;
    protected readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public OrdersApiController(IApiTokenService apiTokenService,
        IConditionCheckService conditionCheckService,
        IContractService contractService,
        ICustomerService customerService,
        IDateTimeHelper dateTimeHelper,
        IGenericAttributeService genericAttributeService,
        ILocalizationService localizationService,
        IOrderProcessingService orderProcessingService,
        IOrderService orderService,
        IPictureService pictureService,
        IProductAttributeFormatter productAttributeFormatter,
        IProductService productService,
        IStoreContext storeContext,
        IVendorService vendorService,
        IWorkContext workContext)
    {
        _apiTokenService = apiTokenService;
        _conditionCheckService = conditionCheckService;
        _contractService = contractService;
        _customerService = customerService;
        _dateTimeHelper = dateTimeHelper;
        _genericAttributeService = genericAttributeService;
        _localizationService = localizationService;
        _orderProcessingService = orderProcessingService;
        _orderService = orderService;
        _pictureService = pictureService;
        _productAttributeFormatter = productAttributeFormatter;
        _productService = productService;
        _storeContext = storeContext;
        _vendorService = vendorService;
        _workContext = workContext;
    }

    #endregion

    #region Utilities

    protected virtual async Task<Customer> GetTokenCustomerAsync()
    {
        return await _apiTokenService.GetAuthenticatedCustomerAsync(Request.Headers["Authorization"].ToString());
    }

    //load an order and assert it belongs to the authenticated customer (else null) [Mahjar mobile-api]
    protected virtual async Task<Order> GetOwnedOrderAsync(int orderId, Customer customer)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null || order.Deleted || order.CustomerId != customer.Id)
            return null;
        return order;
    }

    protected virtual async Task<string> GetCurrencyCodeAsync()
    {
        return (await _workContext.GetWorkingCurrencyAsync()).CurrencyCode;
    }

    /// <summary>
    /// [Mahjar handover] A buyer may withdraw an order only while it is unpaid and nothing of it has reached them. The
    /// stock check lets anything not yet cancelled be cancelled, which from the app would restock — and free the rental
    /// dates of — a watch the customer is holding; anything later goes through the store (refunds are manual).
    /// </summary>
    protected virtual async Task<bool> CanBuyerCancelAsync(Order order)
    {
        if (!_orderProcessingService.CanCancelOrder(order))
            return false;

        if (order.OrderStatus is not (OrderStatus.Pending or OrderStatus.Processing) || order.PaymentStatus != PaymentStatus.Pending)
            return false;

        if (order.ShippingStatus is not (ShippingStatus.NotYetShipped or ShippingStatus.ShippingNotRequired))
            return false;

        foreach (var orderItem in await _orderService.GetOrderItemsAsync(order.Id))
        {
            if (await _conditionCheckService.IsOrderItemDeliveredAsync(orderItem, order))
                return false;
        }

        return true;
    }

    //first product picture URL (same resolution as the catalog API) [Mahjar mobile-api]
    protected virtual async Task<string> GetImageUrlAsync(Nop.Core.Domain.Catalog.Product product)
    {
        if (product == null)
            return null;
        var pictures = await _productService.GetProductPicturesByProductIdAsync(product.Id);
        return await _pictureService.GetPictureUrlAsync(pictures.FirstOrDefault()?.PictureId ?? 0);
    }

    #endregion

    #region Methods

    // GET /api/orders?page=&pageSize=
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<OrderSummaryDto>), 200)]
    public virtual async Task<IActionResult> List(int page = 0, int pageSize = 20)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var orders = await _orderService.SearchOrdersAsync(storeId: store.Id, customerId: customer.Id,
            pageIndex: page, pageSize: Math.Clamp(pageSize, 1, 100));

        var currency = await GetCurrencyCodeAsync();
        var items = new List<OrderSummaryDto>();
        foreach (var o in orders)
            items.Add(new OrderSummaryDto
            {
                Id = o.Id,
                OrderNumber = o.CustomOrderNumber,
                CreatedOn = o.CreatedOnUtc.ToString("yyyy-MM-dd"),
                OrderStatus = await _localizationService.GetLocalizedEnumAsync(o.OrderStatus),
                PaymentStatus = await _localizationService.GetLocalizedEnumAsync(o.PaymentStatus),
                ShippingStatus = await _localizationService.GetLocalizedEnumAsync(o.ShippingStatus),
                OrderTotal = o.OrderTotal,
                Currency = currency
            });

        return Json(new PagedResultDto<OrderSummaryDto>
        {
            Page = orders.PageIndex,
            PageSize = orders.PageSize,
            TotalItems = orders.TotalCount,
            TotalPages = orders.TotalPages,
            Items = items
        });
    }

    // GET /api/orders/{id}
    [HttpGet]
    [ProducesResponseType(typeof(OrderDetailDto), 200)]
    public virtual async Task<IActionResult> Detail(int id)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var order = await GetOwnedOrderAsync(id, customer);
        if (order == null)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.ordernotfound") });

        var orderItems = await _orderService.GetOrderItemsAsync(order.Id);
        var store = await _storeContext.GetCurrentStoreAsync();
        var items = new List<OrderItemDto>();
        foreach (var oi in orderItems)
        {
            var product = await _productService.GetProductByIdAsync(oi.ProductId);
            string vendorName = null;
            if (product is { VendorId: > 0 })
                vendorName = (await _vendorService.GetVendorByIdAsync(product.VendorId))?.Name;

            var isRental = oi.RentalStartDateUtc.HasValue;
            decimal? deposit = isRental && product is { RentalSecurityDeposit: > 0 }
                ? product.RentalSecurityDeposit * oi.Quantity
                : null;

            //rental return / deposit-refund status (tracked per order item) [Mahjar rental-deposit]
            string returnStatus = null;
            string returnRequestedOn = null;
            if (isRental)
            {
                returnStatus = await _genericAttributeService.GetAttributeAsync<string>(oi, RentalReturnStatusAttribute);
                returnRequestedOn = await _genericAttributeService.GetAttributeAsync<string>(oi, RentalReturnDateAttribute);
            }

            //[Mahjar handover] handed over at the desk or from the order screens; only such a rental, not back yet, can be
            //announced as coming back
            var handedOver = await _conditionCheckService.IsOrderItemDeliveredAsync(oi, order);

            items.Add(new OrderItemDto
            {
                OrderItemId = oi.Id,
                ProductId = oi.ProductId,
                Name = product == null ? null : await _localizationService.GetLocalizedAsync(product, x => x.Name),
                ImageUrl = await GetImageUrlAsync(product),
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPriceInclTax,
                LineTotal = oi.PriceInclTax,
                Attributes = product != null && !string.IsNullOrEmpty(oi.AttributesXml)
                    ? await _productAttributeFormatter.FormatAttributesAsync(product, oi.AttributesXml, customer, store,
                        separator: "، ", htmlEncode: false, renderPrices: false)
                    : null,
                IsRental = isRental,
                RentalStartDate = oi.RentalStartDateUtc?.ToString("yyyy-MM-dd"),
                RentalEndDate = oi.RentalEndDateUtc?.ToString("yyyy-MM-dd"),
                RentalSecurityDeposit = deposit,
                RentalReturnStatus = returnStatus,
                RentalReturnRequestedOn = returnRequestedOn,
                VendorId = product is { VendorId: > 0 } ? product.VendorId : (int?)null,
                VendorName = vendorName,
                HandedOver = handedOver,
                CanRequestReturn = isRental && handedOver && !ClosedReturnStates.Contains(returnStatus)
            });
        }

        //[Mahjar handover] the contract behind the order (its document and condition records live there) and the
        //customer-visible notes: an item handed over, a rental returned with its deposit refund
        var contract = await _contractService.GetRequestByOrderIdAsync(order.Id);
        var notes = new List<OrderNoteDto>();
        foreach (var note in (await _orderService.GetOrderNotesByOrderIdAsync(order.Id, displayToCustomer: true))
                     .OrderByDescending(note => note.CreatedOnUtc).ThenByDescending(note => note.Id))
        {
            notes.Add(new OrderNoteDto
            {
                CreatedOn = (await _dateTimeHelper.ConvertToUserTimeAsync(note.CreatedOnUtc, DateTimeKind.Utc))
                    .ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                Text = note.Note
            });
        }

        return Json(new OrderDetailDto
        {
            ContractRequestId = contract?.Id ?? 0,
            HasConditionRecords = contract != null && (await _conditionCheckService.GetCustomerVisibleChecksAsync(contract)).Any(),
            Notes = notes,
            Id = order.Id,
            OrderNumber = order.CustomOrderNumber,
            CreatedOn = order.CreatedOnUtc.ToString("yyyy-MM-dd"),
            OrderStatus = await _localizationService.GetLocalizedEnumAsync(order.OrderStatus),
            PaymentStatus = await _localizationService.GetLocalizedEnumAsync(order.PaymentStatus),
            ShippingStatus = await _localizationService.GetLocalizedEnumAsync(order.ShippingStatus),
            CanCancel = await CanBuyerCancelAsync(order), //[Mahjar handover]
            Items = items,
            SubTotal = order.OrderSubtotalInclTax,
            Shipping = order.OrderShippingInclTax,
            Tax = order.OrderTax,
            Discount = order.OrderDiscount,
            OrderTotal = order.OrderTotal,
            RefundedAmount = order.RefundedAmount,
            Currency = await GetCurrencyCodeAsync()
        });
    }

    // POST /api/orders/{id}/cancel
    [HttpPost]
    [ProducesResponseType(typeof(OrderStatusResultDto), 200)]
    public virtual async Task<IActionResult> Cancel(int id)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var order = await GetOwnedOrderAsync(id, customer);
        if (order == null)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.ordernotfound") });

        if (!await CanBuyerCancelAsync(order)) //[Mahjar handover] not once paid or handed over
            return BadRequest(new { success = false, error = await _localizationService.GetResourceAsync("mahjar.api.orders.cannotcancel") });

        await _orderProcessingService.CancelOrderAsync(order, notifyCustomer: true);
        return Json(new OrderStatusResultDto { Success = true, OrderStatus = await _localizationService.GetLocalizedEnumAsync(order.OrderStatus) });
    }

    // POST /api/orders/{id}/return   { "orderItemId"?: 0, "note"?: "..." }
    // Marks the rental line(s) as return-requested so the seller can inspect and release the deposit.
    [HttpPost]
    [ProducesResponseType(typeof(ReturnResultDto), 200)]
    public virtual async Task<IActionResult> ReturnRequest(int id, [FromBody] ReturnApiRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var order = await GetOwnedOrderAsync(id, customer);
        if (order == null)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.ordernotfound") });

        var rentalItems = (await _orderService.GetOrderItemsAsync(order.Id))
            .Where(x => x.RentalStartDateUtc.HasValue).ToList();
        if (request?.OrderItemId > 0)
            rentalItems = rentalItems.Where(x => x.Id == request.OrderItemId).ToList();
        if (!rentalItems.Any())
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.orders.norentalitems") });

        //[Mahjar handover] only rentals the customer has (handed over) and that are not back or announced yet: a return
        //the handover desk recorded must never be turned back into "return requested"
        var returnable = new List<OrderItem>();
        foreach (var oi in rentalItems)
        {
            var status = await _genericAttributeService.GetAttributeAsync<string>(oi, RentalReturnStatusAttribute);
            if (!ClosedReturnStates.Contains(status) && await _conditionCheckService.IsOrderItemDeliveredAsync(oi, order))
                returnable.Add(oi);
        }
        if (!returnable.Any())
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.orders.nothingtoreturn") });
        rentalItems = returnable;

        foreach (var oi in rentalItems)
        {
            await _genericAttributeService.SaveAttributeAsync(oi, RentalReturnStatusAttribute, "ReturnRequested");
            await _genericAttributeService.SaveAttributeAsync(oi, RentalReturnDateAttribute, DateTime.UtcNow.ToString("yyyy-MM-dd"));
        }

        var note = string.IsNullOrWhiteSpace(request?.Note)
            ? string.Empty
            : string.Format(await _localizationService.GetResourceAsync("mahjar.api.orders.returnnotedetail"), request.Note.Trim());
        await _orderService.InsertOrderNoteAsync(new OrderNote
        {
            OrderId = order.Id,
            Note = string.Format(await _localizationService.GetResourceAsync("mahjar.api.orders.returnnote"), rentalItems.Count, note),
            DisplayToCustomer = true,
            CreatedOnUtc = DateTime.UtcNow
        });

        return Json(new ReturnResultDto
        {
            Success = true,
            ReturnStatus = "ReturnRequested",
            ItemsMarked = rentalItems.Count,
            Message = await _localizationService.GetResourceAsync("mahjar.api.orders.returnrequested")
        });
    }

    // POST /api/orders/{id}/reorder   (re-adds the order's items to the cart) [Mahjar mobile-api]
    [HttpPost]
    [ProducesResponseType(typeof(MessageResponseDto), 200)]
    public virtual async Task<IActionResult> ReOrder(int id)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var order = await GetOwnedOrderAsync(id, customer);
        if (order == null)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.ordernotfound") });

        var warnings = await _orderProcessingService.ReOrderAsync(order);
        if (warnings.Any())
            return BadRequest(new { success = false, warnings });

        return Json(new MessageResponseDto { Success = true, Message = await _localizationService.GetResourceAsync("mahjar.api.orders.reordered") });
    }

    #endregion
}
