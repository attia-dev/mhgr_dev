// [Mahjar handover]
using System.Globalization;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Mahjar;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Mahjar.Notifications;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Services.Vendors;

namespace Nop.Services.Mahjar.Handover;

/// <summary>
/// In-app notifications of the handover desk, every recipient in their own language.
/// <list type="bullet">
/// <item>The handover staff hear about every task that lands in one of their queues: a contract request whose items
/// need before-signing condition media, a paid order waiting to be handed over, a rental now out with a customer.</item>
/// <item>Every confirmation is reported to the admins and to the other party: a delivery to the buyer, an intake to
/// the partner whose item it is, a rental return (with the deposit refund that is due) to the buyer.</item>
/// </list>
/// The desk delivers through the stock shipment path; the notification consumer skips the generic shipped / delivered
/// rows of the desk's own shipments, since the delivery row here already says so, with the condition record.
/// Failures are logged and never break the originating flow. Auto-discovered by ITypeFinder. [Mahjar handover]
/// </summary>
public partial class HandoverEventConsumer :
    IConsumer<ConditionCheckConfirmedEvent>,
    IConsumer<ContractRequestedEvent>,
    IConsumer<OrderPaidEvent>,
    IConsumer<HandoverAssignedEvent>, //[Mahjar handover]
    IConsumer<ShipmentDeliveredEvent>
{
    #region Constants

    /// <summary>The entity name of rows that open a handover-desk queue; their stored URL is the deep link.</summary>
    protected const string HANDOVER_ENTITY_NAME = "Handover";

    #endregion

    #region Fields

    protected readonly CurrencySettings _currencySettings;
    protected readonly IConditionCheckService _conditionCheckService;
    protected readonly IHandoverAssignmentService _assignmentService; //[Mahjar handover]
    protected readonly ICurrencyService _currencyService;
    protected readonly ICustomerService _customerService;
    protected readonly ILanguageService _languageService;
    protected readonly ILocalizationService _localizationService;
    protected readonly ILogger _logger;
    protected readonly INotificationFeedService _feedService;
    protected readonly IOrderService _orderService;
    protected readonly IPriceFormatter _priceFormatter;
    protected readonly IProductService _productService;
    protected readonly IShipmentService _shipmentService;
    protected readonly IStoreContext _storeContext;
    protected readonly IStoreService _storeService;
    protected readonly IVendorService _vendorService;
    protected readonly IWorkContext _workContext;
    protected readonly LocalizationSettings _localizationSettings;

    #endregion

    #region Ctor

    public HandoverEventConsumer(CurrencySettings currencySettings,
        IConditionCheckService conditionCheckService,
        IHandoverAssignmentService assignmentService,
        ICurrencyService currencyService,
        ICustomerService customerService,
        ILanguageService languageService,
        ILocalizationService localizationService,
        ILogger logger,
        INotificationFeedService feedService,
        IOrderService orderService,
        IPriceFormatter priceFormatter,
        IProductService productService,
        IShipmentService shipmentService,
        IStoreContext storeContext,
        IStoreService storeService,
        IVendorService vendorService,
        IWorkContext workContext,
        LocalizationSettings localizationSettings)
    {
        _currencySettings = currencySettings;
        _conditionCheckService = conditionCheckService;
        _assignmentService = assignmentService;
        _currencyService = currencyService;
        _customerService = customerService;
        _languageService = languageService;
        _localizationService = localizationService;
        _logger = logger;
        _feedService = feedService;
        _orderService = orderService;
        _priceFormatter = priceFormatter;
        _productService = productService;
        _shipmentService = shipmentService;
        _storeContext = storeContext;
        _storeService = storeService;
        _vendorService = vendorService;
        _workContext = workContext;
        _localizationSettings = localizationSettings;
    }

    #endregion

    #region Utilities

    /// <summary>The customer-facing number of a contract request.</summary>
    protected static string GetContractNumber(int contractRequestId)
    {
        return $"MHGR-C-{contractRequestId:D5}";
    }

    /// <summary>
    /// Resolve a usable, published language id: preferred → current working language → admin default → first.
    /// (Same fallback chain as the contract and notification event consumers.)
    /// </summary>
    protected virtual async Task<int> EnsureLanguageAsync(int? preferredLanguageId, int storeId)
    {
        if (preferredLanguageId is > 0)
        {
            var language = await _languageService.GetLanguageByIdAsync(preferredLanguageId.Value);
            if (language is { Published: true })
                return language.Id;
        }

        var working = await _workContext.GetWorkingLanguageAsync();
        if (working is { Published: true })
            return working.Id;

        if (_localizationSettings.DefaultAdminLanguageId > 0)
        {
            var adminLanguage = await _languageService.GetLanguageByIdAsync(_localizationSettings.DefaultAdminLanguageId);
            if (adminLanguage is { Published: true })
                return adminLanguage.Id;
        }

        return (await _languageService.GetAllLanguagesAsync(storeId: storeId)).FirstOrDefault()?.Id ?? 0;
    }

    /// <summary>The buyer's language for an order: the language recorded on the order, else the customer's.</summary>
    protected virtual async Task<int> GetBuyerLanguageIdAsync(Order order)
    {
        var preferred = order.CustomerLanguageId > 0
            ? order.CustomerLanguageId
            : (await _customerService.GetCustomerByIdAsync(order.CustomerId))?.LanguageId;

        return await EnsureLanguageAsync(preferred, order.StoreId);
    }

    /// <summary>
    /// The language a dashboard user (admin, handover employee, partner) reads the admin panel in, resolved the way
    /// WebWorkContext does it: their saved language when it is published for the store, else the store default, else
    /// the first. Never the language of whoever happened to trigger the event.
    /// </summary>
    protected virtual async Task<int> GetDashboardLanguageIdAsync(Customer customer, int storeId)
    {
        var languages = await _languageService.GetAllLanguagesAsync(storeId: storeId);
        var store = await _storeService.GetStoreByIdAsync(storeId);

        var language = languages.FirstOrDefault(l => l.Id == customer.LanguageId)
                       ?? languages.FirstOrDefault(l => l.Id == store?.DefaultLanguageId)
                       ?? languages.FirstOrDefault();

        return language?.Id ?? await EnsureLanguageAsync(customer.LanguageId, storeId);
    }

    /// <summary>Formats an amount in the primary store currency for a language.</summary>
    protected virtual async Task<string> FormatAmountAsync(decimal amount, int languageId)
    {
        var currency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
        return await _priceFormatter.FormatPriceAsync(amount, true, currency, languageId, false);
    }

    /// <summary>A product's name in a language (empty when the product is gone).</summary>
    protected virtual async Task<string> GetProductNameAsync(int productId, int languageId)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        return product == null ? string.Empty : await _localizationService.GetLocalizedAsync(product, p => p.Name, languageId);
    }

    /// <summary>The names of several products in a language, joined the way that language writes a list.</summary>
    protected virtual async Task<string> GetProductNamesAsync(IEnumerable<int> productIds, int languageId)
    {
        var names = new List<string>();
        foreach (var productId in productIds.Distinct())
        {
            var name = await GetProductNameAsync(productId, languageId);
            if (!string.IsNullOrEmpty(name))
                names.Add(name);
        }

        var language = await _languageService.GetLanguageByIdAsync(languageId);
        var isArabic = language != null &&
                       (string.Equals(language.UniqueSeoCode, "ar", StringComparison.OrdinalIgnoreCase) ||
                        (language.LanguageCulture?.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ?? false));

        return string.Join(isArabic ? "، " : ", ", names);
    }

    /// <summary>A display name for an employee: full name, falling back to the email.</summary>
    protected virtual async Task<string> GetCustomerDisplayNameAsync(int customerId)
    {
        var customer = customerId > 0 ? await _customerService.GetCustomerByIdAsync(customerId) : null;
        if (customer == null)
            return string.Empty;

        var fullName = await _customerService.GetCustomerFullNameAsync(customer);
        return string.IsNullOrWhiteSpace(fullName) ? customer.Email : fullName;
    }

    /// <summary>The active administrators.</summary>
    protected virtual async Task<IList<Customer>> GetAdminsAsync()
    {
        var role = await _customerService.GetCustomerRoleBySystemNameAsync(NopCustomerDefaults.AdministratorsRoleName);
        if (role == null)
            return new List<Customer>();

        return (await _customerService.GetAllCustomersAsync(customerRoleIds: new[] { role.Id }))
            .Where(customer => customer.Active && !customer.Deleted)
            .ToList();
    }

    /// <summary>
    /// The active handover employees (members of the active HandoverStaff role), without the employee who just acted
    /// when one is given.
    /// </summary>
    protected virtual async Task<IList<Customer>> GetHandoverStaffAsync(int exceptCustomerId = 0)
    {
        var role = await _customerService.GetCustomerRoleBySystemNameAsync(NopCustomerDefaults.HandoverStaffRoleName);
        if (role is not { Active: true })
            return new List<Customer>();

        return (await _customerService.GetAllCustomersAsync(customerRoleIds: new[] { role.Id }))
            .Where(customer => customer.Active && !customer.Deleted && customer.Id != exceptCustomerId)
            .ToList();
    }

    /// <summary>Compose a notification row from the localized title/body resources of its type.</summary>
    protected virtual async Task<Notification> BuildAsync(int recipientCustomerId, NotificationRecipientType recipientType,
        int recipientVendorId, NotificationType type, int languageId, int storeId, string entityName, int entityId,
        string url, params object[] bodyArgs)
    {
        var key = "mahjar.notifications.type." + type.ToString().ToLowerInvariant();
        var title = await _localizationService.GetResourceAsync(key + ".title", languageId);
        var bodyFormat = await _localizationService.GetResourceAsync(key + ".body", languageId);

        return new Notification
        {
            RecipientCustomerId = recipientCustomerId,
            RecipientVendorId = recipientVendorId,
            RecipientType = recipientType,
            NotificationType = type,
            Title = title,
            Body = bodyArgs is { Length: > 0 } && !string.IsNullOrEmpty(bodyFormat) ? string.Format(bodyFormat, bodyArgs) : bodyFormat,
            EntityName = entityName,
            EntityId = entityId,
            Url = url,
            StoreId = storeId,
            CreatedOnUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// One row for every dashboard recipient, each in their own language (the body arguments are composed once per
    /// language).
    /// </summary>
    protected virtual async Task NotifyDashboardAsync(IEnumerable<Customer> recipients, NotificationRecipientType recipientType,
        int recipientVendorId, NotificationType type, int storeId, string entityName, int entityId, string url,
        Func<int, Task<object[]>> bodyArgsForLanguage)
    {
        var argsByLanguage = new Dictionary<int, object[]>();
        var rows = new List<Notification>();
        foreach (var recipient in recipients)
        {
            var languageId = await GetDashboardLanguageIdAsync(recipient, storeId);
            if (!argsByLanguage.TryGetValue(languageId, out var bodyArgs))
                argsByLanguage[languageId] = bodyArgs = await bodyArgsForLanguage(languageId);

            rows.Add(await BuildAsync(recipient.Id, recipientType, recipientVendorId, type, languageId, storeId,
                entityName, entityId, url, bodyArgs));
        }

        await _feedService.CreateNotificationsAsync(rows);
    }

    /// <summary>Tells the handover staff that rented items are now out with the customer, and when they are due back.</summary>
    protected virtual async Task NotifyReturnTaskAsync(Order order, IList<OrderItem> rentalItems, int exceptCustomerId)
    {
        var dueDates = rentalItems
            .Where(item => item.RentalEndDateUtc.HasValue)
            .Select(item => item.RentalEndDateUtc.Value)
            .ToList();
        var due = dueDates.Any() ? dueDates.Min().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;

        //[Mahjar handover] the assignee takes the item back — including when they are the very employee who just
        //handed it over, so they are NOT excluded here. Only an unassigned order still reaches the rest of the desk.
        var assignee = await GetAssigneeAsync(order.Id);
        var recipients = assignee != null
            ? new List<Customer> { assignee }
            : await GetHandoverStaffAsync(exceptCustomerId);

        if (!recipients.Any())
            return;

        await NotifyDashboardAsync(recipients, NotificationRecipientType.Admin, 0,
            NotificationType.HandoverReturnStaff, order.StoreId, HANDOVER_ENTITY_NAME, order.Id,
            //[Mahjar handover] carry the order, so the employee lands on ITS rows and not the whole return queue
            $"/Admin/Handover/List?stageId={(int)HandoverStage.AwaitingReturn}&keyword={Uri.EscapeDataString(order.CustomOrderNumber ?? string.Empty)}",
            async languageId => new object[]
            {
                order.CustomOrderNumber, await GetProductNamesAsync(rentalItems.Select(item => item.ProductId), languageId), due
            });
    }

    /// <summary>An intake from a partner was confirmed: the admins, and the partner whose item it is.</summary>
    protected virtual async Task NotifyIntakeRecordedAsync(ConditionCheck check)
    {
        var product = await _productService.GetProductByIdAsync(check.ProductId);
        if (product == null)
            return;

        var storeId = (await _storeContext.GetCurrentStoreAsync()).Id;
        var employeeName = await GetCustomerDisplayNameAsync(check.ConfirmedByCustomerId);
        var url = $"/Admin/Product/Edit/{product.Id}";

        await NotifyDashboardAsync(await GetAdminsAsync(), NotificationRecipientType.Admin, 0,
            NotificationType.IntakeRecordedAdmin, storeId, "Product", product.Id, url,
            async languageId => new object[] { await GetProductNameAsync(product.Id, languageId), employeeName });

        var vendor = product.VendorId > 0 ? await _vendorService.GetVendorByIdAsync(product.VendorId) : null;
        if (vendor is not { Deleted: false })
            return;

        var managers = (await _customerService.GetAllCustomersAsync(vendorId: vendor.Id))
            .Where(customer => customer.Active && !customer.Deleted)
            .ToList();
        await NotifyDashboardAsync(managers, NotificationRecipientType.Vendor, vendor.Id,
            NotificationType.IntakeRecordedVendor, storeId, "Product", product.Id, url,
            async languageId => new object[] { await GetProductNameAsync(product.Id, languageId) });
    }

    /// <summary>An item was handed over: the buyer, the admins and, for a rental, the staff's return queue.</summary>
    protected virtual async Task NotifyDeliveredAsync(ConditionCheck check)
    {
        var orderItem = await _orderService.GetOrderItemByIdAsync(check.OrderItemId);
        var order = orderItem == null ? null : await _orderService.GetOrderByIdAsync(orderItem.OrderId);
        if (order == null)
            return;

        //the item may have been delivered from the stock order screens BEFORE the desk documented it: the buyer then had
        //the stock "delivered" row, and a rental its return task, at that time — only the admins hear of the record now
        var deliveredEarlier = false;
        foreach (var shipment in await _shipmentService.GetShipmentsByOrderIdAsync(order.Id))
        {
            if (shipment.DeliveryDateUtc.HasValue && shipment.DeliveryDateUtc < check.ConfirmedOnUtc &&
                (await _shipmentService.GetShipmentItemsByShipmentIdAsync(shipment.Id)).Any(item => item.OrderItemId == orderItem.Id))
            {
                deliveredEarlier = true;
                break;
            }
        }

        //the buyer, in the language of the order
        if (!deliveredEarlier)
        {
            var buyerLanguageId = await GetBuyerLanguageIdAsync(order);
            await _feedService.CreateNotificationAsync(await BuildAsync(order.CustomerId, NotificationRecipientType.Buyer, 0,
                NotificationType.HandoverDeliveredBuyer, buyerLanguageId, order.StoreId, "Order", order.Id, $"orderdetails/{order.Id}",
                order.CustomOrderNumber, await GetProductNameAsync(check.ProductId, buyerLanguageId)));
        }

        //every admin, each in their own language
        var employeeName = await GetCustomerDisplayNameAsync(check.ConfirmedByCustomerId);
        await NotifyDashboardAsync(await GetAdminsAsync(), NotificationRecipientType.Admin, 0,
            NotificationType.HandoverDeliveredAdmin, order.StoreId, "Order", order.Id, $"/Admin/Order/Edit/{order.Id}",
            async languageId => new object[] { order.CustomOrderNumber, await GetProductNameAsync(check.ProductId, languageId), employeeName });

        //a rental is now out with the customer, so it enters the return queue (the employee who handed it over knows)
        if (orderItem.RentalStartDateUtc.HasValue && !deliveredEarlier)
            await NotifyReturnTaskAsync(order, new List<OrderItem> { orderItem }, check.ConfirmedByCustomerId);
    }

    /// <summary>A rental came back: the buyer learns their refund, every admin that the refund is due.</summary>
    protected virtual async Task NotifyReturnedAsync(ConditionCheck check)
    {
        var order = await _orderService.GetOrderByIdAsync(check.OrderId);
        if (order == null)
            return;

        var refund = Math.Max(check.DepositAmount - check.DamageDeduction, decimal.Zero);

        //the buyer, in the language of the order
        var buyerLanguageId = await GetBuyerLanguageIdAsync(order);
        await _feedService.CreateNotificationAsync(await BuildAsync(order.CustomerId, NotificationRecipientType.Buyer, 0,
            NotificationType.RentalReturnRecordedBuyer, buyerLanguageId, order.StoreId, "Order", order.Id, $"orderdetails/{order.Id}",
            order.CustomOrderNumber, await FormatAmountAsync(refund, buyerLanguageId)));

        //every admin, each in their own language: the deposit refund is due (it is still made by hand)
        await NotifyDashboardAsync(await GetAdminsAsync(), NotificationRecipientType.Admin, 0,
            NotificationType.RentalReturnRecordedAdmin, order.StoreId, "Order", order.Id, $"/Admin/Order/Edit/{order.Id}",
            async languageId => new object[]
            {
                order.CustomOrderNumber, await GetProductNameAsync(check.ProductId, languageId),
                await FormatAmountAsync(refund, languageId), await FormatAmountAsync(check.DepositAmount, languageId)
            });
    }

    #endregion

    #region Methods

    /// <summary>A check was confirmed: tell the admins and the other party of the handover.</summary>
    public virtual async Task HandleEventAsync(ConditionCheckConfirmedEvent eventMessage)
    {
        var check = eventMessage?.Check;
        if (check == null)
            return;

        try
        {
            switch (check.CheckType)
            {
                case ConditionCheckType.Intake:
                    await NotifyIntakeRecordedAsync(check);
                    break;

                case ConditionCheckType.Delivery:
                    await NotifyDeliveredAsync(check);
                    break;

                case ConditionCheckType.Return:
                    await NotifyReturnedAsync(check);
                    break;
            }
        }
        catch (Exception exc)
        {
            await _logger.ErrorAsync($"[Mahjar handover] notifications for condition check #{check.Id} failed: {exc.Message}", exc);
        }
    }

    /// <summary>A new contract request: its items need before-signing condition media before it can be sent.</summary>
    public virtual async Task HandleEventAsync(ContractRequestedEvent eventMessage)
    {
        var request = eventMessage?.ContractRequest;
        if (request == null)
            return;

        try
        {
            var productIds = await _conditionCheckService.GetProductIdsMissingPreContractMediaAsync(request);
            if (!productIds.Any())
                return;

            await NotifyDashboardAsync(await GetHandoverStaffAsync(), NotificationRecipientType.Admin, 0,
                NotificationType.HandoverPreContractStaff, request.StoreId, "ContractRequest", request.Id,
                $"/Admin/ContractRequest/Edit/{request.Id}",
                async languageId => new object[] { GetContractNumber(request.Id), await GetProductNamesAsync(productIds, languageId) });
        }
        catch (Exception exc)
        {
            await _logger.ErrorAsync($"[Mahjar handover] pre-contract task notification for contract request #{request.Id} failed: {exc.Message}", exc);
        }
    }

    /// <summary>An order was paid: its items wait at the desk to be handed over to the customer.</summary>
    public virtual async Task HandleEventAsync(OrderPaidEvent eventMessage)
    {
        var order = eventMessage?.Order;
        if (order == null)
            return;

        try
        {
            var items = await _conditionCheckService.GetItemsAwaitingDeliveryAsync(order);
            if (!items.Any())
                return;

            //[Mahjar handover] the work belongs to the one employee the order was assigned to. Until an admin assigns
            //it, the desk is not told at all — the admins are asked to assign it instead.
            var assignee = await GetAssigneeAsync(order.Id);
            if (assignee != null)
            {
                await NotifyDeliveryTaskAsync(order, items, new[] { assignee });

                return;
            }

            await NotifyDashboardAsync(await GetAdminsAsync(), NotificationRecipientType.Admin, 0,
                NotificationType.HandoverUnassignedAdmin, order.StoreId, HANDOVER_ENTITY_NAME, order.Id,
                $"/Admin/Order/Edit/{order.Id}",
                async languageId => new object[] { order.CustomOrderNumber, await GetProductNamesAsync(items.Select(item => item.ProductId), languageId) });
        }
        catch (Exception exc)
        {
            await _logger.ErrorAsync($"[Mahjar handover] delivery task notification for order #{order.Id} failed: {exc.Message}", exc);
        }
    }

    /// <summary>[Mahjar handover] The active employee an order's handover is assigned to (null when nobody is).</summary>
    protected virtual async Task<Customer> GetAssigneeAsync(int orderId)
    {
        var assignedTo = await _assignmentService.GetAssignedCustomerIdAsync(orderId);
        if (assignedTo == 0)
            return null;

        var employee = await _customerService.GetCustomerByIdAsync(assignedTo);

        return employee is { Active: true, Deleted: false } ? employee : null;
    }

    /// <summary>[Mahjar handover] Tells the given employees that an order's items wait at the desk to be handed over.</summary>
    protected virtual async Task NotifyDeliveryTaskAsync(Order order, IList<OrderItem> items, IEnumerable<Customer> recipients)
    {
        await NotifyDashboardAsync(recipients, NotificationRecipientType.Admin, 0,
            NotificationType.HandoverDeliveryStaff, order.StoreId, HANDOVER_ENTITY_NAME, order.Id,
            //[Mahjar handover] carry the order, so the employee lands on ITS rows and not the whole delivery queue
            $"/Admin/Handover/List?stageId={(int)HandoverStage.AwaitingDelivery}&keyword={Uri.EscapeDataString(order.CustomOrderNumber ?? string.Empty)}",
            async languageId => new object[] { order.CustomOrderNumber, await GetProductNamesAsync(items.Select(item => item.ProductId), languageId) });
    }

    /// <summary>
    /// An admin assigned the order's handover (the usual case: the order was already paid when they assigned it), so
    /// the employee who now owns the work is told what waits for them.
    /// </summary>
    public virtual async Task HandleEventAsync(HandoverAssignedEvent eventMessage)
    {
        var order = eventMessage?.Order;
        if (order == null)
            return;

        try
        {
            var employee = await GetAssigneeAsync(order.Id);
            if (employee == null || employee.Id != eventMessage.AssignedToCustomerId)
                return;

            var items = await _conditionCheckService.GetItemsAwaitingDeliveryAsync(order);
            if (items.Any())
            {
                await NotifyDeliveryTaskAsync(order, items, new[] { employee });

                return;
            }

            //nothing left to hand over: what this employee inherits is the RETURN of a rental already out with the
            //customer. Reassigning an order at that point must not leave them holding the work with no word of it.
            var rentals = await _conditionCheckService.GetItemsAwaitingReturnAsync(order);
            if (rentals.Any())
                await NotifyReturnTaskAsync(order, rentals, 0);
        }
        catch (Exception exc)
        {
            await _logger.ErrorAsync($"[Mahjar handover] assignment notification for order #{order.Id} failed: {exc.Message}", exc);
        }
    }

    /// <summary>
    /// A stock shipment was delivered from the order screens rather than the desk: its rented items are now out with
    /// the customer, so they enter the staff's return queue.
    /// </summary>
    public virtual async Task HandleEventAsync(ShipmentDeliveredEvent eventMessage)
    {
        var shipment = eventMessage?.Shipment;
        if (shipment == null)
            return;

        try
        {
            var order = await _orderService.GetOrderByIdAsync(shipment.OrderId);
            if (order == null || order.Deleted || order.OrderStatus == OrderStatus.Cancelled)
                return;

            //an item that went out in an earlier delivered shipment was announced then
            var alreadyOut = new HashSet<int>();
            foreach (var other in await _shipmentService.GetShipmentsByOrderIdAsync(order.Id))
            {
                if (other.Id == shipment.Id || !other.DeliveryDateUtc.HasValue)
                    continue;

                foreach (var item in await _shipmentService.GetShipmentItemsByShipmentIdAsync(other.Id))
                    alreadyOut.Add(item.OrderItemId);
            }

            var rentalItems = new List<OrderItem>();
            foreach (var shipmentItem in await _shipmentService.GetShipmentItemsByShipmentIdAsync(shipment.Id))
            {
                if (alreadyOut.Contains(shipmentItem.OrderItemId) || rentalItems.Any(item => item.Id == shipmentItem.OrderItemId))
                    continue;

                //a desk handover announces its return task itself, when it is confirmed
                var orderItem = await _orderService.GetOrderItemByIdAsync(shipmentItem.OrderItemId);
                if (orderItem?.RentalStartDateUtc == null || await _conditionCheckService.IsHandedOverByDeskAsync(orderItem.Id))
                    continue;

                rentalItems.Add(orderItem);
            }

            if (rentalItems.Any())
                await NotifyReturnTaskAsync(order, rentalItems, 0);
        }
        catch (Exception exc)
        {
            await _logger.ErrorAsync($"[Mahjar handover] return task notification for shipment #{shipment.Id} failed: {exc.Message}", exc);
        }
    }

    #endregion
}
