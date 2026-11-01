// [Mahjar notifications]
using System.Threading.Tasks;
using Nop.Core.Domain.Mahjar;

namespace Nop.Services.Mahjar.Notifications;

/// <summary>
/// Real-time push abstraction. Lets <see cref="INotificationFeedService"/> trigger a live SignalR broadcast
/// without the service layer taking a hard dependency on ASP.NET SignalR types. The concrete
/// SignalR-backed implementation lives in Nop.Web and overrides the no-op default registered for the
/// framework. [Mahjar notifications]
/// </summary>
public partial interface INotificationRealtimeNotifier
{
    /// <summary>
    /// Push a freshly-created notification (and the recipient's recomputed unread count) to the connected
    /// recipient. Implementations must never throw to the caller.
    /// </summary>
    /// <param name="notification">The notification that was just persisted.</param>
    /// <param name="unreadCount">The recipient's current unread count.</param>
    Task NotifyRecipientAsync(Notification notification, int unreadCount);
}
