// [Mahjar notifications]
using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Mahjar;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Mahjar.Notifications;
using Nop.Web.Areas.Admin.Models.Notification;

namespace Nop.Web.Areas.Admin.Factories;

/// <summary>Admin/vendor notification model factory. Recipient is resolved from the current user (vendor →
/// vendor-scoped, otherwise admin), so vendor isolation is automatic. [Mahjar notifications]</summary>
public partial class NotificationModelFactory : INotificationModelFactory
{
    #region Fields

    protected readonly ICustomerService _customerService; //[Mahjar handover]
    protected readonly IDateTimeHelper _dateTimeHelper;
    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationFeedService _feedService;
    protected readonly INotificationPreferenceService _preferenceService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;
    protected readonly NotificationSettings _notificationSettings;

    #endregion

    #region Ctor

    public NotificationModelFactory(ICustomerService customerService,
        IDateTimeHelper dateTimeHelper,
        ILocalizationService localizationService,
        INotificationFeedService feedService,
        INotificationPreferenceService preferenceService,
        IStoreContext storeContext,
        IWorkContext workContext,
        NotificationSettings notificationSettings)
    {
        _customerService = customerService;
        _dateTimeHelper = dateTimeHelper;
        _localizationService = localizationService;
        _feedService = feedService;
        _preferenceService = preferenceService;
        _storeContext = storeContext;
        _workContext = workContext;
        _notificationSettings = notificationSettings;
    }

    #endregion

    #region Utilities

    /// <summary>Resolve the dashboard user's recipient identity.</summary>
    protected virtual async Task<(NotificationRecipientType type, int customerId, int vendorId, string group)> ResolveRecipientAsync()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var vendor = await _workContext.GetCurrentVendorAsync();

        //[Mahjar handover] each dashboard user has their own live group (vendor managers, admins and handover staff alike)
        if (vendor != null)
            return (NotificationRecipientType.Vendor, customer.Id, vendor.Id, $"vendor-{vendor.Id}-{customer.Id}");

        return (NotificationRecipientType.Admin, customer.Id, 0, $"admin-{customer.Id}");
    }

    /// <summary>
    /// [Mahjar handover] The notification types the dashboard user can receive: a vendor's, else by role — the admin
    /// types for an administrator, the handover types for a handover employee, both for someone who is both.
    /// </summary>
    protected virtual async Task<NotificationType[]> GetRecipientTypesAsync(NotificationRecipientType type)
    {
        if (type == NotificationRecipientType.Vendor)
            return NotificationTypeCatalog.VendorTypes;

        var customer = await _workContext.GetCurrentCustomerAsync();
        return NotificationTypeCatalog.ForDashboardUser(await _customerService.IsAdminAsync(customer),
            await _customerService.IsInCustomerRoleAsync(customer, NopCustomerDefaults.HandoverStaffRoleName));
    }

    /// <summary>Admin-area deep link for a notification's referenced entity (vendors see filtered views).</summary>
    protected static string AdminLink(Notification n)
    {
        // [Mahjar contracts] fall back to the row's own stored deep link, but only when it points into
        // the admin area — never leak a storefront URL into the admin/vendor panel navigation
        var storedAdminUrl = !string.IsNullOrEmpty(n.Url) && n.Url.StartsWith("/Admin/", StringComparison.OrdinalIgnoreCase)
            ? n.Url
            : null;

        if (n.EntityId <= 0 && n.EntityName != "ProductReview")
            return storedAdminUrl;

        //[Mahjar partner-request] an application notification belongs to the request queue, not to the partner
        //page — the whole point of opening it is to review and decide. The action behind this link works out
        //which page that means, since a reviewed or admin-created partner has no request to show
        if (n.NotificationType == NotificationType.NewVendorApplication && n.EntityName == "Vendor")
            return $"/Admin/PartnerRequest/Open/{n.EntityId}";

        return n.EntityName switch
        {
            "Order" => $"/Admin/Order/Edit/{n.EntityId}",
            "Product" => $"/Admin/Product/Edit/{n.EntityId}",
            "Vendor" => $"/Admin/Vendor/Edit/{n.EntityId}",
            "ProductReview" => "/Admin/ProductReview/List",
            "ContractRequest" => $"/Admin/ContractRequest/Edit/{n.EntityId}", // [Mahjar contracts]
            _ => storedAdminUrl
        };
    }

    #endregion

    #region Methods

    public virtual async Task<AdminNotificationBellModel> PrepareBellModelAsync()
    {
        var (type, customerId, vendorId, group) = await ResolveRecipientAsync();
        var store = await _storeContext.GetCurrentStoreAsync();

        return new AdminNotificationBellModel
        {
            UnreadCount = await _feedService.GetUnreadCountAsync(customerId, type, vendorId, store.Id),
            Group = group,
            IsVendor = type == NotificationRecipientType.Vendor
        };
    }

    public virtual async Task<NotificationListModel> PrepareListModelAsync(int pageIndex = 0)
    {
        var (type, customerId, vendorId, _) = await ResolveRecipientAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var pageSize = _notificationSettings.FeedPageSize > 0 ? _notificationSettings.FeedPageSize : 20;

        var notifications = await _feedService.GetNotificationsAsync(customerId, type, vendorId, store.Id, null, pageIndex, pageSize);

        var model = new NotificationListModel
        {
            PageIndex = notifications.PageIndex,
            TotalPages = notifications.TotalPages,
            HasPreviousPage = notifications.HasPreviousPage,
            HasNextPage = notifications.HasNextPage,
            UnreadCount = await _feedService.GetUnreadCountAsync(customerId, type, vendorId, store.Id)
        };

        foreach (var n in notifications)
            model.Items.Add(new NotificationModel
            {
                Id = n.Id,
                Title = n.Title,
                Body = n.Body,
                Url = AdminLink(n),
                IsRead = n.IsRead,
                TypeLabel = await _localizationService.GetResourceAsync(NotificationTypeCatalog.TitleResourceKey(n.NotificationType)),
                CreatedOnStr = (await _dateTimeHelper.ConvertToUserTimeAsync(n.CreatedOnUtc, DateTimeKind.Utc)).ToString("g")
            });

        return model;
    }

    public virtual async Task<NotificationPreferenceModel> PreparePreferenceModelAsync()
    {
        var (type, customerId, _, _) = await ResolveRecipientAsync();
        var saved = (await _preferenceService.GetPreferencesAsync(customerId))
            .ToDictionary(p => p.NotificationTypeId, p => p);

        var model = new NotificationPreferenceModel();
        foreach (var nType in await GetRecipientTypesAsync(type))
        {
            saved.TryGetValue((int)nType, out var row);
            model.Items.Add(new NotificationPreferenceItemModel
            {
                TypeId = (int)nType,
                TypeLabel = await _localizationService.GetResourceAsync(NotificationTypeCatalog.TitleResourceKey(nType)),
                InAppEnabled = row?.InAppEnabled ?? true,
                EmailEnabled = row?.EmailEnabled ?? true
            });
        }

        return model;
    }

    public virtual async Task SavePreferencesAsync(NotificationPreferenceModel model)
    {
        var (type, customerId, _, _) = await ResolveRecipientAsync();
        var allowed = (await GetRecipientTypesAsync(type)).Select(t => (int)t).ToHashSet();

        foreach (var item in model.Items)
        {
            if (!allowed.Contains(item.TypeId))
                continue;
            await _preferenceService.SetPreferenceAsync(customerId, (NotificationType)item.TypeId, item.InAppEnabled, item.EmailEnabled);
        }
    }

    #endregion
}
