using Nop.Core.Domain.Common;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Seo;

namespace Nop.Core.Domain.Vendors;

/// <summary>
/// Represents a vendor
/// </summary>
public partial class Vendor : BaseEntity, ILocalizedEntity, ISlugSupported, ISoftDeletedEntity
{
    /// <summary>
    /// Gets or sets the name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the email
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// Gets or sets the description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the picture identifier
    /// </summary>
    public int PictureId { get; set; }

    /// <summary>
    /// Gets or sets the address identifier
    /// </summary>
    public int AddressId { get; set; }

    /// <summary>
    /// Gets or sets the admin comment
    /// </summary>
    public string AdminComment { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the entity is active
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the entity has been deleted
    /// </summary>
    public bool Deleted { get; set; }

    /// <summary>
    /// Gets or sets the display order
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the meta keywords
    /// </summary>
    public string MetaKeywords { get; set; }

    /// <summary>
    /// Gets or sets the meta description
    /// </summary>
    public string MetaDescription { get; set; }

    /// <summary>
    /// Gets or sets the meta title
    /// </summary>
    public string MetaTitle { get; set; }

    /// <summary>
    /// Gets or sets the page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether customers can select the page size
    /// </summary>
    public bool AllowCustomersToSelectPageSize { get; set; }

    /// <summary>
    /// Gets or sets the available customer selectable page size options
    /// </summary>
    public string PageSizeOptions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the price range filtering is enabled
    /// </summary>
    public bool PriceRangeFiltering { get; set; }

    /// <summary>
    /// Gets or sets the "from" price
    /// </summary>
    public decimal PriceFrom { get; set; }

    /// <summary>
    /// Gets or sets the "to" price
    /// </summary>
    public decimal PriceTo { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the price range should be entered manually
    /// </summary>
    public bool ManuallyPriceRange { get; set; }

    /// <summary>
    /// Gets or sets the customer identifier
    /// </summary>
    public int? PmCustomerId { get; set; }

    /// <summary>
    /// [Mahjar vendor-application] Gets or sets the partner type identifier (see <see cref="VendorType"/>):
    /// an individual trading under their own name, or a registered company
    /// </summary>
    public int VendorTypeId { get; set; }

    /// <summary>
    /// [Mahjar vendor-application] Gets or sets the national ID / Iqama number (individuals only)
    /// </summary>
    public string NationalId { get; set; }

    /// <summary>
    /// [Mahjar vendor-application] Gets or sets the commercial registration (CR) number (corporations only)
    /// </summary>
    public string CommercialRegistrationNumber { get; set; }

    /// <summary>
    /// [Mahjar vendor-application] Gets or sets the VAT registration number (optional for both types)
    /// </summary>
    public string VatNumber { get; set; }

    /// <summary>
    /// [Mahjar vendor-application] Gets or sets the date and time (UTC) the applicant accepted the partner
    /// terms and conditions; null when the application predates the terms gate or terms were not enabled
    /// </summary>
    public DateTime? TermsAcceptedOnUtc { get; set; }

    /// <summary>
    /// [Mahjar partner-agreement] Gets or sets the services the partner activated (see
    /// <see cref="Nop.Core.Domain.Mahjar.Agreements.PartnerServices"/>, a flags value): renting out, selling,
    /// or both. It is the "الخدمات المفعلة" row of the partner agreement, and it decides which of the
    /// agreement's service annexes binds the partner
    /// </summary>
    public int ActivatedServicesId { get; set; }

    /// <summary>
    /// [Mahjar partner-agreement] Gets or sets the name of the person authorised to sign for the partner
    /// (corporations only — the corporate agreement names them in its header)
    /// </summary>
    public string AuthorizedSignatoryName { get; set; }

    /// <summary>
    /// [Mahjar partner-agreement] Gets or sets the capacity the authorised signatory signs in — owner,
    /// manager, holder of a power of attorney (corporations only)
    /// </summary>
    public string AuthorizedSignatoryCapacity { get; set; }

    /// <summary>
    /// [Mahjar partner-request] Gets or sets where the application stands — see
    /// <see cref="Nop.Core.Domain.Mahjar.Agreements.PartnerApplicationStatus"/>. Zero means this partner was
    /// created directly in admin and never was an application
    /// </summary>
    public int ApplicationStatusId { get; set; }

    /// <summary>
    /// [Mahjar partner-request] Gets or sets the customer who submitted the application. It is kept even after
    /// a rejection clears <c>Customer.VendorId</c> to let them apply again, so the record still says who applied
    /// </summary>
    public int ApplicationCustomerId { get; set; }

    /// <summary>
    /// [Mahjar partner-request] Gets or sets when the application was submitted (UTC)
    /// </summary>
    public DateTime? ApplicationSubmittedOnUtc { get; set; }

    /// <summary>
    /// [Mahjar partner-request] Gets or sets when the store owner approved or rejected it (UTC)
    /// </summary>
    public DateTime? ApplicationReviewedOnUtc { get; set; }

    /// <summary>
    /// [Mahjar partner-request] Gets or sets the reason the application was rejected, shown to the applicant so
    /// they know what to correct before applying again
    /// </summary>
    public string ApplicationRejectionReason { get; set; }

    /// <summary>
    /// [Mahjar partner-agreement] Gets or sets which master agreement was accepted — see
    /// <see cref="Nop.Core.Domain.Mahjar.Agreements.PartnerAgreementDocument"/>; null when the partner
    /// predates the agreement gate and only accepted the old short terms
    /// </summary>
    public string AgreementDocumentKey { get; set; }

    /// <summary>
    /// [Mahjar partner-agreement] Gets or sets the version of the agreement that was accepted ("1.0"). A
    /// partner whose accepted version is behind the current one has not accepted the text in force
    /// </summary>
    public string AgreementVersion { get; set; }

    /// <summary>
    /// [Mahjar partner-agreement] Gets or sets the moment the master agreement was accepted (UTC)
    /// </summary>
    public DateTime? AgreementAcceptedOnUtc { get; set; }

    /// <summary>
    /// [Mahjar partner-agreement] Gets or sets the IP address the acceptance was made from
    /// </summary>
    public string AgreementAcceptedIp { get; set; }

    /// <summary>
    /// [Mahjar partner-agreement] Gets or sets how the agreement was accepted ("وسيلة القبول الإلكتروني") —
    /// e.g. the web application form or the mobile application
    /// </summary>
    public string AgreementAcceptanceMethod { get; set; }

    /// <summary>
    /// [Mahjar partner-agreement] Gets or sets the text of the agreement exactly as it was shown and accepted,
    /// frozen at that moment. The settings body an admin edits afterwards must never change what a partner is
    /// held to, which is why the accepted text is copied here rather than referenced
    /// </summary>
    public string AgreementBody { get; set; }

    /// <summary>
    /// [Mahjar partner-agreement] Gets or sets the SHA-256 hash (uppercase hex) of <see cref="AgreementBody"/>,
    /// the short proof that the stored text is the accepted one
    /// </summary>
    public string AgreementBodyHash { get; set; }

    /// <summary>
    /// [Mahjar partner-agreement] Gets or sets the technical reference of the acceptance
    /// ("سجل/مرجع العملية التقنية") — a GUID identifying this acceptance in the store's records
    /// </summary>
    public string AgreementTechnicalRef { get; set; }

    /// <summary>
    /// [Mahjar vendor-application] Gets or sets the partner type
    /// </summary>
    public VendorType VendorType
    {
        get => (VendorType)VendorTypeId;
        set => VendorTypeId = (int)value;
    }

    /// <summary>
    /// [Mahjar partner-request] Gets or sets where the application stands
    /// </summary>
    public Nop.Core.Domain.Mahjar.Agreements.PartnerApplicationStatus ApplicationStatus
    {
        get => (Nop.Core.Domain.Mahjar.Agreements.PartnerApplicationStatus)ApplicationStatusId;
        set => ApplicationStatusId = (int)value;
    }

    /// <summary>
    /// [Mahjar partner-agreement] Gets or sets the activated services
    /// </summary>
    public Nop.Core.Domain.Mahjar.Agreements.PartnerServices ActivatedServices
    {
        get => (Nop.Core.Domain.Mahjar.Agreements.PartnerServices)ActivatedServicesId;
        set => ActivatedServicesId = (int)value;
    }
}