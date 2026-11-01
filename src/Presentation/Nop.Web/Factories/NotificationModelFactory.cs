// [Mahjar notifications]
using System;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Mahjar;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Mahjar.Notifications;
using Nop.Web.Models.Notification;

namespace Nop.Web.Factories;

/// <summary>Storefront (buyer) notification model factory. [Mahjar notifications]</summary>
public partial class NotificationModelFactory : INotificationModelFactory
{
    #region Fields

    protected readonly IDateTimeHelper _dateTimeHelper;
    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationFeedService _feedService;
    protected readonly INotificationPreferenceService _preferenceService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;
    protected readonly NotificationSettings _notificationSettings;

    #endregion

    #region Ctor

    public NotificationModelFactory(IDateTimeHelper dateTimeHelper,
        ILocalizationService localizationService,
        INotificationFeedService feedService,
        INotificationPreferenceService preferenceService,
        IStoreContext storeContext,
        IWorkContext workContext,
        NotificationSettings notificationSettings)
    {
        _dateTimeHelper = dateTimeHelper;
        _localizationService = localizationService;
        _feedService = feedService;
        _preferenceService = preferenceService;
        _storeContext = storeContext;
        _workContext = workContext;
        _notificationSettings = notificationSettings;
    }

    #endregion

    #region Methods

    public virtual async Task<NotificationBellModel> PrepareBellModelAsync()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();

        return new NotificationBellModel
        {
            CurrentCustomerId = customer.Id,
            UnreadCount = await _feedService.GetUnreadCountAsync(customer.Id, NotificationRecipientType.Buyer, 0, store.Id),
            Group = $"customer-{customer.Id}"
        };
    }

    public virtual async Task<NotificationListModel> PrepareListModelAsync(int pageIndex = 0)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var pageSize = _notificationSettings.FeedPageSize > 0 ? _notificationSettings.FeedPageSize : 20;

        var notifications = await _feedService.GetNotificationsAsync(customer.Id, NotificationRecipientType.Buyer,
            0, store.Id, null, pageIndex, pageSize);

        var model = new NotificationListModel
        {
            PageIndex = notifications.PageIndex,
            TotalPages = notifications.TotalPages,
            TotalItems = notifications.TotalCount,
            HasPreviousPage = notifications.HasPreviousPage,
            HasNextPage = notifications.HasNextPage,
            UnreadCount = await _feedService.GetUnreadCountAsync(customer.Id, NotificationRecipientType.Buyer, 0, store.Id)
        };

        foreach (var n in notifications)
            model.Items.Add(new NotificationItemModel
            {
                Id = n.Id,
                Title = n.Title,
                Body = n.Body,
                Url = n.Url,
                IsRead = n.IsRead,
                TypeId = n.NotificationTypeId,
                TypeLabel = await _localizationService.GetResourceAsync(
                    NotificationTypeCatalog.TitleResourceKey(n.NotificationType)),
                CreatedOnStr = (await _dateTimeHelper.ConvertToUserTimeAsync(n.CreatedOnUtc, DateTimeKind.Utc)).ToString("g")
            });

        return model;
    }

    public virtual async Task<NotificationPreferenceModel> PreparePreferenceModelAsync()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var saved = (await _preferenceService.GetPreferencesAsync(customer.Id))
            .ToDictionary(p => p.NotificationTypeId, p => p);

        var model = new NotificationPreferenceModel();
        foreach (var type in NotificationTypeCatalog.BuyerTypes)
        {
            saved.TryGetValue((int)type, out var row);
            model.Items.Add(new NotificationPreferenceItemModel
            {
                TypeId = (int)type,
                TypeLabel = await _localizationService.GetResourceAsync(NotificationTypeCatalog.TitleResourceKey(type)),
                InAppEnabled = row?.InAppEnabled ?? true,
                EmailEnabled = row?.EmailEnabled ?? true
            });
        }

        return model;
    }

    public virtual async Task SavePreferencesAsync(NotificationPreferenceModel model)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var buyerTypeIds = NotificationTypeCatalog.BuyerTypes.Select(t => (int)t).ToHashSet();

        foreach (var item in model.Items)
        {
            if (!buyerTypeIds.Contains(item.TypeId))
                continue;
            await _preferenceService.SetPreferenceAsync(customer.Id, (NotificationType)item.TypeId,
                item.InAppEnabled, item.EmailEnabled);
        }
    }

    #endregion
}
