namespace Nop.Core.Domain.Vendors;

/// <summary>
/// [Mahjar vendor-application] Represents the legal nature of a partner (vendor): a private person or a
/// registered company. Captured on the "become a partner" application (web + mobile) and shown to the store
/// owner with the request, because the two are onboarded with different identity documents — an individual
/// supplies a National ID / Iqama number, a corporation a commercial registration (CR) number.
/// </summary>
public enum VendorType
{
    /// <summary>
    /// Not specified (vendors created before the application collected a type, or created directly in admin)
    /// </summary>
    NotSpecified = 0,

    /// <summary>
    /// A private person trading under their own name
    /// </summary>
    Individual = 1,

    /// <summary>
    /// A company with a commercial registration
    /// </summary>
    Corporation = 2
}
