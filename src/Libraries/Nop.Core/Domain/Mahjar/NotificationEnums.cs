// [Mahjar notifications]
namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// Who a notification is addressed to. One <see cref="Notification"/> table is shared by all three
/// audiences; the bell/feed always query with an explicit recipient type so a user holding several roles
/// is never double-counted. [Mahjar notifications]
/// </summary>
public enum NotificationRecipientType
{
    /// <summary>A buyer / customer (storefront + mobile app).</summary>
    Buyer = 1,

    /// <summary>A vendor / seller (web vendor panel inside /Admin).</summary>
    Vendor = 2,

    /// <summary>
    /// A dashboard user who is not a vendor: a store owner / administrator or, [Mahjar handover], an employee of the
    /// handover desk (HandoverStaff role). Both read the same admin-panel bell, scoped to their own customer id.
    /// </summary>
    Admin = 3
}

/// <summary>
/// The kind of event a notification represents. Values are grouped by audience (buyer 10–24, vendor 30–33,
/// admin 50–57, handover staff 70–72). The lower-cased enum name is the locale-resource key suffix, e.g.
/// <c>mahjar.notifications.type.orderplacedbuyer.title</c> / <c>.body</c>. [Mahjar notifications]
/// </summary>
public enum NotificationType
{
    //buyer
    OrderPlacedBuyer = 10,
    OrderPaidBuyer = 11,
    OrderStatusChanged = 12,
    OrderRefunded = 13,
    OrderShipped = 14,
    OrderDelivered = 15,
    OrderReadyForPickup = 16,
    Welcome = 17,
    SellerApproved = 18,
    SellerDeactivated = 19,
    //[Mahjar contracts] contract-approval stage
    ContractSentBuyer = 20,
    ContractApprovedBuyer = 21,
    ContractRejectedBuyer = 22,
    //[Mahjar handover] a rented item came back and its deposit refund was assessed
    RentalReturnRecordedBuyer = 23,
    //[Mahjar handover] the desk handed an item over, its condition documented
    HandoverDeliveredBuyer = 24,

    /// <summary>[Mahjar partner-request] The store owner refused a partner application</summary>
    SellerRejected = 25,

    //vendor
    OrderPlacedVendor = 30,
    OrderPaidVendor = 31,
    ReviewApprovedVendor = 32,
    //[Mahjar handover] the desk received the partner's item, its condition documented
    IntakeRecordedVendor = 33,

    //admin / store owner
    OrderPlacedAdmin = 50,

    //reserved — NOT wired in v1 (no consumable domain event exists today)
    NewVendorApplication = 51,
    NewReviewPending = 52,

    //[Mahjar contracts] contract-approval stage
    ContractRequestedAdmin = 53,
    ContractSignedAdmin = 54,

    //[Mahjar handover] a rented item came back — the deposit refund is due
    RentalReturnRecordedAdmin = 55,
    //[Mahjar handover] an employee handed an item over to the customer
    HandoverDeliveredAdmin = 56,
    //[Mahjar handover] an employee received an item from a partner
    IntakeRecordedAdmin = 57,
    //[Mahjar handover] an order is paid and waiting, but no handover employee is assigned to it yet
    HandoverUnassignedAdmin = 58,

    //[Mahjar handover] handover staff (admin panel, recipient type Admin): a task landed in one of their queues
    HandoverPreContractStaff = 70,
    HandoverDeliveryStaff = 71,
    HandoverReturnStaff = 72
}
