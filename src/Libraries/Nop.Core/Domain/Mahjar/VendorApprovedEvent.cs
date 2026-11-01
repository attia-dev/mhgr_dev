// [Mahjar notifications]
using Nop.Core.Domain.Vendors;

namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// Published when an admin activates a vendor whose application was pending (Active false → true), so the
/// applicant can be notified their "become a seller" request was approved. [Mahjar notifications]
/// </summary>
public partial class VendorApprovedEvent
{
    public VendorApprovedEvent(Vendor vendor)
    {
        Vendor = vendor;
    }

    /// <summary>The vendor that was just activated.</summary>
    public Vendor Vendor { get; }
}
