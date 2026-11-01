// [Mahjar notifications]
using Nop.Core.Domain.Vendors;

namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// Published when an admin deactivates a vendor (Active true → false), so the seller can be notified that their
/// store has been suspended — the mirror of <see cref="VendorApprovedEvent"/>. [Mahjar notifications]
/// </summary>
public partial class VendorDeactivatedEvent
{
    public VendorDeactivatedEvent(Vendor vendor)
    {
        Vendor = vendor;
    }

    /// <summary>The vendor that was just deactivated.</summary>
    public Vendor Vendor { get; }
}
