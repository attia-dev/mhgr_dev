using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Catalog;
using Nop.Web.Areas.Admin.Models.Common;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Models.Vendors;

/// <summary>
/// Represents a vendor model
/// </summary>
public partial record VendorModel : BaseNopEntityModel, ILocalizedModel<VendorLocalizedModel>
{
    #region Ctor

    public VendorModel()
    {
        if (PageSize < 1)
            PageSize = 5;

        Address = new AddressModel();
        VendorAttributes = new List<VendorAttributeModel>();
        Locales = new List<VendorLocalizedModel>();
        AssociatedCustomers = new List<VendorAssociatedCustomerModel>();
        VendorNoteSearchModel = new VendorNoteSearchModel();

        //[Mahjar vendor-application] partner type options shown on the vendor (partner) record
        AvailableVendorTypes = new List<SelectListItem>();
    }

    #endregion

    #region Properties

    [NopResourceDisplayName("Admin.Vendors.Fields.Name")]
    public string Name { get; set; }

    [DataType(DataType.EmailAddress)]
    [NopResourceDisplayName("Admin.Vendors.Fields.Email")]
    public string Email { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.Description")]
    public string Description { get; set; }

    [UIHint("Picture")]
    [NopResourceDisplayName("Admin.Vendors.Fields.Picture")]
    public int PictureId { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.AdminComment")]
    public string AdminComment { get; set; }

    public AddressModel Address { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.Active")]
    public bool Active { get; set; }

    //[Mahjar vendor-application] identity of the partner as declared on the "become a partner" application:
    //an individual (National ID / Iqama) or a corporation (commercial registration), plus the terms acceptance stamp
    [NopResourceDisplayName("Admin.Vendors.Fields.VendorType")]
    public int VendorTypeId { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.VendorType")]
    public string VendorTypeName { get; set; }

    public IList<SelectListItem> AvailableVendorTypes { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.NationalId")]
    public string NationalId { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.CommercialRegistrationNumber")]
    public string CommercialRegistrationNumber { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.VatNumber")]
    public string VatNumber { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.TermsAcceptedOn")]
    public DateTime? TermsAcceptedOnUtc { get; set; }

    //[Mahjar partner-agreement] the acceptance record of the master partner agreement, shown read-only: which
    //document, which version, when, from where, how, and the technical reference of the acceptance. The text the
    //partner accepted is frozen on the record and can be read back from the link this block carries
    [NopResourceDisplayName("Admin.Vendors.Fields.AuthorizedSignatoryName")]
    public string AuthorizedSignatoryName { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.AuthorizedSignatoryCapacity")]
    public string AuthorizedSignatoryCapacity { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.ActivatedServices")]
    public int ActivatedServicesId { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.ActivatedServices")]
    public string ActivatedServicesName { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.AgreementDocument")]
    public string AgreementDocumentName { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.AgreementVersion")]
    public string AgreementVersion { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.AgreementAcceptedOn")]
    public DateTime? AgreementAcceptedOnUtc { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.AgreementAcceptedIp")]
    public string AgreementAcceptedIp { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.AgreementAcceptanceMethod")]
    public string AgreementAcceptanceMethod { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.AgreementTechnicalRef")]
    public string AgreementTechnicalRef { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.AgreementBodyHash")]
    public string AgreementBodyHash { get; set; }

    /// <summary>Whether a frozen copy of the accepted agreement exists and can be opened.</summary>
    public bool HasAgreementBody { get; set; }

    /// <summary>Whether the accepted version is still the one in force — false means the partner must re-accept.</summary>
    public bool AgreementIsCurrent { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.MetaKeywords")]
    public string MetaKeywords { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.MetaDescription")]
    public string MetaDescription { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.MetaTitle")]
    public string MetaTitle { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.SeName")]
    public string SeName { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.PageSize")]
    public int PageSize { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.AllowCustomersToSelectPageSize")]
    public bool AllowCustomersToSelectPageSize { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.PageSizeOptions")]
    public string PageSizeOptions { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.PmCustomerId")]
    public int? PmCustomerId { get; set; }
    public string PmCustomerInfo { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.PriceRangeFiltering")]
    public bool PriceRangeFiltering { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.PriceFrom")]
    public decimal PriceFrom { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.PriceTo")]
    public decimal PriceTo { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.ManuallyPriceRange")]
    public bool ManuallyPriceRange { get; set; }

    public List<VendorAttributeModel> VendorAttributes { get; set; }

    public IList<VendorLocalizedModel> Locales { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.AssociatedCustomerEmails")]
    public IList<VendorAssociatedCustomerModel> AssociatedCustomers { get; set; }

    //vendor notes
    [NopResourceDisplayName("Admin.Vendors.VendorNotes.Fields.Note")]
    public string AddVendorNoteMessage { get; set; }

    public VendorNoteSearchModel VendorNoteSearchModel { get; set; }

    public string PrimaryStoreCurrencyCode { get; set; }

    #endregion

    #region Nested classes

    public partial record VendorAttributeModel : BaseNopEntityModel
    {
        public VendorAttributeModel()
        {
            Values = new List<VendorAttributeValueModel>();
        }

        public string Name { get; set; }

        public bool IsRequired { get; set; }

        /// <summary>
        /// Default value for textboxes
        /// </summary>
        public string DefaultValue { get; set; }

        public AttributeControlType AttributeControlType { get; set; }

        public IList<VendorAttributeValueModel> Values { get; set; }
    }

    public partial record VendorAttributeValueModel : BaseNopEntityModel
    {
        public string Name { get; set; }

        public bool IsPreSelected { get; set; }
    }

    #endregion
}

public partial record VendorLocalizedModel : ILocalizedLocaleModel
{
    public int LanguageId { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.Name")]
    public string Name { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.Description")]
    public string Description { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.MetaKeywords")]
    public string MetaKeywords { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.MetaDescription")]
    public string MetaDescription { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.MetaTitle")]
    public string MetaTitle { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.SeName")]
    public string SeName { get; set; }
}