using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MHGR.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Services.Authentication;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Orders;

namespace Nop.Web.Controllers;

/// <summary>
/// Return-request (RMA) API for the buyer app: list the customer's return requests, discover the returnable items
/// (plus reasons / actions) for an order, and submit a return request for purchased goods. Distinct from the
/// rental-return flow (POST /api/orders/{id}/return). Returns shared MHGR.Api.Contracts DTOs. [Mahjar mobile-api]
/// </summary>
[IgnoreAntiforgeryToken]
public partial class ReturnsApiController : Controller
{
    #region Fields

    protected readonly IApiTokenService _apiTokenService;
    protected readonly ICustomNumberFormatter _customNumberFormatter;
    protected readonly ILocalizationService _localizationService;
    protected readonly IOrderProcessingService _orderProcessingService;
    protected readonly IOrderService _orderService;
    protected readonly IPictureService _pictureService;
    protected readonly IProductService _productService;
    protected readonly IReturnRequestService _returnRequestService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkflowMessageService _workflowMessageService;
    protected readonly LocalizationSettings _localizationSettings;

    #endregion

    #region Ctor

    public ReturnsApiController(IApiTokenService apiTokenService,
        ICustomNumberFormatter customNumberFormatter,
        ILocalizationService localizationService,
        IOrderProcessingService orderProcessingService,
        IOrderService orderService,
        IPictureService pictureService,
        IProductService productService,
        IReturnRequestService returnRequestService,
        IStoreContext storeContext,
        IWorkflowMessageService workflowMessageService,
        LocalizationSettings localizationSettings)
    {
        _apiTokenService = apiTokenService;
        _customNumberFormatter = customNumberFormatter;
        _localizationService = localizationService;
        _orderProcessingService = orderProcessingService;
        _orderService = orderService;
        _pictureService = pictureService;
        _productService = productService;
        _returnRequestService = returnRequestService;
        _storeContext = storeContext;
        _workflowMessageService = workflowMessageService;
        _localizationSettings = localizationSettings;
    }

    #endregion

    #region Utilities

    protected virtual async Task<Customer> GetTokenCustomerAsync()
    {
        return await _apiTokenService.GetAuthenticatedCustomerAsync(Request.Headers["Authorization"].ToString());
    }

    protected virtual async Task<Order> GetOwnedOrderAsync(int orderId, Customer customer)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null || order.Deleted || order.CustomerId != customer.Id)
            return null;
        return order;
    }

    protected virtual async Task<string> ProductNameForOrderItemAsync(int orderItemId)
    {
        var orderItem = await _orderService.GetOrderItemByIdAsync(orderItemId);
        if (orderItem == null)
            return null;
        var product = await _productService.GetProductByIdAsync(orderItem.ProductId);
        return product == null ? null : await _localizationService.GetLocalizedAsync(product, x => x.Name);
    }

    #endregion

    #region Methods

    // GET /api/returns?page=&pageSize=
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ReturnRequestSummaryDto>), 200)]
    public virtual async Task<IActionResult> List(int page = 0, int pageSize = 20)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var requests = await _returnRequestService.SearchReturnRequestsAsync(storeId: store.Id, customerId: customer.Id,
            pageIndex: page, pageSize: Math.Clamp(pageSize, 1, 100));

        var items = new List<ReturnRequestSummaryDto>();
        foreach (var r in requests)
            items.Add(new ReturnRequestSummaryDto
            {
                Id = r.Id,
                CustomNumber = r.CustomNumber,
                ProductName = await ProductNameForOrderItemAsync(r.OrderItemId),
                Quantity = r.Quantity,
                Status = await _localizationService.GetLocalizedEnumAsync(r.ReturnRequestStatus),
                ReasonForReturn = r.ReasonForReturn,
                RequestedAction = r.RequestedAction,
                CreatedOn = r.CreatedOnUtc.ToString("yyyy-MM-dd")
            });

        return Json(new PagedResultDto<ReturnRequestSummaryDto>
        {
            Page = requests.PageIndex,
            PageSize = requests.PageSize,
            TotalItems = requests.TotalCount,
            TotalPages = requests.TotalPages,
            Items = items
        });
    }

    // GET /api/orders/{id}/returnable
    [HttpGet]
    [ProducesResponseType(typeof(ReturnableOrderDto), 200)]
    public virtual async Task<IActionResult> Returnable(int id)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var order = await GetOwnedOrderAsync(id, customer);
        if (order == null)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.ordernotfound") });

        var allowed = await _orderProcessingService.IsReturnRequestAllowedAsync(order);
        var result = new ReturnableOrderDto { ReturnAllowed = allowed };
        if (!allowed)
            return Json(result);

        var orderItems = await _orderService.GetOrderItemsAsync(order.Id, isNotReturnable: false);
        foreach (var oi in orderItems)
        {
            var product = await _productService.GetProductByIdAsync(oi.ProductId);
            var pictureId = product == null ? 0
                : (await _productService.GetProductPicturesByProductIdAsync(product.Id)).FirstOrDefault()?.PictureId ?? 0;
            result.Items.Add(new ReturnableItemDto
            {
                OrderItemId = oi.Id,
                ProductId = oi.ProductId,
                Name = product == null ? null : await _localizationService.GetLocalizedAsync(product, x => x.Name),
                ImageUrl = await _pictureService.GetPictureUrlAsync(pictureId),
                Quantity = oi.Quantity
            });
        }

        foreach (var reason in await _returnRequestService.GetAllReturnRequestReasonsAsync())
            result.Reasons.Add(new ReturnRequestReasonDto { Id = reason.Id, Name = await _localizationService.GetLocalizedAsync(reason, x => x.Name) });
        foreach (var action in await _returnRequestService.GetAllReturnRequestActionsAsync())
            result.Actions.Add(new ReturnRequestActionDto { Id = action.Id, Name = await _localizationService.GetLocalizedAsync(action, x => x.Name) });

        return Json(result);
    }

    // POST /api/orders/{id}/return-request   { items:[{orderItemId, quantity}], reasonId, actionId, comments }
    [HttpPost]
    [ProducesResponseType(typeof(ReturnRequestResultDto), 200)]
    public virtual async Task<IActionResult> Submit(int id, [FromBody] SubmitReturnApiRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var order = await GetOwnedOrderAsync(id, customer);
        if (order == null)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.ordernotfound") });
        if (!await _orderProcessingService.IsReturnRequestAllowedAsync(order))
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.returns.notallowed") });
        if (request?.Items == null || !request.Items.Any())
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.returns.selectitems") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var orderItems = (await _orderService.GetOrderItemsAsync(order.Id, isNotReturnable: false)).ToList();

        var reasons = await _returnRequestService.GetAllReturnRequestReasonsAsync();
        var actions = await _returnRequestService.GetAllReturnRequestActionsAsync();
        var reason = reasons.FirstOrDefault(x => x.Id == request.ReasonId) ?? reasons.FirstOrDefault();
        var action = actions.FirstOrDefault(x => x.Id == request.ActionId) ?? actions.FirstOrDefault();
        var reasonName = reason == null ? string.Empty : await _localizationService.GetLocalizedAsync(reason, x => x.Name);
        var actionName = action == null ? string.Empty : await _localizationService.GetLocalizedAsync(action, x => x.Name);

        var created = 0;
        foreach (var line in request.Items)
        {
            var orderItem = orderItems.FirstOrDefault(x => x.Id == line.OrderItemId);
            if (orderItem == null)
                continue;
            var quantity = Math.Clamp(line.Quantity <= 0 ? 1 : line.Quantity, 1, orderItem.Quantity);

            var rr = new ReturnRequest
            {
                StoreId = store.Id,
                OrderItemId = orderItem.Id,
                Quantity = quantity,
                ReturnedQuantity = 0,
                CustomerId = customer.Id,
                ReasonForReturn = reasonName,
                RequestedAction = actionName,
                CustomerComments = request.Comments,
                StaffNotes = string.Empty,
                ReturnRequestStatusId = (int)ReturnRequestStatus.Pending,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            await _returnRequestService.InsertReturnRequestAsync(rr);
            rr.CustomNumber = _customNumberFormatter.GenerateReturnRequestCustomNumber(rr);
            await _returnRequestService.UpdateReturnRequestAsync(rr);
            created++;

            //notifications are best-effort — a mail failure must not fail the request
            try
            {
                await _workflowMessageService.SendNewReturnRequestStoreOwnerNotificationAsync(rr, orderItem, order, _localizationSettings.DefaultAdminLanguageId);
                await _workflowMessageService.SendNewReturnRequestCustomerNotificationAsync(rr, orderItem, order);
            }
            catch { /* ignore mail errors */ }
        }

        if (created == 0)
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.returns.novaliditems") });

        return Json(new ReturnRequestResultDto { Success = true, Created = created, Message = await _localizationService.GetResourceAsync("mahjar.api.returns.submitted") });
    }

    #endregion
}
