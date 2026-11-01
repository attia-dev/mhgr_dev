using System.ComponentModel.DataAnnotations;
using Nop.Web.Areas.Admin.Models.Vendors;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Models.Settings;

/// <summary>
/// Represents a vendor settings model
/// </summary>
public partial record VendorSettingsModel : BaseNopModel, ISettingsModel, ILocalizedModel<VendorSettingsLocalizedModel>
{
    #region Ctor

    public VendorSettingsModel()
    {
        VendorAttributeSearchModel = new VendorAttributeSearchModel();
        Locales = new List<VendorSettingsLocalizedModel>();
    }

    #endregion

    #region Properties

    public int ActiveStoreScopeConfiguration { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.VendorsBlockItemsToDisplay")]
    public int VendorsBlockItemsToDisplay { get; set; }
    public bool VendorsBlockItemsToDisplay_OverrideForStore { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.ShowVendorOnProductDetailsPage")]
    public bool ShowVendorOnProductDetailsPage { get; set; }
    public bool ShowVendorOnProductDetailsPage_OverrideForStore { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.AllowCustomersToContactVendors")]
    public bool AllowCustomersToContactVendors { get; set; }
    public bool AllowCustomersToContactVendors_OverrideForStore { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.AllowCustomersToApplyForVendorAccount")]
    public bool AllowCustomersToApplyForVendorAccount { get; set; }
    public bool AllowCustomersToApplyForVendorAccount_OverrideForStore { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.TermsOfServiceEnabled")]
    public bool TermsOfServiceEnabled { get; set; }
    public bool TermsOfServiceEnabled_OverrideForStore { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.AllowSearchByVendor")]
    public bool AllowSearchByVendor { get; set; }
    public bool AllowSearchByVendor_OverrideForStore { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.AllowVendorsToEditInfo")]
    public bool AllowVendorsToEditInfo { get; set; }
    public bool AllowVendorsToEditInfo_OverrideForStore { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.NotifyStoreOwnerAboutVendorInformationChange")]
    public bool NotifyStoreOwnerAboutVendorInformationChange { get; set; }
    public bool NotifyStoreOwnerAboutVendorInformationChange_OverrideForStore { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.MaximumProductNumber")]
    public int MaximumProductNumber { get; set; }
    public bool MaximumProductNumber_OverrideForStore { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.AllowVendorsToImportProducts")]
    public bool AllowVendorsToImportProducts { get; set; }
    public bool AllowVendorsToImportProducts_OverrideForStore { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.ShowVendorOnOrderDetailsPage")]
    public bool ShowVendorOnOrderDetailsPage { get; set; }
    public bool ShowVendorOnOrderDetailsPage_OverrideForStore { get; set; }

    //[Mahjar vendor-application] the terms body is a localized setting: one value per language
    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.TermsOfService")]
    public string TermsOfService { get; set; }
    public bool TermsOfService_OverrideForStore { get; set; }

    //[Mahjar partner-agreement] the two master partner agreements and the Annex A schedule printed into them.
    //The bodies are localized (Arabic prevails); everything else is one store-wide value shown to every partner
    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.PartnerAgreementEnabled")]
    public bool PartnerAgreementEnabled { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.PartnerAgreementVersion")]
    public string PartnerAgreementVersion { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.PartnerAgreementEffectiveDate")]
    [UIHint("DateNullable")]
    public DateTime? PartnerAgreementEffectiveDateUtc { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.IndividualBody")]
    public string IndividualBody { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.CorporateBody")]
    public string CorporateBody { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.OperatorNationalAddress")]
    public string OperatorNationalAddress { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.VerificationMethod")]
    public string VerificationMethod { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.RentalCommission")]
    public string RentalCommission { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.SaleCommission")]
    public string SaleCommission { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.ListingFee")]
    public string ListingFee { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.InspectionFee")]
    public string InspectionFee { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.DeliveryFee")]
    public string DeliveryFee { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.StorageFee")]
    public string StorageFee { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.MinimumSettlement")]
    public string MinimumSettlement { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.PreparationTime")]
    public string PreparationTime { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.SettlementCycle")]
    public string SettlementCycle { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.SettlementAccountTerms")]
    public string SettlementAccountTerms { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.ClaimResponseDays")]
    public int ClaimResponseDays { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.TerminationNoticeDays")]
    public int TerminationNoticeDays { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.AmicableSettlementDays")]
    public int AmicableSettlementDays { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.ReturnInspectionDays")]
    public int ReturnInspectionDays { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.AgreementTerm")]
    public string AgreementTerm { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.RenewalBasis")]
    public string RenewalBasis { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.CommercialNotes")]
    public string CommercialNotes { get; set; }

    //[Mahjar partner-agreement] the field picker offered above the agreement editors, grouped by where each
    //value comes from. Display-only — the page never posts it back
    public IList<PartnerAgreementTokenGroupModel> AgreementTokenGroups { get; set; }

    public IList<VendorSettingsLocalizedModel> Locales { get; set; }

    public VendorAttributeSearchModel VendorAttributeSearchModel { get; set; }

    #endregion
}

/// <summary>
/// [Mahjar partner-agreement] One group of insertable agreement fields, named after where its values come from
/// </summary>
public partial record PartnerAgreementTokenGroupModel
{
    public PartnerAgreementTokenGroupModel()
    {
        Tokens = new List<PartnerAgreementTokenModel>();
    }

    /// <summary>Gets or sets the group heading, localized</summary>
    public string Title { get; set; }

    /// <summary>Gets or sets the line saying where the values of this group are set, localized</summary>
    public string Hint { get; set; }

    /// <summary>Gets or sets the fields of the group</summary>
    public IList<PartnerAgreementTokenModel> Tokens { get; set; }
}

/// <summary>
/// [Mahjar partner-agreement] One insertable agreement field: what it is called, and what gets inserted
/// </summary>
public partial record PartnerAgreementTokenModel
{
    /// <summary>Gets or sets the token exactly as it goes into the body, e.g. "%Partner.PrepTime%"</summary>
    public string Token { get; set; }

    /// <summary>Gets or sets the field's name in the admin's own language</summary>
    public string Title { get; set; }
}

/// <summary>
/// Represents a localized vendor settings model [Mahjar vendor-application]
/// </summary>
public partial record VendorSettingsLocalizedModel : ILocalizedLocaleModel
{
    public int LanguageId { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.TermsOfService")]
    public string TermsOfService { get; set; }

    //[Mahjar partner-agreement] the Arabic body is the legally prevailing text, so each agreement is edited
    //per language exactly like the short terms above it
    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.IndividualBody")]
    public string IndividualBody { get; set; }

    [NopResourceDisplayName("Admin.Configuration.Settings.Vendor.CorporateBody")]
    public string CorporateBody { get; set; }
}