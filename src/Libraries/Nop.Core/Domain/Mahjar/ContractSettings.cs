// [Mahjar contracts]
using Nop.Core.Configuration;

namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// Settings for the contract-approval stage before payment, and the store-level values the contract templates print
/// through their %Contract.*% / %Store.*% tokens (the rental agreement's operator details, policy periods and
/// default handover wording). Auto-discovered/auto-registered as an <see cref="ISettings"/>; the C# defaults below
/// apply until they are overridden in Admin -> Sales -> Contracts -> Contract settings.
///
/// The free-text values are LOCALIZED settings (one text per language, saved through
/// ILocalizationService.SaveLocalizedSettingAsync), because the contract is rendered in the customer's own language.
/// A contract request may override the operational ones per transaction. [Mahjar contracts]
/// </summary>
public partial class ContractSettings : ISettings
{
    /// <summary>Whether customers must request, sign and get an approved contract before they can pay.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>When true, only carts containing at least one rental line require a contract.</summary>
    public bool RequireOnlyForRentals { get; set; } = false;

    #region The platform as a contracting party

    /// <summary>
    /// The platform operator's legal name as it should appear in the contract (localized).
    /// Falls back to the store's company name, then to the store name.
    /// </summary>
    public string OperatorLegalName { get; set; }

    /// <summary>The operator's commercial registration number (سجل تجاري), printed next to the legal name.</summary>
    public string CommercialRegistrationNumber { get; set; }

    /// <summary>The support channel shown to the customer in the contract (localized) — an email, a number or both.</summary>
    public string SupportChannel { get; set; }

    #endregion

    #region The agreement itself

    /// <summary>The version of the agreement text, printed in its header and in the acceptance record.</summary>
    public string AgreementVersion { get; set; } = "1.0";

    /// <summary>The date the agreement text takes effect (تاريخ النفاذ); null hides the row.</summary>
    public DateTime? AgreementEffectiveDateUtc { get; set; }

    /// <summary>The date the agreement text was last updated (آخر تحديث); null hides the row.</summary>
    public DateTime? AgreementUpdatedDateUtc { get; set; }

    #endregion

    #region Rental policy values printed in the agreement

    /// <summary>The highest item value the store rents out (حد قيمة القطعة), in the primary store currency.</summary>
    public decimal RentalItemValueCap { get; set; } = 15000;

    /// <summary>Hours after the return time before a late rental is escalated (مهلة المعالجة الأولية).</summary>
    public int LateReturnGraceHours { get; set; } = 24;

    /// <summary>How the late-return charge is worked out (localized). Empty prints a dash.</summary>
    public string LateReturnFeeTerms { get; set; }

    /// <summary>The cancellation charge before handover, if any (localized). Empty prints a dash.</summary>
    public string CancellationFeeTerms { get; set; }

    /// <summary>Working days the store takes to process a refund once its requirements are met.</summary>
    public int RefundProcessingDays { get; set; } = 5;

    /// <summary>Working days the renter has to object to a damage claim.</summary>
    public int DamageObjectionDays { get; set; } = 5;

    /// <summary>Working days the store takes to handle a complaint once its documents are complete.</summary>
    public int ComplaintHandlingDays { get; set; } = 10;

    /// <summary>Working days allowed for an amicable settlement before a dispute goes to the competent body.</summary>
    public int AmicableSettlementDays { get; set; } = 10;

    #endregion

    #region Default wording for the per-transaction rows (a contract request may override each of these)

    /// <summary>The guarantee or security instrument the store requires (localized), e.g. a refundable deposit.</summary>
    public string GuaranteeType { get; set; }

    /// <summary>Where and when the item is handed over (localized), e.g. the handover desk and its hours.</summary>
    public string DeliveryDetails { get; set; }

    /// <summary>Where and when the item is taken back (localized).</summary>
    public string ReturnDetails { get; set; }

    /// <summary>Any additional fees that apply (localized). Empty prints a dash.</summary>
    public string ExtraFeesTerms { get; set; }

    /// <summary>How tax is treated in the contract amounts (localized), e.g. VAT-inclusive.</summary>
    public string TaxTerms { get; set; }

    #endregion
}
