// [Mahjar notifications]
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Mahjar;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Vendors;
using Nop.Core.Events;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Mahjar.Handover;
using Nop.Services.Orders;
using Nop.Services.Vendors;

namespace Nop.Services.Mahjar.Notifications;

/// <summary>
/// Subscribes to the domain events nopCommerce already publishes (and already emails on) and writes a
/// persisted in-app notification row for each relevant recipient — buyer, vendor(s) and admin(s). It does NOT
/// edit OrderProcessingService / WorkflowMessageService: the email path is untouched, we only add rows.
/// Auto-discovered by ITypeFinder (one registration per closed IConsumer interface). [Mahjar notifications]
/// </summary>
public partial class NotificationEventConsumer :
    IConsumer<OrderPlacedEvent>,
    IConsumer<OrderPaidEvent>,
    IConsumer<OrderStatusChangedEvent>,
    IConsumer<OrderRefundedEvent>,
    IConsumer<ShipmentSentEvent>,
    IConsumer<ShipmentDeliveredEvent>,
    IConsumer<ShipmentReadyForPickupEvent>,
    IConsumer<ProductReviewApprovedEvent>,
    IConsumer<CustomerRegisteredEvent>,
    IConsumer<EntityInsertedEvent<Vendor>>,
    IConsumer<VendorApprovedEvent>,
    IConsumer<VendorApplicationRejectedEvent>, //[Mahjar partner-request]
    IConsumer<VendorDeactivatedEvent>
{
    #region Fields

    protected readonly IConditionCheckService _conditionCheckService; //[Mahjar handover]
    protected readonly ICustomerService _customerService;
    protected readonly ILanguageService _languageService;
    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationFeedService _feedService;
    protected readonly IOrderService _orderService;
    protected readonly IProductService _productService;
    protected readonly IStoreContext _storeContext;
    protected readonly IVendorService _vendorService;
    protected readonly IWorkContext _workContext;
    protected readonly LocalizationSettings _localizationSettings;
    protected readonly NotificationSettings _notificationSettings;

    #endregion

    #region Ctor

    public NotificationEventConsumer(IConditionCheckService conditionCheckService,
        ICustomerService customerService,
        ILanguageService languageService,
        ILocalizationService localizationService,
        INotificationFeedService feedService,
        IOrderService orderService,
        IProductService productService,
        IStoreContext storeContext,
        IVendorService vendorService,
        IWorkContext workContext,
        LocalizationSettings localizationSettings,
        NotificationSettings notificationSettings)
    {
        _conditionCheckService = conditionCheckService;
        _customerService = customerService;
        _languageService = languageService;
        _localizationService = localizationService;
        _feedService = feedService;
        _orderService = orderService;
        _productService = productService;
        _storeContext = storeContext;
        _vendorService = vendorService;
        _workContext = workContext;
        _localizationSettings = localizationSettings;
        _notificationSettings = notificationSettings;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Resolve a usable, published language id: preferred → the current working language (e.g. the buyer's at
    /// checkout, or the admin's when approving a vendor — both Arabic on this store) → admin default → first.
    /// This is what keeps notifications in the right language when the customer's saved LanguageId is null.
    /// </summary>
    protected virtual async Task<int> EnsureLanguageAsync(int? preferredLanguageId, int storeId)
    {
        if (preferredLanguageId is > 0)
        {
            var lang = await _languageService.GetLanguageByIdAsync(preferredLanguageId.Value);
            if (lang is { Published: true })
                return lang.Id;
        }

        var working = await _workContext.GetWorkingLanguageAsync();
        if (working is { Published: true })
            return working.Id;

        if (_localizationSettings.DefaultAdminLanguageId > 0)
        {
            var adminLang = await _languageService.GetLanguageByIdAsync(_localizationSettings.DefaultAdminLanguageId);
            if (adminLang is { Published: true })
                return adminLang.Id;
        }

        var languages = await _languageService.GetAllLanguagesAsync(storeId: storeId);
        return languages.FirstOrDefault()?.Id ?? 0;
    }

    /// <summary>Compose a notification row from the localized title/body resources for its type.</summary>
    protected virtual async Task<Notification> BuildAsync(int recipientCustomerId,
        NotificationRecipientType recipientType, int recipientVendorId, NotificationType type, int languageId,
        int storeId, string entityName, int entityId, string url, params object[] bodyArgs)
    {
        var key = "mahjar.notifications.type." + type.ToString().ToLowerInvariant();
        var title = await _localizationService.GetResourceAsync(key + ".title", languageId);
        var bodyFormat = await _localizationService.GetResourceAsync(key + ".body", languageId);
        var body = bodyArgs is { Length: > 0 } && !string.IsNullOrEmpty(bodyFormat)
            ? string.Format(bodyFormat, bodyArgs)
            : bodyFormat;

        return new Notification
        {
            RecipientCustomerId = recipientCustomerId,
            RecipientVendorId = recipientVendorId,
            RecipientType = recipientType,
            NotificationType = type,
            Title = title,
            Body = body,
            EntityName = entityName,
            EntityId = entityId,
            Url = url,
            StoreId = storeId,
            CreatedOnUtc = DateTime.UtcNow
        };
    }

    /// <summary>(manager customer, vendorId) pairs for every vendor with a product in the order.</summary>
    protected virtual async Task<IList<(Customer customer, int vendorId)>> ResolveVendorRecipientsAsync(Order order)
    {
        var result = new List<(Customer, int)>();

        var orderItems = await _orderService.GetOrderItemsAsync(order.Id);
        var productIds = orderItems.Select(oi => oi.ProductId).Distinct().ToArray();
        if (productIds.Length == 0)
            return result;

        var vendors = await _vendorService.GetVendorsByProductIdsAsync(productIds);
        foreach (var vendor in vendors)
        {
            var managers = await _customerService.GetAllCustomersAsync(vendorId: vendor.Id);
            foreach (var manager in managers)
                result.Add((manager, vendor.Id));
        }

        return result;
    }

    /// <summary>Admin/store-owner customers.</summary>
    protected virtual async Task<IList<Customer>> ResolveAdminRecipientsAsync()
    {
        var adminRole = await _customerService.GetCustomerRoleBySystemNameAsync(NopCustomerDefaults.AdministratorsRoleName);
        if (adminRole == null)
            return new List<Customer>();

        var admins = await _customerService.GetAllCustomersAsync(customerRoleIds: new[] { adminRole.Id });
        return admins.ToList();
    }

    /// <summary>
    /// Buyer language for an order. Prefers the language recorded ON the order (the buyer's language at order
    /// time) — the customer's saved LanguageId is frequently null because the storefront language can be
    /// driven by cookie/URL rather than persisted on the customer. Falls back through EnsureLanguageAsync.
    /// </summary>
    protected virtual async Task<int> BuyerLanguageAsync(Order order)
    {
        var preferred = order.CustomerLanguageId > 0
            ? order.CustomerLanguageId
            : (await _customerService.GetCustomerByIdAsync(order.CustomerId))?.LanguageId;
        return await EnsureLanguageAsync(preferred, order.StoreId);
    }

    #endregion

    #region Order events

    public virtual async Task HandleEventAsync(OrderPlacedEvent eventMessage)
    {
        var order = eventMessage.Order;
        var url = $"orderdetails/{order.Id}";
        var rows = new List<Notification>();

        //buyer
        var buyerLang = await BuyerLanguageAsync(order);
        rows.Add(await BuildAsync(order.CustomerId, NotificationRecipientType.Buyer, 0,
            NotificationType.OrderPlacedBuyer, buyerLang, order.StoreId, "Order", order.Id, url, order.CustomOrderNumber));

        //vendors — each in their own language
        foreach (var (customer, vendorId) in await ResolveVendorRecipientsAsync(order))
        {
            var lang = await EnsureLanguageAsync(customer.LanguageId ?? order.CustomerLanguageId, order.StoreId);
            rows.Add(await BuildAsync(customer.Id, NotificationRecipientType.Vendor, vendorId,
                NotificationType.OrderPlacedVendor, lang, order.StoreId, "Order", order.Id, url, order.CustomOrderNumber));
        }

        //admins (gated) — each in their own language
        if (_notificationSettings.NotifyAdminsOnNewOrder)
            foreach (var admin in await ResolveAdminRecipientsAsync())
            {
                var lang = await EnsureLanguageAsync(admin.LanguageId ?? order.CustomerLanguageId, order.StoreId);
                rows.Add(await BuildAsync(admin.Id, NotificationRecipientType.Admin, 0,
                    NotificationType.OrderPlacedAdmin, lang, order.StoreId, "Order", order.Id, url, order.CustomOrderNumber));
            }

        await _feedService.CreateNotificationsAsync(rows);
    }

    public virtual async Task HandleEventAsync(OrderPaidEvent eventMessage)
    {
        var order = eventMessage.Order;
        var url = $"orderdetails/{order.Id}";
        var rows = new List<Notification>();

        var buyerLang = await BuyerLanguageAsync(order);
        rows.Add(await BuildAsync(order.CustomerId, NotificationRecipientType.Buyer, 0,
            NotificationType.OrderPaidBuyer, buyerLang, order.StoreId, "Order", order.Id, url, order.CustomOrderNumber));

        foreach (var (customer, vendorId) in await ResolveVendorRecipientsAsync(order))
        {
            var lang = await EnsureLanguageAsync(customer.LanguageId ?? order.CustomerLanguageId, order.StoreId);
            rows.Add(await BuildAsync(customer.Id, NotificationRecipientType.Vendor, vendorId,
                NotificationType.OrderPaidVendor, lang, order.StoreId, "Order", order.Id, url, order.CustomOrderNumber));
        }

        await _feedService.CreateNotificationsAsync(rows);
    }

    public virtual async Task HandleEventAsync(OrderStatusChangedEvent eventMessage)
    {
        var order = eventMessage.Order;
        //skip the initial Pending → noise; only meaningful transitions
        if (order.OrderStatus == eventMessage.PreviousOrderStatus)
            return;

        var buyerLang = await BuyerLanguageAsync(order);
        var statusText = await _localizationService.GetLocalizedEnumAsync(order.OrderStatus, buyerLang);
        var notification = await BuildAsync(order.CustomerId, NotificationRecipientType.Buyer, 0,
            NotificationType.OrderStatusChanged, buyerLang, order.StoreId, "Order", order.Id,
            $"orderdetails/{order.Id}", order.CustomOrderNumber, statusText);

        await _feedService.CreateNotificationAsync(notification);
    }

    public virtual async Task HandleEventAsync(OrderRefundedEvent eventMessage)
    {
        var order = eventMessage.Order;
        var buyerLang = await BuyerLanguageAsync(order);
        var amount = $"{eventMessage.Amount:N2} {order.CustomerCurrencyCode}";
        var notification = await BuildAsync(order.CustomerId, NotificationRecipientType.Buyer, 0,
            NotificationType.OrderRefunded, buyerLang, order.StoreId, "Order", order.Id,
            $"orderdetails/{order.Id}", order.CustomOrderNumber, amount);

        await _feedService.CreateNotificationAsync(notification);
    }

    #endregion

    #region Shipment events

    protected virtual async Task HandleShipmentAsync(Shipment shipment, NotificationType type)
    {
        var order = await _orderService.GetOrderByIdAsync(shipment.OrderId);
        if (order == null)
            return;

        //[Mahjar handover] the handover desk ships and delivers its own shipments through this stock path, and its
        //confirmation already told the buyer (with the documented condition), so these generic rows would only repeat it
        if (await _conditionCheckService.IsShipmentHandedOverByDeskAsync(shipment.Id))
            return;

        var buyerLang = await BuyerLanguageAsync(order);
        var notification = await BuildAsync(order.CustomerId, NotificationRecipientType.Buyer, 0,
            type, buyerLang, order.StoreId, "Order", order.Id, $"orderdetails/{order.Id}", order.CustomOrderNumber);

        await _feedService.CreateNotificationAsync(notification);
    }

    public virtual Task HandleEventAsync(ShipmentSentEvent eventMessage)
        => HandleShipmentAsync(eventMessage.Shipment, NotificationType.OrderShipped);

    public virtual Task HandleEventAsync(ShipmentDeliveredEvent eventMessage)
        => HandleShipmentAsync(eventMessage.Shipment, NotificationType.OrderDelivered);

    public virtual Task HandleEventAsync(ShipmentReadyForPickupEvent eventMessage)
        => HandleShipmentAsync(eventMessage.Shipment, NotificationType.OrderReadyForPickup);

    #endregion

    #region Review + customer events

    public virtual async Task HandleEventAsync(ProductReviewApprovedEvent eventMessage)
    {
        var review = eventMessage.ProductReview;
        var product = await _productService.GetProductByIdAsync(review.ProductId);
        if (product == null)
            return;

        var vendor = await _vendorService.GetVendorByProductIdAsync(product.Id);
        if (vendor == null)
            return;

        var managers = await _customerService.GetAllCustomersAsync(vendorId: vendor.Id);
        var rows = new List<Notification>();
        foreach (var manager in managers)
        {
            var lang = await EnsureLanguageAsync(manager.LanguageId, review.StoreId);
            rows.Add(await BuildAsync(manager.Id, NotificationRecipientType.Vendor, vendor.Id,
                NotificationType.ReviewApprovedVendor, lang, review.StoreId, "Product", product.Id, null, product.Name));
        }

        await _feedService.CreateNotificationsAsync(rows);
    }

    public virtual async Task HandleEventAsync(CustomerRegisteredEvent eventMessage)
    {
        var customer = eventMessage.Customer;
        var store = await _storeContext.GetCurrentStoreAsync();
        var lang = await EnsureLanguageAsync(customer.LanguageId, store.Id);
        var notification = await BuildAsync(customer.Id, NotificationRecipientType.Buyer, 0,
            NotificationType.Welcome, lang, store.Id, "Customer", customer.Id, null);

        await _feedService.CreateNotificationAsync(notification);
    }

    #endregion

    #region Become-a-seller (vendor application + approval)

    /// <summary>A new vendor row inserted by the "become a seller" apply flow (inactive) → notify admins.</summary>
    public virtual async Task HandleEventAsync(EntityInsertedEvent<Vendor> eventMessage)
    {
        var vendor = eventMessage.Entity;
        //only seller APPLICATIONS (created inactive); an admin creating an already-active vendor isn't an application
        if (vendor == null || vendor.Active)
            return;

        var store = await _storeContext.GetCurrentStoreAsync();
        var rows = new List<Notification>();
        foreach (var admin in await ResolveAdminRecipientsAsync())
        {
            var lang = await EnsureLanguageAsync(admin.LanguageId, store.Id);
            rows.Add(await BuildAsync(admin.Id, NotificationRecipientType.Admin, 0,
                NotificationType.NewVendorApplication, lang, store.Id, "Vendor", vendor.Id, null, vendor.Name));
        }

        await _feedService.CreateNotificationsAsync(rows);
    }

    /// <summary>Admin approved (activated) a pending vendor → notify the applicant customer(s).</summary>
    public virtual async Task HandleEventAsync(VendorApprovedEvent eventMessage)
    {
        var vendor = eventMessage.Vendor;
        if (vendor == null)
            return;

        var store = await _storeContext.GetCurrentStoreAsync();
        var applicants = await _customerService.GetAllCustomersAsync(vendorId: vendor.Id);
        var rows = new List<Notification>();
        foreach (var customer in applicants)
        {
            var lang = await EnsureLanguageAsync(customer.LanguageId, store.Id);
            rows.Add(await BuildAsync(customer.Id, NotificationRecipientType.Buyer, 0,
                NotificationType.SellerApproved, lang, store.Id, "Vendor", vendor.Id, null, vendor.Name));
        }

        await _feedService.CreateNotificationsAsync(rows);
    }

    /// <summary>
    /// [Mahjar partner-request] The store owner refused a partner application → tell the applicant, with the
    /// reason, so they know what to correct before applying again.
    ///
    /// The recipient is resolved through <see cref="Vendor.ApplicationCustomerId"/>, not through
    /// <c>Customer.VendorId</c> as the two handlers around this one do: a rejection releases the customer so
    /// they can apply again, so by the time this runs nobody points at the vendor any more.
    /// </summary>
    public virtual async Task HandleEventAsync(VendorApplicationRejectedEvent eventMessage)
    {
        var vendor = eventMessage.Vendor;
        if (vendor == null || vendor.ApplicationCustomerId <= 0)
            return;

        var customer = await _customerService.GetCustomerByIdAsync(vendor.ApplicationCustomerId);
        if (customer == null || customer.Deleted)
            return;

        var store = await _storeContext.GetCurrentStoreAsync();
        var lang = await EnsureLanguageAsync(customer.LanguageId, store.Id);

        //the reason is part of the message. The review screen requires one, but an application rejected before
        //that rule existed has none, and "because:" followed by nothing reads like a bug
        var reason = string.IsNullOrWhiteSpace(vendor.ApplicationRejectionReason)
            ? await _localizationService.GetResourceAsync("mahjar.notifications.type.sellerrejected.noreason", lang)
            : vendor.ApplicationRejectionReason.Trim();

        var row = await BuildAsync(customer.Id, NotificationRecipientType.Buyer, 0,
            NotificationType.SellerRejected, lang, store.Id, "Vendor", vendor.Id, null, vendor.Name, reason);

        await _feedService.CreateNotificationsAsync(new List<Notification> { row });
    }

    /// <summary>Admin deactivated (suspended) a vendor → notify the seller customer(s). Mirror of approval above.</summary>
    public virtual async Task HandleEventAsync(VendorDeactivatedEvent eventMessage)
    {
        var vendor = eventMessage.Vendor;
        if (vendor == null)
            return;

        var store = await _storeContext.GetCurrentStoreAsync();
        var owners = await _customerService.GetAllCustomersAsync(vendorId: vendor.Id);
        var rows = new List<Notification>();
        foreach (var customer in owners)
        {
            var lang = await EnsureLanguageAsync(customer.LanguageId, store.Id);
            rows.Add(await BuildAsync(customer.Id, NotificationRecipientType.Buyer, 0,
                NotificationType.SellerDeactivated, lang, store.Id, "Vendor", vendor.Id, null, vendor.Name));
        }

        await _feedService.CreateNotificationsAsync(rows);
    }

    #endregion
}
