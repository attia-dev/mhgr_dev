// [Mahjar notifications]
using Nop.Core.Configuration;

namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// Settings for the in-app notification feed. Auto-discovered/auto-registered as an <see cref="ISettings"/>;
/// C# defaults below apply until overridden in admin. [Mahjar notifications]
/// </summary>
public partial class NotificationSettings : ISettings
{
    /// <summary>Whether the SignalR real-time push is attempted (the feed still persists if false).</summary>
    public bool RealtimeEnabled { get; set; } = true;

    /// <summary>Default page size for the feed list.</summary>
    public int FeedPageSize { get; set; } = 20;

    /// <summary>Whether store-owner/admin customers get an in-app row for new orders (admin fan-out gate).</summary>
    public bool NotifyAdminsOnNewOrder { get; set; } = true;
}
