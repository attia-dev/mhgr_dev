// [Mahjar notifications]
namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// A customer's per-type notification preference. Opt-out model: the ABSENCE of a row means the type is
/// enabled, so existing customers need no seeding. <see cref="EmailEnabled"/> is persisted in v1 but not yet
/// enforced (email keeps flowing through the workflow message service). [Mahjar notifications]
/// </summary>
public partial class NotificationPreference : BaseEntity
{
    /// <summary>The customer this preference belongs to.</summary>
    public int CustomerId { get; set; }

    /// <summary>The notification type (backing value for <see cref="NotificationType"/>).</summary>
    public int NotificationTypeId { get; set; }

    /// <summary>Whether the in-app feed row is written for this type.</summary>
    public bool InAppEnabled { get; set; }

    /// <summary>Whether email is wanted for this type (reserved; not enforced in v1).</summary>
    public bool EmailEnabled { get; set; }

    /// <summary>Typed wrapper over <see cref="NotificationTypeId"/>.</summary>
    public NotificationType NotificationType
    {
        get => (NotificationType)NotificationTypeId;
        set => NotificationTypeId = (int)value;
    }
}
