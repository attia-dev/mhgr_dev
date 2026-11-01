// [Mahjar partner-agreement]
using Nop.Core.Domain.Mahjar.Agreements;
using Nop.Core.Domain.Vendors;

namespace Nop.Services.Mahjar.Agreements;

/// <summary>
/// [Mahjar partner-agreement] Serves the two master partner agreements — "اتفاقية الشريك الفرد" and
/// "اتفاقية الشريك التجاري (المؤسسات والشركات)" — and records their acceptance.
///
/// The body lives in <see cref="PartnerAgreementSettings"/> as a localized setting; the "[●]" blanks of the
/// owner's documents are %Partner.* / %Store.* / %Agreement.* tokens resolved here, from the commercial
/// schedule in those settings and from the applicant's own details. What the applicant actually saw is frozen
/// onto the partner record at acceptance, together with the version, the moment, the IP, the method and a
/// technical reference — the "سجل القبول الإلكتروني" the agreements' closing table asks for.
/// </summary>
public partial interface IPartnerAgreementService
{
    /// <summary>
    /// Gets the agreement body for a partner type, in a language, with its tokens still unresolved
    /// </summary>
    /// <param name="vendorType">The partner type deciding which of the two documents applies</param>
    /// <param name="languageId">Language identifier</param>
    /// <returns>A task that represents the asynchronous operation; the raw HTML body, or null when there is none</returns>
    Task<string> GetRawBodyAsync(VendorType vendorType, int languageId);

    /// <summary>
    /// Gets the agreement body for a partner type, in a language, with every token resolved
    /// </summary>
    /// <param name="context">Who the agreement is being rendered for, and which services they activated</param>
    /// <param name="languageId">Language identifier</param>
    /// <returns>A task that represents the asynchronous operation; the rendered HTML, or null when there is no body</returns>
    Task<string> RenderAsync(PartnerAgreementContext context, int languageId);

    /// <summary>
    /// Stamps the acceptance of an agreement onto a partner: the document, the version, the moment, the IP,
    /// the method, a technical reference, and the agreement text frozen with all of those printed into its
    /// acceptance table. The caller saves the vendor.
    /// </summary>
    /// <param name="vendor">The partner accepting — mutated in place, not saved</param>
    /// <param name="context">Who is accepting and which services they activated</param>
    /// <param name="acceptanceMethod">How it was accepted, e.g. the web form or the mobile application</param>
    /// <param name="ipAddress">The IP address the acceptance came from; may be null</param>
    /// <param name="languageId">The language the applicant read the agreement in</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task RecordAcceptanceAsync(Vendor vendor, PartnerAgreementContext context,
        string acceptanceMethod, string ipAddress, int languageId);

    /// <summary>
    /// Gets a value indicating whether a partner has accepted the version of the agreement currently in force
    /// </summary>
    /// <param name="vendor">The partner</param>
    /// <returns>True when the accepted version equals the version in the settings</returns>
    bool HasAcceptedCurrentVersion(Vendor vendor);

    /// <summary>
    /// Gets the names of every token the renderer resolves, e.g. "%Partner.PrepTime%". The admin field picker
    /// offers only these, so a field it lists can never be one that would print literally.
    /// </summary>
    /// <param name="languageId">Language identifier</param>
    /// <returns>A task that represents the asynchronous operation; the token names</returns>
    Task<IReadOnlyCollection<string>> GetKnownTokenNamesAsync(int languageId);

    /// <summary>
    /// Gets the localized name of a set of activated services ("التأجير" / "البيع" / "كلاهما")
    /// </summary>
    /// <param name="services">The activated services</param>
    /// <param name="languageId">Language identifier</param>
    /// <returns>A task that represents the asynchronous operation; the localized text</returns>
    Task<string> GetServicesNameAsync(PartnerServices services, int languageId);
}
