// [Mahjar notifications]
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Mahjar;
using Nop.Data;

namespace Nop.Services.Mahjar.Notifications;

/// <summary>
/// The in-app notification feed service. Inserts go through a per-type opt-in check; after a successful
/// insert the recipient's recomputed unread count + the row are pushed via <see cref="INotificationRealtimeNotifier"/>
/// (failures there never break the caller). [Mahjar notifications]
/// </summary>
public partial class NotificationFeedService : INotificationFeedService
{
    #region Fields

    protected readonly IRepository<Notification> _notificationRepository;
    protected readonly INotificationPreferenceService _preferenceService;
    protected readonly INotificationRealtimeNotifier _realtimeNotifier;

    #endregion

    #region Ctor

    public NotificationFeedService(IRepository<Notification> notificationRepository,
        INotificationPreferenceService preferenceService,
        INotificationRealtimeNotifier realtimeNotifier)
    {
        _notificationRepository = notificationRepository;
        _preferenceService = preferenceService;
        _realtimeNotifier = realtimeNotifier;
    }

    #endregion

    #region Utilities

    /// <summary>Apply the recipient filter shared by list + unread-count queries.</summary>
    protected virtual IQueryable<Notification> FilterByRecipient(IQueryable<Notification> query,
        int recipientCustomerId, NotificationRecipientType recipientType, int recipientVendorId, int storeId)
    {
        var recipientTypeId = (int)recipientType;
        query = query.Where(n => n.RecipientCustomerId == recipientCustomerId && n.RecipientTypeId == recipientTypeId);

        if (recipientType == NotificationRecipientType.Vendor && recipientVendorId > 0)
            query = query.Where(n => n.RecipientVendorId == recipientVendorId);

        if (storeId > 0)
            query = query.Where(n => n.StoreId == storeId);

        return query;
    }

    #endregion

    #region Methods

    public virtual async Task<Notification> CreateNotificationAsync(Notification notification, bool broadcast = true)
    {
        ArgumentNullException.ThrowIfNull(notification);

        //per-type opt-in check — skip the in-app row when the recipient opted out
        if (!await _preferenceService.IsInAppEnabledAsync(notification.RecipientCustomerId, notification.NotificationType))
            return null;

        notification.IsRead = false;
        notification.ReadOnUtc = null;
        if (notification.CreatedOnUtc == default)
            notification.CreatedOnUtc = DateTime.UtcNow;

        await _notificationRepository.InsertAsync(notification);

        if (broadcast)
        {
            try
            {
                var unread = await GetUnreadCountAsync(notification.RecipientCustomerId, notification.RecipientType,
                    notification.RecipientVendorId, notification.StoreId);
                await _realtimeNotifier.NotifyRecipientAsync(notification, unread);
            }
            catch
            {
                //a real-time push failure must never break the originating event or the email path
            }
        }

        return notification;
    }

    public virtual async Task CreateNotificationsAsync(IEnumerable<Notification> notifications)
    {
        if (notifications == null)
            return;

        foreach (var notification in notifications)
            await CreateNotificationAsync(notification);
    }

    public virtual async Task<IPagedList<Notification>> GetNotificationsAsync(int recipientCustomerId,
        NotificationRecipientType recipientType, int recipientVendorId = 0, int storeId = 0,
        bool? unreadOnly = null, int pageIndex = 0, int pageSize = 20)
    {
        return await _notificationRepository.GetAllPagedAsync(query =>
        {
            query = FilterByRecipient(query, recipientCustomerId, recipientType, recipientVendorId, storeId);

            if (unreadOnly == true)
                query = query.Where(n => !n.IsRead);

            return query.OrderByDescending(n => n.CreatedOnUtc).ThenByDescending(n => n.Id);
        }, pageIndex, pageSize);
    }

    public virtual async Task<int> GetUnreadCountAsync(int recipientCustomerId,
        NotificationRecipientType recipientType, int recipientVendorId = 0, int storeId = 0)
    {
        var query = FilterByRecipient(_notificationRepository.Table, recipientCustomerId, recipientType,
            recipientVendorId, storeId);
        return await query.CountAsync(n => !n.IsRead);
    }

    public virtual async Task MarkAsReadAsync(int notificationId, int recipientCustomerId)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId, cache => default);
        if (notification == null || notification.RecipientCustomerId != recipientCustomerId || notification.IsRead)
            return;

        notification.IsRead = true;
        notification.ReadOnUtc = DateTime.UtcNow;
        await _notificationRepository.UpdateAsync(notification);
    }

    public virtual async Task MarkAllAsReadAsync(int recipientCustomerId, NotificationRecipientType recipientType,
        int recipientVendorId = 0, int storeId = 0)
    {
        var query = FilterByRecipient(_notificationRepository.Table, recipientCustomerId, recipientType,
            recipientVendorId, storeId).Where(n => !n.IsRead);

        var unread = await query.ToListAsync();
        if (unread.Count == 0)
            return;

        var now = DateTime.UtcNow;
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadOnUtc = now;
        }

        await _notificationRepository.UpdateAsync(unread);
    }

    public virtual async Task<Notification> GetByIdAsync(int id)
    {
        return await _notificationRepository.GetByIdAsync(id, cache => default, useShortTermCache: true);
    }

    #endregion
}
