// [Mahjar notifications]
using System.Threading.Tasks;
using Nop.Core.Domain.Mahjar;

namespace Nop.Services.Mahjar.Notifications;

/// <summary>
/// No-op real-time notifier registered for the framework so the service layer always resolves something.
/// The real SignalR notifier in Nop.Web overrides this registration. [Mahjar notifications]
/// </summary>
public partial class NullNotificationRealtimeNotifier : INotificationRealtimeNotifier
{
    public Task NotifyRecipientAsync(Notification notification, int unreadCount)
    {
        return Task.CompletedTask;
    }
}
