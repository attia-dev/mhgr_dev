// [Mahjar partner-agreement]
namespace Nop.Core.Domain.Mahjar.Agreements;

/// <summary>
/// [Mahjar partner-request] Where a partner application stands. A partner created straight in admin never was
/// an application and keeps <see cref="NotAnApplication"/>, which is what hides it from the requests queue.
/// </summary>
public enum PartnerApplicationStatus
{
    /// <summary>
    /// Not an application — the partner was created directly in admin
    /// </summary>
    NotAnApplication = 0,

    /// <summary>
    /// Submitted and waiting for the store owner to review it
    /// </summary>
    Pending = 10,

    /// <summary>
    /// Approved: the partner is active and may list
    /// </summary>
    Approved = 20,

    /// <summary>
    /// Rejected. The record is kept for the audit trail, and the customer behind it is released so they can
    /// apply again — see <see cref="Nop.Core.Domain.Vendors.Vendor.ApplicationCustomerId"/>, which keeps the
    /// link after <c>Customer.VendorId</c> is cleared
    /// </summary>
    Rejected = 30
}
