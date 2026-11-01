// [Mahjar notifications]
namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// A persisted, per-user in-app notification (the feed behind the bell on storefront, admin/vendor panel and
/// the mobile app). Written alongside the existing email — it does not replace it. [Mahjar notifications]
/// </summary>
public partial class Notification : BaseEntity
{
    /// <summary>
    /// The customer this notification belongs to. Always a customer row — a buyer, a vendor-manager
    /// customer, or an admin customer (see <see cref="RecipientType"/>).
    /// </summary>
    public int RecipientCustomerId { get; set; }

    /// <summary>
    /// The vendor this notification targets; 0 unless <see cref="RecipientType"/> is
    /// <see cref="NotificationRecipientType.Vendor"/>. Kept as a plain int (no FK) so vendor reassignment
    /// or deletion never cascades feed history.
    /// </summary>
    public int RecipientVendorId { get; set; }

    /// <summary>The recipient discriminator (backing value for <see cref="RecipientType"/>).</summary>
    public int RecipientTypeId { get; set; }

    /// <summary>The event kind (backing value for <see cref="NotificationType"/>).</summary>
    public int NotificationTypeId { get; set; }

    /// <summary>Short localized title.</summary>
    public string Title { get; set; }

    /// <summary>Localized body.</summary>
    public string Body { get; set; }

    /// <summary>The referenced entity name, e.g. "Order", "Shipment", "ProductReview" (nullable).</summary>
    public string EntityName { get; set; }

    /// <summary>The referenced entity id (0 if none).</summary>
    public int EntityId { get; set; }

    /// <summary>Optional deep-link/route (storefront route or app target).</summary>
    public string Url { get; set; }

    /// <summary>Whether the recipient has read it.</summary>
    public bool IsRead { get; set; }

    /// <summary>The store identifier.</summary>
    public int StoreId { get; set; }

    /// <summary>Creation date (UTC).</summary>
    public DateTime CreatedOnUtc { get; set; }

    /// <summary>When it was read (UTC), if read.</summary>
    public DateTime? ReadOnUtc { get; set; }

    /// <summary>Typed wrapper over <see cref="RecipientTypeId"/>.</summary>
    public NotificationRecipientType RecipientType
    {
        get => (NotificationRecipientType)RecipientTypeId;
        set => RecipientTypeId = (int)value;
    }

    /// <summary>Typed wrapper over <see cref="NotificationTypeId"/>.</summary>
    public NotificationType NotificationType
    {
        get => (NotificationType)NotificationTypeId;
        set => NotificationTypeId = (int)value;
    }
}
