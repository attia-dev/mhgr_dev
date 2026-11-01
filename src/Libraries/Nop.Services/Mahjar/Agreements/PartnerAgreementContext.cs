// [Mahjar partner-agreement]
using Nop.Core.Domain.Mahjar.Agreements;
using Nop.Core.Domain.Vendors;

namespace Nop.Services.Mahjar.Agreements;

/// <summary>
/// [Mahjar partner-agreement] Who a partner agreement is being rendered for. The applicant's side of the
/// header table — name, identity documents, activated services — comes from here; the platform's side and the
/// whole commercial schedule come from the settings.
///
/// It is a plain record rather than the <see cref="Vendor"/> entity because the agreement is shown and
/// accepted on the application form, before any vendor row exists.
/// </summary>
public partial record PartnerAgreementContext
{
    /// <summary>Gets the partner type, which decides which of the two documents is rendered</summary>
    public VendorType VendorType { get; init; }

    /// <summary>Gets the partner's name as declared on the application</summary>
    public string PartnerName { get; init; }

    /// <summary>Gets the partner's approved contact e-mail</summary>
    public string Email { get; init; }

    /// <summary>Gets the identifier of the customer account behind the partner ("معرف المستخدم")</summary>
    public int CustomerId { get; init; }

    /// <summary>Gets the National ID / Iqama number (individuals)</summary>
    public string NationalId { get; init; }

    /// <summary>Gets the commercial registration number (corporations)</summary>
    public string CommercialRegistrationNumber { get; init; }

    /// <summary>Gets the VAT registration number</summary>
    public string VatNumber { get; init; }

    /// <summary>Gets the name of the person authorised to sign (corporations)</summary>
    public string AuthorizedSignatoryName { get; init; }

    /// <summary>Gets the capacity the authorised signatory signs in (corporations)</summary>
    public string AuthorizedSignatoryCapacity { get; init; }

    /// <summary>Gets the services being activated</summary>
    public PartnerServices Services { get; init; }

    /// <summary>
    /// Gets the moment of acceptance. Null while the agreement is only being shown — the closing acceptance
    /// table then prints a dash, and the values are filled in on the copy that is frozen at acceptance
    /// </summary>
    public DateTime? AcceptedOnUtc { get; init; }

    /// <summary>Gets how the agreement was accepted ("وسيلة القبول الإلكتروني"); null while it is only shown</summary>
    public string AcceptanceMethod { get; init; }

    /// <summary>Gets the technical reference of the acceptance; null while the agreement is only being shown</summary>
    public string TechnicalRef { get; init; }

    /// <summary>
    /// Gets a value indicating whether the applicant's own fields are wrapped in a marker element instead of
    /// being written as plain text. The application form sets it so the agreement it displays can follow what
    /// the applicant types, field by field, without a round trip. It is never set on the copy that is frozen at
    /// acceptance — that one is the plain, final text.
    /// </summary>
    public bool MarkLiveFields { get; init; }

    /// <summary>
    /// Builds the context of a partner that already exists — used to re-print what a partner accepted, or to
    /// render the agreement in force for them
    /// </summary>
    /// <param name="vendor">The partner</param>
    /// <param name="customerId">The identifier of the customer account behind the partner</param>
    /// <returns>The context</returns>
    public static PartnerAgreementContext FromVendor(Vendor vendor, int customerId = 0)
    {
        ArgumentNullException.ThrowIfNull(vendor);

        return new PartnerAgreementContext
        {
            VendorType = vendor.VendorType,
            PartnerName = vendor.Name,
            Email = vendor.Email,
            CustomerId = customerId,
            NationalId = vendor.NationalId,
            CommercialRegistrationNumber = vendor.CommercialRegistrationNumber,
            VatNumber = vendor.VatNumber,
            AuthorizedSignatoryName = vendor.AuthorizedSignatoryName,
            AuthorizedSignatoryCapacity = vendor.AuthorizedSignatoryCapacity,
            Services = vendor.ActivatedServices,
            AcceptedOnUtc = vendor.AgreementAcceptedOnUtc,
            AcceptanceMethod = vendor.AgreementAcceptanceMethod,
            TechnicalRef = vendor.AgreementTechnicalRef
        };
    }
}
