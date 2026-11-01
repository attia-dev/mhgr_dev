// [Mahjar contracts]
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Mahjar;
using Nop.Core.Domain.Orders;
using Nop.Services.Customers;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Mahjar.Notifications;
using Nop.Services.Messages;
using Nop.Services.Orders;

namespace Nop.Services.Mahjar.Contracts;

/// <summary>
/// Subscribes to the contract lifecycle events and writes the matching in-app notification rows (buyer in
/// the request's language, admins each in their own) plus the matching workflow email. Also completes an
/// Approved request once the order it unlocked is placed. Notification failures are logged and never break
/// the originating flow. Auto-discovered by ITypeFinder. [Mahjar contracts]
/// </summary>
public partial class ContractEventConsumer :
    IConsumer<ContractRequestedEvent>,
    IConsumer<ContractSentEvent>,
    IConsumer<ContractSignedEvent>,
    IConsumer<ContractApprovedEvent>,
    IConsumer<ContractRejectedEvent>,
    IConsumer<OrderPlacedEvent>
{
    #region Fields

    protected readonly ContractSettings _contractSettings;
    protected readonly IContractService _contractService;
    protected readonly ICustomerService _customerService;
    protected readonly ILanguageService _languageService;
    protected readonly ILocalizationService _localizationService;
    protected readonly ILogger _logger;
    protected readonly INotificationFeedService _feedService;
    protected readonly IOrderService _orderService;
    protected readonly IWorkContext _workContext;
    protected readonly IWorkflowMessageService _workflowMessageService;
    protected readonly LocalizationSettings _localizationSettings;

    #endregion

    #region Ctor

    public ContractEventConsumer(ContractSettings contractSettings,
        IContractService contractService,
        ICustomerService customerService,
        ILanguageService languageService,
        ILocalizationService localizationService,
        ILogger logger,
        INotificationFeedService feedService,
        IOrderService orderService,
        IWorkContext workContext,
        IWorkflowMessageService workflowMessageService,
        LocalizationSettings localizationSettings)
    {
        _contractSettings = contractSettings;
        _contractService = contractService;
        _customerService = customerService;
        _languageService = languageService;
        _localizationService = localizationService;
        _logger = logger;
        _feedService = feedService;
        _orderService = orderService;
        _workContext = workContext;
        _workflowMessageService = workflowMessageService;
        _localizationSettings = localizationSettings;
    }

    #endregion

    #region Utilities

    /// <summary>The customer-facing contract number of a request.</summary>
    protected static string GetContractNumber(ContractRequest request)
    {
        return $"MHGR-C-{request.Id:D5}";
    }

    /// <summary>
    /// Resolve a usable, published language id: preferred → current working language → admin default → first.
    /// (Same fallback chain as the notifications feature's event consumer.)
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
        NotificationRecipientType recipientType, NotificationType type, int languageId,
        int storeId, int entityId, string url, params object[] bodyArgs)
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
            RecipientVendorId = 0,
            RecipientType = recipientType,
            NotificationType = type,
            Title = title,
            Body = body,
            EntityName = "ContractRequest",
            EntityId = entityId,
            Url = url,
            StoreId = storeId,
            CreatedOnUtc = DateTime.UtcNow
        };
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

    /// <summary>In-app rows for every admin (each in their own language) + one admin-facing body arg.</summary>
    protected virtual async Task NotifyAdminsAsync(ContractRequest request, NotificationType type, params object[] bodyArgs)
    {
        var url = $"/Admin/ContractRequest/Edit/{request.Id}";
        var rows = new List<Notification>();
        foreach (var admin in await ResolveAdminRecipientsAsync())
        {
            var lang = await EnsureLanguageAsync(admin.LanguageId ?? request.LanguageId, request.StoreId);
            rows.Add(await BuildAsync(admin.Id, NotificationRecipientType.Admin, type, lang,
                request.StoreId, request.Id, url, bodyArgs));
        }

        await _feedService.CreateNotificationsAsync(rows);
    }

    /// <summary>In-app row for the buyer, in the request's (= the customer's) language.</summary>
    protected virtual async Task NotifyBuyerAsync(ContractRequest request, NotificationType type, params object[] bodyArgs)
    {
        var lang = await EnsureLanguageAsync(request.LanguageId, request.StoreId);
        var notification = await BuildAsync(request.CustomerId, NotificationRecipientType.Buyer, type, lang,
            request.StoreId, request.Id, "/customer/contracts", bodyArgs);

        await _feedService.CreateNotificationAsync(notification);
    }

    /// <summary>A display name for the requesting customer (full name, falling back to email).</summary>
    protected virtual async Task<string> GetCustomerDisplayNameAsync(Customer customer)
    {
        if (customer == null)
            return string.Empty;

        var fullName = await _customerService.GetCustomerFullNameAsync(customer);
        return string.IsNullOrWhiteSpace(fullName) ? customer.Email : fullName;
    }

    #endregion

    #region Contract events

    /// <summary>A customer submitted a new contract request → notify admins (feed + email).</summary>
    public virtual async Task HandleEventAsync(ContractRequestedEvent eventMessage)
    {
        var request = eventMessage.ContractRequest;
        if (request == null)
            return;

        try
        {
            var customer = await _customerService.GetCustomerByIdAsync(request.CustomerId);
            var customerName = await GetCustomerDisplayNameAsync(customer);

            await NotifyAdminsAsync(request, NotificationType.ContractRequestedAdmin, GetContractNumber(request), customerName);

            if (customer != null)
                await _workflowMessageService.SendContractRequestedStoreOwnerNotificationAsync(request, customer, request.LanguageId);
        }
        catch (Exception exception)
        {
            await _logger.ErrorAsync($"[Mahjar contracts] Failed to send contract-requested notifications for request #{request.Id}", exception);
        }
    }

    /// <summary>The admin sent the contract for signing → notify the customer (feed + email with link).</summary>
    public virtual async Task HandleEventAsync(ContractSentEvent eventMessage)
    {
        var request = eventMessage.ContractRequest;
        if (request == null)
            return;

        try
        {
            await NotifyBuyerAsync(request, NotificationType.ContractSentBuyer, GetContractNumber(request));

            var customer = await _customerService.GetCustomerByIdAsync(request.CustomerId);
            if (customer != null)
                await _workflowMessageService.SendContractSentCustomerNotificationAsync(request, customer, request.LanguageId);
        }
        catch (Exception exception)
        {
            await _logger.ErrorAsync($"[Mahjar contracts] Failed to send contract-sent notifications for request #{request.Id}", exception);
        }
    }

    /// <summary>The customer signed the contract → notify admins (feed + email).</summary>
    public virtual async Task HandleEventAsync(ContractSignedEvent eventMessage)
    {
        var request = eventMessage.ContractRequest;
        if (request == null)
            return;

        try
        {
            var customer = await _customerService.GetCustomerByIdAsync(request.CustomerId);
            var signerName = !string.IsNullOrWhiteSpace(request.SignedName)
                ? request.SignedName
                : await GetCustomerDisplayNameAsync(customer);

            await NotifyAdminsAsync(request, NotificationType.ContractSignedAdmin, GetContractNumber(request), signerName);

            if (customer != null)
                await _workflowMessageService.SendContractSignedStoreOwnerNotificationAsync(request, customer, request.LanguageId);
        }
        catch (Exception exception)
        {
            await _logger.ErrorAsync($"[Mahjar contracts] Failed to send contract-signed notifications for request #{request.Id}", exception);
        }
    }

    /// <summary>The admin approved the contract → notify the customer (feed + email).</summary>
    public virtual async Task HandleEventAsync(ContractApprovedEvent eventMessage)
    {
        var request = eventMessage.ContractRequest;
        if (request == null)
            return;

        try
        {
            await NotifyBuyerAsync(request, NotificationType.ContractApprovedBuyer, GetContractNumber(request));

            var customer = await _customerService.GetCustomerByIdAsync(request.CustomerId);
            if (customer != null)
                await _workflowMessageService.SendContractApprovedCustomerNotificationAsync(request, customer, request.LanguageId);
        }
        catch (Exception exception)
        {
            await _logger.ErrorAsync($"[Mahjar contracts] Failed to send contract-approved notifications for request #{request.Id}", exception);
        }
    }

    /// <summary>The admin rejected the contract request → notify the customer (feed + email, with reason).</summary>
    public virtual async Task HandleEventAsync(ContractRejectedEvent eventMessage)
    {
        var request = eventMessage.ContractRequest;
        if (request == null)
            return;

        try
        {
            await NotifyBuyerAsync(request, NotificationType.ContractRejectedBuyer, GetContractNumber(request),
                request.RejectionReason ?? string.Empty);

            var customer = await _customerService.GetCustomerByIdAsync(request.CustomerId);
            if (customer != null)
                await _workflowMessageService.SendContractRejectedCustomerNotificationAsync(request, customer, request.LanguageId);
        }
        catch (Exception exception)
        {
            await _logger.ErrorAsync($"[Mahjar contracts] Failed to send contract-rejected notifications for request #{request.Id}", exception);
        }
    }

    #endregion

    #region Order events

    /// <summary>An order was placed → complete the Approved contract request it was gated on.</summary>
    public virtual async Task HandleEventAsync(OrderPlacedEvent eventMessage)
    {
        var order = eventMessage.Order;
        if (order == null)
            return;

        try
        {
            if (!_contractSettings.Enabled)
                return;

            //complete only the request this order actually corresponds to: the newest Approved request
            //whose frozen cart hash matches the hash of the order's own items. Any other order of the
            //same customer (a side cart, a recurring order, ...) must leave the request untouched.
            var orderItems = await _orderService.GetOrderItemsAsync(order.Id);
            if (!orderItems.Any())
                return;

            var orderHash = await _contractService.ComputeOrderHashAsync(orderItems);

            var approvedRequests = await _contractService.SearchRequestsAsync(customerId: order.CustomerId,
                storeId: order.StoreId, status: ContractRequestStatus.Approved);
            var request = approvedRequests
                .FirstOrDefault(r => string.Equals(r.CartHash, orderHash, StringComparison.Ordinal));
            if (request != null)
                await _contractService.MarkCompletedAsync(request, order.Id);
        }
        catch (Exception exception)
        {
            await _logger.ErrorAsync($"[Mahjar contracts] Failed to complete the contract request for order #{order.Id}", exception);
        }
    }

    #endregion
}
