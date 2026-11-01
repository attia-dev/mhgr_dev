// [Mahjar partner-request]
using Nop.Core.Domain.Vendors;

namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// Published when the store owner rejects a partner application, so the applicant is told — and told why, since
/// they may correct it and apply again.
///
/// It is its own event rather than a reuse of <see cref="VendorDeactivatedEvent"/> for two reasons: a pending
/// application was never active, so the deactivation path never fires for it, and "your account was suspended"
/// is the wrong thing to say to someone whose application was refused. [Mahjar partner-request]
/// </summary>
public partial class VendorApplicationRejectedEvent
{
    public VendorApplicationRejectedEvent(Vendor vendor)
    {
        Vendor = vendor;
    }

    /// <summary>The application that was just rejected, carrying its reason.</summary>
    public Vendor Vendor { get; }
}
