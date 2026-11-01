// [Mahjar notifications]
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Mahjar;

namespace Nop.Services.Mahjar.Notifications;

/// <summary>
/// Per-customer, per-type notification preferences. Opt-out model: a missing row means the type is enabled.
/// [Mahjar notifications]
/// </summary>
public partial interface INotificationPreferenceService
{
    /// <summary>True if the in-app feed should be written for this customer + type (no row ⇒ true).</summary>
    Task<bool> IsInAppEnabledAsync(int customerId, NotificationType type);

    /// <summary>All explicit preference rows for a customer (types without a row use the defaults).</summary>
    Task<IList<NotificationPreference>> GetPreferencesAsync(int customerId);

    /// <summary>Create or update the preference for a customer + type.</summary>
    Task SetPreferenceAsync(int customerId, NotificationType type, bool inApp, bool email);
}
