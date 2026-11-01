// [Mahjar partner-agreement]
using Nop.Core.Configuration;

namespace Nop.Core.Domain.Mahjar.Agreements;

/// <summary>
/// [Mahjar partner-agreement] Everything the two master partner agreements need in order to print without a
/// blank. The store owner's documents leave their commercial terms as "[●]" placeholders — commission, fees,
/// preparation time, settlement cycle, the notice and objection windows — and this settings class is where
/// those values live. They are store-wide: one schedule applies to every partner, and each blank is rendered
/// into the agreement through its %Partner.*% / %Agreement.*% token
/// (see <see cref="Nop.Services.Mahjar.Agreements.IPartnerAgreementService"/>).
///
/// <see cref="IndividualBody"/> and <see cref="CorporateBody"/> are the agreement texts themselves, edited per
/// language in Admin -> Settings -> Vendor settings. Like every localized setting they are stored as a
/// LocalizedProperty over the Setting row, so the Arabic (legally prevailing) text shadows the English one for
/// Arabic visitors. Values of the platform's own side of the header — legal name, CR, support channel — are
/// reused from <see cref="ContractSettings"/> so the buyer and partner documents cannot drift apart.
///
/// NOTE: settings are cached; restart or POST /Admin/Common/ClearCache after a direct DB edit. [Mahjar partner-agreement]
/// </summary>
public partial class PartnerAgreementSettings : ISettings
{
    #region The agreement

    /// <summary>
    /// Gets or sets a value indicating whether the partner application shows a full master agreement instead of
    /// the short terms-and-conditions body. When it is disabled the legacy
    /// <see cref="Nop.Core.Domain.Vendors.VendorSettings.TermsOfService"/> body is used.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the version of both agreements, printed in the header and in the acceptance record
    /// (token %Agreement.Version%) and stamped on the partner when they accept
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets the effective date of the current version (token %Agreement.EffectiveDate%)
    /// </summary>
    public DateTime? EffectiveDateUtc { get; set; }

    /// <summary>
    /// Gets or sets the body of the individual partner agreement — اتفاقية الشريك الفرد (localized, HTML)
    /// </summary>
    public string IndividualBody { get; set; }

    /// <summary>
    /// Gets or sets the body of the corporate partner agreement — اتفاقية الشريك التجاري (localized, HTML)
    /// </summary>
    public string CorporateBody { get; set; }

    /// <summary>
    /// Gets or sets the platform's national address and contact details, the one header blank
    /// <see cref="ContractSettings"/> does not already carry (token %Store.NationalAddress%)
    /// </summary>
    public string OperatorNationalAddress { get; set; }

    /// <summary>
    /// Gets or sets how the partner's identity is verified, printed as "وسيلة التحقق من الهوية"
    /// (token %Partner.VerificationMethod%)
    /// </summary>
    public string VerificationMethod { get; set; }

    #endregion

    #region Annex A — the commercial schedule

    /// <summary>
    /// Gets or sets the rental commission, as written in the schedule — a percentage or an amount
    /// (token %Partner.RentalCommission%)
    /// </summary>
    public string RentalCommission { get; set; }

    /// <summary>
    /// Gets or sets the sale commission, as written in the schedule (token %Partner.SaleCommission%)
    /// </summary>
    public string SaleCommission { get; set; }

    /// <summary>
    /// Gets or sets the listing fee, if any — corporate schedule only (token %Partner.ListingFee%)
    /// </summary>
    public string ListingFee { get; set; }

    /// <summary>
    /// Gets or sets the inspection fee, if any (token %Partner.InspectionFee%)
    /// </summary>
    public string InspectionFee { get; set; }

    /// <summary>
    /// Gets or sets the delivery / pick-up fee, if any (token %Partner.DeliveryFee%)
    /// </summary>
    public string DeliveryFee { get; set; }

    /// <summary>
    /// Gets or sets the storage fee, if any — corporate schedule only (token %Partner.StorageFee%)
    /// </summary>
    public string StorageFee { get; set; }

    /// <summary>
    /// Gets or sets the minimum amount a settlement is paid out at — corporate schedule only
    /// (token %Partner.MinimumSettlement%)
    /// </summary>
    public string MinimumSettlement { get; set; }

    /// <summary>
    /// Gets or sets how long the partner has to prepare and hand over a piece (token %Partner.PrepTime%)
    /// </summary>
    public string PreparationTime { get; set; }

    /// <summary>
    /// Gets or sets the settlement period / cycle (token %Partner.SettlementCycle%)
    /// </summary>
    public string SettlementCycle { get; set; }

    /// <summary>
    /// Gets or sets what is recorded about the approved settlement account. The account number itself is NOT
    /// stored on the partner record — it is collected and verified outside the platform, and this text says so
    /// in the agreement (token %Partner.SettlementAccount%)
    /// </summary>
    public string SettlementAccountTerms { get; set; }

    /// <summary>
    /// Gets or sets the working days the partner has to answer an operational claim
    /// (token %Partner.ClaimResponseDays%)
    /// </summary>
    public int ClaimResponseDays { get; set; } = 5;

    /// <summary>
    /// Gets or sets the notice, in days, either party gives to terminate (token %Partner.TerminationNoticeDays%)
    /// </summary>
    public int TerminationNoticeDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the term the agreement runs for — the corporate agreement's article 31 leaves it blank
    /// (token %Partner.AgreementTerm%)
    /// </summary>
    public string AgreementTerm { get; set; }

    /// <summary>
    /// Gets or sets what the agreement renews on — the other blank of the corporate article 31
    /// (token %Partner.RenewalBasis%)
    /// </summary>
    public string RenewalBasis { get; set; }

    /// <summary>
    /// Gets or sets the working days for amicable settlement before a dispute goes to the competent authority
    /// (token %Partner.AmicableSettlementDays%)
    /// </summary>
    public int AmicableSettlementDays { get; set; } = 10;

    /// <summary>
    /// Gets or sets the working days a returned piece may be inspected within (token %Partner.ReturnInspectionDays%)
    /// </summary>
    public int ReturnInspectionDays { get; set; } = 5;

    /// <summary>
    /// Gets or sets any further commercial notes printed at the end of the schedule
    /// (token %Partner.CommercialNotes%)
    /// </summary>
    public string CommercialNotes { get; set; }

    #endregion
}
