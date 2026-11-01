// [Mahjar partner-agreement]
namespace Nop.Services.Mahjar.Agreements;

/// <summary>
/// [Mahjar partner-agreement] The catalogue of fields an agreement body may carry, grouped by where their value
/// comes from. It drives the field picker on Admin -> Settings -> Vendor settings -> Partner agreement, which is
/// how a store owner puts a field into the text without having to remember its spelling.
///
/// It must stay in step with the tokens <see cref="PartnerAgreementService.RenderAsync"/> resolves: a field
/// listed here but not resolved there would be inserted and then printed literally. The admin page checks the
/// two against each other and hides any field the renderer does not know, so drift is silent rather than wrong.
/// </summary>
public static class PartnerAgreementTokens
{
    /// <summary>The prefix of every token's locale resource, completed with the token's own key.</summary>
    public const string RESOURCE_PREFIX = "admin.mahjar.partneragreement.token.";

    /// <summary>One group of fields, named after where its values come from.</summary>
    /// <param name="TitleResource">The locale resource of the group heading</param>
    /// <param name="HintResource">The locale resource of the line explaining where the values are set</param>
    /// <param name="Tokens">The tokens of the group, in the order they are offered</param>
    public record TokenGroup(string TitleResource, string HintResource, string[] Tokens);

    /// <summary>The groups, in the order the picker shows them.</summary>
    public static IReadOnlyList<TokenGroup> Groups { get; } =
    [
        new TokenGroup(
            "admin.mahjar.partneragreement.tokens.group.platform",
            "admin.mahjar.partneragreement.tokens.group.platform.hint",
            [
                "%Store.LegalName%",
                "%Store.CommercialRegistration%",
                "%Store.NationalAddress%",
                "%Store.SupportChannel%"
            ]),

        new TokenGroup(
            "admin.mahjar.partneragreement.tokens.group.partner",
            "admin.mahjar.partneragreement.tokens.group.partner.hint",
            [
                "%Partner.Name%",
                "%Partner.Type%",
                "%Partner.LegalForm%",
                "%Partner.UserId%",
                "%Partner.Email%",
                "%Partner.NationalId%",
                "%Partner.CommercialRegistration%",
                "%Partner.VatNumber%",
                "%Partner.SignatoryName%",
                "%Partner.SignatoryCapacity%",
                "%Partner.ActivatedServices%"
            ]),

        new TokenGroup(
            "admin.mahjar.partneragreement.tokens.group.schedule",
            "admin.mahjar.partneragreement.tokens.group.schedule.hint",
            [
                "%Partner.VerificationMethod%",
                "%Partner.SettlementAccount%",
                "%Partner.RentalCommission%",
                "%Partner.SaleCommission%",
                "%Partner.ListingFee%",
                "%Partner.InspectionFee%",
                "%Partner.DeliveryFee%",
                "%Partner.StorageFee%",
                "%Partner.MinimumSettlement%",
                "%Partner.PrepTime%",
                "%Partner.SettlementCycle%",
                "%Partner.ClaimResponseDays%",
                "%Partner.TerminationNoticeDays%",
                "%Partner.AmicableSettlementDays%",
                "%Partner.ReturnInspectionDays%",
                "%Partner.AgreementTerm%",
                "%Partner.RenewalBasis%",
                "%Partner.RentalItemValueCap%",
                "%Partner.CommercialNotes%"
            ]),

        new TokenGroup(
            "admin.mahjar.partneragreement.tokens.group.acceptance",
            "admin.mahjar.partneragreement.tokens.group.acceptance.hint",
            [
                "%Agreement.Id%",
                "%Agreement.Version%",
                "%Agreement.EffectiveDate%",
                "%Agreement.AcceptedAnnexes%",
                "%Agreement.AcceptedOn%",
                "%Agreement.AcceptanceMethod%",
                "%Agreement.TechnicalRef%"
            ])
    ];

    /// <summary>
    /// The locale resource holding a token's label, e.g. "%Partner.PrepTime%" ->
    /// "admin.mahjar.partneragreement.token.partner.preptime"
    /// </summary>
    /// <param name="token">The token, with its percent signs</param>
    /// <returns>The resource name</returns>
    public static string GetResourceName(string token)
    {
        return RESOURCE_PREFIX + token.Trim('%').ToLowerInvariant();
    }
}
