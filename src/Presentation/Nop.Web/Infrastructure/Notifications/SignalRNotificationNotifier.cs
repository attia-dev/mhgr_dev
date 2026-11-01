// [Mahjar notifications]
using System.Threading.Tasks;
using MHGR.Api.Contracts;
using Microsoft.AspNetCore.SignalR;
using Nop.Core.Domain.Mahjar;
using Nop.Services.Mahjar.Notifications;

namespace Nop.Web.Infrastructure.Notifications;

/// <summary>
/// SignalR-backed real-time notifier. Maps the persisted <see cref="Notification"/> to the shared
/// <see cref="NotificationDto"/> and pushes "ReceiveNotification" + "UnreadCount" to the recipient's group.
/// Registered in Nop.Web to override the framework's no-op notifier. [Mahjar notifications]
/// </summary>
public partial class SignalRNotificationNotifier : INotificationRealtimeNotifier
{
    protected readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationNotifier(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public virtual async Task NotifyRecipientAsync(Notification notification, int unreadCount)
    {
        //[Mahjar handover] one group per dashboard user: the row and its unread count are that user's alone (a shared
        //"admin" / "vendor-{id}" group put every recipient's count on every badge, and showed staff the admin rows)
        var group = notification.RecipientType switch
        {
            NotificationRecipientType.Vendor => $"vendor-{notification.RecipientVendorId}-{notification.RecipientCustomerId}",
            NotificationRecipientType.Admin => $"admin-{notification.RecipientCustomerId}",
            _ => $"customer-{notification.RecipientCustomerId}"
        };

        var dto = new NotificationDto
        {
            Id = notification.Id,
            Type = notification.NotificationTypeId,
            Title = notification.Title,
            Body = notification.Body,
            IsRead = notification.IsRead,
            CreatedOn = notification.CreatedOnUtc.ToString("o"),
            TargetEntity = string.IsNullOrEmpty(notification.EntityName) ? null : notification.EntityName.ToLowerInvariant(),
            TargetId = notification.EntityId > 0 ? notification.EntityId : (int?)null
        };

        await _hubContext.Clients.Group(group).SendAsync("ReceiveNotification", dto);
        await _hubContext.Clients.Group(group).SendAsync("UnreadCount", unreadCount);
    }
}
