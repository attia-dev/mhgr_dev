// [Mahjar notifications]
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Mahjar;

namespace Nop.Services.Mahjar.Notifications;

/// <summary>
/// The in-app notification feed: create rows (with a per-type opt-in check + real-time broadcast), list them
/// per recipient, count unread, and mark read. [Mahjar notifications]
/// </summary>
public partial interface INotificationFeedService
{
    /// <summary>
    /// Persist a notification and (optionally) broadcast it in real time. Returns null if the recipient has
    /// opted out of this type in-app.
    /// </summary>
    Task<Notification> CreateNotificationAsync(Notification notification, bool broadcast = true);

    /// <summary>Persist many notifications (one event → many recipients), each opt-in checked + broadcast.</summary>
    Task CreateNotificationsAsync(IEnumerable<Notification> notifications);

    /// <summary>Paged feed for a recipient.</summary>
    Task<IPagedList<Notification>> GetNotificationsAsync(int recipientCustomerId,
        NotificationRecipientType recipientType, int recipientVendorId = 0, int storeId = 0,
        bool? unreadOnly = null, int pageIndex = 0, int pageSize = 20);

    /// <summary>Unread count for a recipient.</summary>
    Task<int> GetUnreadCountAsync(int recipientCustomerId, NotificationRecipientType recipientType,
        int recipientVendorId = 0, int storeId = 0);

    /// <summary>Mark one notification read (ownership-checked against <paramref name="recipientCustomerId"/>).</summary>
    Task MarkAsReadAsync(int notificationId, int recipientCustomerId);

    /// <summary>Mark all of a recipient's notifications read.</summary>
    Task MarkAllAsReadAsync(int recipientCustomerId, NotificationRecipientType recipientType,
        int recipientVendorId = 0, int storeId = 0);

    /// <summary>Get a notification by id (no ownership check — callers must scope).</summary>
    Task<Notification> GetByIdAsync(int id);
}
