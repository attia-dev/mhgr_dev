// [Mahjar partner-agreement]
namespace Nop.Core.Domain.Mahjar.Agreements;

/// <summary>
/// [Mahjar partner-agreement] Which of the two master partner agreements a partner accepted. The document
/// follows the declared <see cref="Nop.Core.Domain.Vendors.VendorType"/>: an individual accepts
/// "اتفاقية الشريك الفرد", a company "اتفاقية الشريك التجاري (المؤسسات والشركات)". The key is stored on the
/// vendor next to the accepted version so the acceptance record names the document it belongs to.
/// </summary>
public static class PartnerAgreementDocument
{
    /// <summary>The individual partner master agreement — اتفاقية الشريك الفرد</summary>
    public const string Individual = "IndividualMaster";

    /// <summary>The corporate partner master agreement — اتفاقية الشريك التجاري (المؤسسات والشركات)</summary>
    public const string Corporate = "CorporateMaster";
}
