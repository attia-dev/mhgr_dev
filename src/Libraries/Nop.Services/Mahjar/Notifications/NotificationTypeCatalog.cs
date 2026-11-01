// [Mahjar notifications]
using Nop.Core.Domain.Mahjar;

namespace Nop.Services.Mahjar.Notifications;

/// <summary>
/// The notification types shown on each surface's preferences screen, grouped by audience. Reserved types
/// (NewVendorApplication / NewReviewPending) are intentionally excluded — they are not wired in v1.
/// [Mahjar notifications]
/// </summary>
public static class NotificationTypeCatalog
{
    /// <summary>Buyer-facing types (storefront + mobile app).</summary>
    public static readonly NotificationType[] BuyerTypes =
    {
        NotificationType.OrderPlacedBuyer,
        NotificationType.OrderPaidBuyer,
        NotificationType.OrderStatusChanged,
        NotificationType.OrderRefunded,
        NotificationType.OrderShipped,
        NotificationType.OrderDelivered,
        NotificationType.OrderReadyForPickup,
        NotificationType.Welcome,
        NotificationType.SellerApproved,
        NotificationType.SellerDeactivated,
        //[Mahjar contracts]
        NotificationType.ContractSentBuyer,
        NotificationType.ContractApprovedBuyer,
        NotificationType.ContractRejectedBuyer,
        //[Mahjar handover]
        NotificationType.HandoverDeliveredBuyer,
        NotificationType.RentalReturnRecordedBuyer
    };

    /// <summary>Vendor-facing types (vendor panel).</summary>
    public static readonly NotificationType[] VendorTypes =
    {
        NotificationType.OrderPlacedVendor,
        NotificationType.OrderPaidVendor,
        NotificationType.ReviewApprovedVendor,
        //[Mahjar handover]
        NotificationType.IntakeRecordedVendor
    };

    /// <summary>Admin/store-owner-facing types.</summary>
    public static readonly NotificationType[] AdminTypes =
    {
        NotificationType.OrderPlacedAdmin,
        NotificationType.NewVendorApplication,
        //[Mahjar contracts]
        NotificationType.ContractRequestedAdmin,
        NotificationType.ContractSignedAdmin,
        //[Mahjar handover]
        NotificationType.IntakeRecordedAdmin,
        NotificationType.HandoverDeliveredAdmin,
        NotificationType.RentalReturnRecordedAdmin,
        NotificationType.HandoverUnassignedAdmin
    };

    /// <summary>[Mahjar handover] Handover-desk employee types: a task landed in one of their queues.</summary>
    public static readonly NotificationType[] HandoverStaffTypes =
    {
        NotificationType.HandoverPreContractStaff,
        NotificationType.HandoverDeliveryStaff,
        NotificationType.HandoverReturnStaff
    };

    /// <summary>The locale-resource key for a type's display title.</summary>
    public static string TitleResourceKey(NotificationType type)
        => "mahjar.notifications.type." + type.ToString().ToLowerInvariant() + ".title";

    /// <summary>The types relevant to a recipient audience.</summary>
    public static NotificationType[] ForRecipient(NotificationRecipientType recipientType)
    {
        return recipientType switch
        {
            NotificationRecipientType.Vendor => VendorTypes,
            NotificationRecipientType.Admin => AdminTypes,
            _ => BuyerTypes
        };
    }

    /// <summary>
    /// [Mahjar handover] The types a non-vendor dashboard user can receive: the admin types for an administrator, the
    /// handover types for a handover employee, both for someone who is both — and the admin types for any other
    /// dashboard user, as before.
    /// </summary>
    public static NotificationType[] ForDashboardUser(bool isAdmin, bool isHandoverStaff)
    {
        if (!isHandoverStaff)
            return AdminTypes;

        return isAdmin ? AdminTypes.Concat(HandoverStaffTypes).ToArray() : HandoverStaffTypes;
    }
}
