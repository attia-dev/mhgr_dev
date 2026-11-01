// [Mahjar partner-agreement]
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Nop.Core;
using Nop.Core.Domain.Mahjar;
using Nop.Core.Domain.Mahjar.Agreements;
using Nop.Core.Domain.Vendors;
using Nop.Services.Localization;

namespace Nop.Services.Mahjar.Agreements;

/// <summary>
/// [Mahjar partner-agreement] The partner agreement service — see <see cref="IPartnerAgreementService"/>.
///
/// Token resolution mirrors the buyer-side contract service: every value falls back to an em-dash rather than
/// leaving a blank or throwing, so an agreement always prints. The platform's own header values are read from
/// <see cref="ContractSettings"/> (the same ones the buyer contract prints) and the commercial schedule from
/// <see cref="PartnerAgreementSettings"/>.
/// </summary>
public partial class PartnerAgreementService : IPartnerAgreementService
{
    #region Constants

    /// <summary>What an unset value prints as, exactly as the buyer contract does it.</summary>
    protected const string EMPTY_VALUE = "—";

    /// <summary>
    /// The fields an applicant fills in on the application form itself. They are the ones the form keeps in
    /// step as it is typed, so they are the ones wrapped when <see cref="PartnerAgreementContext.MarkLiveFields"/>
    /// is set. Everything else is either a store-wide setting or stamped at acceptance.
    /// </summary>
    protected static readonly string[] LIVE_FIELDS =
    [
        "%Partner.Name%",
        "%Partner.Type%",
        "%Partner.LegalForm%",
        "%Partner.Email%",
        "%Partner.NationalId%",
        "%Partner.CommercialRegistration%",
        "%Partner.VatNumber%",
        "%Partner.SignatoryName%",
        "%Partner.SignatoryCapacity%",
        "%Partner.ActivatedServices%",
        "%Agreement.AcceptedAnnexes%"
    ];

    #endregion

    #region Fields

    protected readonly ContractSettings _contractSettings;
    protected readonly ILocalizationService _localizationService;
    protected readonly PartnerAgreementSettings _partnerAgreementSettings;
    protected readonly IStoreContext _storeContext;

    #endregion

    #region Ctor

    public PartnerAgreementService(ContractSettings contractSettings,
        ILocalizationService localizationService,
        PartnerAgreementSettings partnerAgreementSettings,
        IStoreContext storeContext)
    {
        _contractSettings = contractSettings;
        _localizationService = localizationService;
        _partnerAgreementSettings = partnerAgreementSettings;
        _storeContext = storeContext;
    }

    #endregion

    #region Utilities

    /// <summary>The value, or the em-dash when there is nothing to print.</summary>
    protected static string Pick(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? EMPTY_VALUE : value.Trim();
    }

    /// <summary>A date in an unambiguous, culture-independent form — Arabic-Indic digits are never wanted here.</summary>
    protected static string Pick(DateTime? value, bool withTime = false)
    {
        if (!value.HasValue)
            return EMPTY_VALUE;

        return value.Value.ToString(withTime ? "yyyy-MM-dd HH:mm 'UTC'" : "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    /// <summary>A count of days, written as Latin digits so the agreement reads the same in both languages.</summary>
    protected static string Pick(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>The localized name of the annexes a set of activated services brings into force.</summary>
    protected virtual async Task<string> GetAnnexesNameAsync(PartnerServices services, int languageId)
    {
        var accepted = new List<string>
        {
            await _localizationService.GetResourceAsync("mahjar.partneragreement.annexes.master", languageId)
        };

        if (services.HasFlag(PartnerServices.Rental))
            accepted.Add(await _localizationService.GetResourceAsync("mahjar.partneragreement.annexes.rental", languageId));

        if (services.HasFlag(PartnerServices.Sale))
            accepted.Add(await _localizationService.GetResourceAsync("mahjar.partneragreement.annexes.sale", languageId));

        return string.Join("، ", accepted.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets the agreement body for a partner type, in a language, with its tokens still unresolved
    /// </summary>
    public virtual async Task<string> GetRawBodyAsync(VendorType vendorType, int languageId)
    {
        //the corporate document is for registered companies, the individual one for everybody else — a partner
        //whose type was never declared reads the individual agreement, which is the lighter of the two
        return vendorType == VendorType.Corporation
            ? await _localizationService.GetLocalizedSettingAsync(_partnerAgreementSettings,
                x => x.CorporateBody, languageId, storeId: 0)
            : await _localizationService.GetLocalizedSettingAsync(_partnerAgreementSettings,
                x => x.IndividualBody, languageId, storeId: 0);
    }

    /// <summary>
    /// Builds every token an agreement body may carry, with its value for this context. It is the single list
    /// the renderer and the admin field picker both answer to — see <see cref="PartnerAgreementTokens"/>.
    /// </summary>
    /// <param name="context">Who the agreement is being rendered for</param>
    /// <param name="languageId">Language identifier</param>
    /// <returns>A task that represents the asynchronous operation; token name -> printed value</returns>
    protected virtual async Task<IDictionary<string, string>> BuildTokensAsync(PartnerAgreementContext context, int languageId)
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var partnerTypeName = await _localizationService.GetLocalizedEnumAsync(context.VendorType, languageId);

        var tokens = new Dictionary<string, string>
        {
            //the platform's side of the header — shared with the buyer contract so the two cannot drift apart
            ["%Store.LegalName%"] = Pick(_contractSettings.OperatorLegalName ?? store?.CompanyName ?? store?.Name),
            ["%Store.CommercialRegistration%"] = Pick(_contractSettings.CommercialRegistrationNumber),
            ["%Store.NationalAddress%"] = Pick(_partnerAgreementSettings.OperatorNationalAddress ?? store?.CompanyAddress),
            ["%Store.SupportChannel%"] = Pick(_contractSettings.SupportChannel),

            //the partner's side
            ["%Partner.Name%"] = Pick(context.PartnerName),
            ["%Partner.Type%"] = Pick(partnerTypeName),
            ["%Partner.LegalForm%"] = Pick(partnerTypeName),
            ["%Partner.UserId%"] = context.CustomerId > 0 ? Pick(context.CustomerId) : EMPTY_VALUE,
            ["%Partner.Email%"] = Pick(context.Email),
            ["%Partner.NationalId%"] = Pick(context.NationalId),
            ["%Partner.CommercialRegistration%"] = Pick(context.CommercialRegistrationNumber),
            ["%Partner.VatNumber%"] = Pick(context.VatNumber),
            ["%Partner.SignatoryName%"] = Pick(context.AuthorizedSignatoryName),
            ["%Partner.SignatoryCapacity%"] = Pick(context.AuthorizedSignatoryCapacity),
            ["%Partner.VerificationMethod%"] = Pick(_partnerAgreementSettings.VerificationMethod),
            ["%Partner.ActivatedServices%"] = Pick(await GetServicesNameAsync(context.Services, languageId)),
            ["%Partner.SettlementAccount%"] = Pick(_partnerAgreementSettings.SettlementAccountTerms),

            //Annex A — the commercial schedule
            ["%Partner.RentalCommission%"] = Pick(_partnerAgreementSettings.RentalCommission),
            ["%Partner.SaleCommission%"] = Pick(_partnerAgreementSettings.SaleCommission),
            ["%Partner.ListingFee%"] = Pick(_partnerAgreementSettings.ListingFee),
            ["%Partner.InspectionFee%"] = Pick(_partnerAgreementSettings.InspectionFee),
            ["%Partner.DeliveryFee%"] = Pick(_partnerAgreementSettings.DeliveryFee),
            ["%Partner.StorageFee%"] = Pick(_partnerAgreementSettings.StorageFee),
            ["%Partner.MinimumSettlement%"] = Pick(_partnerAgreementSettings.MinimumSettlement),
            ["%Partner.PrepTime%"] = Pick(_partnerAgreementSettings.PreparationTime),
            ["%Partner.SettlementCycle%"] = Pick(_partnerAgreementSettings.SettlementCycle),
            ["%Partner.ClaimResponseDays%"] = Pick(_partnerAgreementSettings.ClaimResponseDays),
            ["%Partner.TerminationNoticeDays%"] = Pick(_partnerAgreementSettings.TerminationNoticeDays),
            ["%Partner.AgreementTerm%"] = Pick(_partnerAgreementSettings.AgreementTerm),
            ["%Partner.RenewalBasis%"] = Pick(_partnerAgreementSettings.RenewalBasis),
            ["%Partner.AmicableSettlementDays%"] = Pick(_partnerAgreementSettings.AmicableSettlementDays),
            ["%Partner.ReturnInspectionDays%"] = Pick(_partnerAgreementSettings.ReturnInspectionDays),
            ["%Partner.RentalItemValueCap%"] = _contractSettings.RentalItemValueCap.ToString("0.##", CultureInfo.InvariantCulture),
            ["%Partner.CommercialNotes%"] = Pick(_partnerAgreementSettings.CommercialNotes),

            //the acceptance record — a dash while the agreement is only being read
            ["%Agreement.Id%"] = Pick(context.TechnicalRef),
            ["%Agreement.Version%"] = Pick(_partnerAgreementSettings.Version),
            ["%Agreement.EffectiveDate%"] = Pick(_partnerAgreementSettings.EffectiveDateUtc),
            ["%Agreement.AcceptedAnnexes%"] = Pick(await GetAnnexesNameAsync(context.Services, languageId)),
            ["%Agreement.AcceptedOn%"] = Pick(context.AcceptedOnUtc, withTime: true),
            ["%Agreement.AcceptanceMethod%"] = Pick(context.AcceptanceMethod),
            ["%Agreement.TechnicalRef%"] = Pick(context.TechnicalRef)
        };

        //[Mahjar partner-request] on the application form the applicant's own fields are wrapped so the page can
        //keep them in step with what is being typed. Only their fields: everything else is already final
        if (context.MarkLiveFields)
        {
            foreach (var token in LIVE_FIELDS.Where(tokens.ContainsKey))
                tokens[token] = $"<span class=\"mahjar-live-field\" data-mahjar-field=\"{token.Trim('%')}\">{WebUtility.HtmlEncode(tokens[token])}</span>";
        }

        return tokens;
    }

    /// <summary>
    /// Gets the agreement body for a partner type, in a language, with every token resolved
    /// </summary>
    public virtual async Task<string> RenderAsync(PartnerAgreementContext context, int languageId)
    {
        ArgumentNullException.ThrowIfNull(context);

        var body = await GetRawBodyAsync(context.VendorType, languageId);
        if (string.IsNullOrWhiteSpace(body))
            return null;

        var tokens = await BuildTokensAsync(context, languageId);

        var rendered = new StringBuilder(body);
        foreach (var token in tokens)
            rendered.Replace(token.Key, token.Value);

        return rendered.ToString();
    }

    /// <summary>
    /// Gets the names of every token the renderer resolves. The admin field picker offers only these, so a
    /// field it lists can never be one that would print literally.
    /// </summary>
    public virtual async Task<IReadOnlyCollection<string>> GetKnownTokenNamesAsync(int languageId)
    {
        var tokens = await BuildTokensAsync(new PartnerAgreementContext(), languageId);

        return tokens.Keys.ToList();
    }

    /// <summary>
    /// Stamps the acceptance of an agreement onto a partner
    /// </summary>
    public virtual async Task RecordAcceptanceAsync(Vendor vendor, PartnerAgreementContext context,
        string acceptanceMethod, string ipAddress, int languageId)
    {
        ArgumentNullException.ThrowIfNull(vendor);
        ArgumentNullException.ThrowIfNull(context);

        var acceptedOnUtc = DateTime.UtcNow;
        var technicalRef = Guid.NewGuid().ToString("N").ToUpperInvariant();

        //render the copy that is kept: the same text the applicant read, with its acceptance table filled in
        var acceptedBody = await RenderAsync(context with
        {
            AcceptedOnUtc = acceptedOnUtc,
            AcceptanceMethod = acceptanceMethod,
            TechnicalRef = technicalRef
        }, languageId);

        vendor.ActivatedServices = context.Services;
        vendor.AgreementDocumentKey = context.VendorType == VendorType.Corporation
            ? PartnerAgreementDocument.Corporate
            : PartnerAgreementDocument.Individual;
        vendor.AgreementVersion = _partnerAgreementSettings.Version;
        vendor.AgreementAcceptedOnUtc = acceptedOnUtc;
        vendor.AgreementAcceptedIp = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim();
        vendor.AgreementAcceptanceMethod = acceptanceMethod;
        vendor.AgreementTechnicalRef = technicalRef;
        vendor.AgreementBody = acceptedBody;
        vendor.AgreementBodyHash = string.IsNullOrEmpty(acceptedBody)
            ? null
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(acceptedBody)));

        //the legacy short-terms stamp keeps working for anything still reading it
        vendor.TermsAcceptedOnUtc = acceptedOnUtc;
    }

    /// <summary>
    /// Gets a value indicating whether a partner has accepted the version of the agreement currently in force
    /// </summary>
    public virtual bool HasAcceptedCurrentVersion(Vendor vendor)
    {
        ArgumentNullException.ThrowIfNull(vendor);

        if (string.IsNullOrWhiteSpace(vendor.AgreementVersion))
            return false;

        return string.Equals(vendor.AgreementVersion.Trim(),
            (_partnerAgreementSettings.Version ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the localized name of a set of activated services
    /// </summary>
    public virtual async Task<string> GetServicesNameAsync(PartnerServices services, int languageId)
    {
        var resourceName = services switch
        {
            PartnerServices.Rental | PartnerServices.Sale => "mahjar.partneragreement.services.both",
            PartnerServices.Rental => "mahjar.partneragreement.services.rental",
            PartnerServices.Sale => "mahjar.partneragreement.services.sale",
            _ => "mahjar.partneragreement.services.none"
        };

        return await _localizationService.GetResourceAsync(resourceName, languageId);
    }

    #endregion
}
