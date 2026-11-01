// [Mahjar partner-request]
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Models.Mahjar;

/// <summary>
/// Represents a partner request search model
/// </summary>
public partial record PartnerRequestSearchModel : BaseSearchModel
{
    #region Ctor

    public PartnerRequestSearchModel()
    {
        AvailableStatuses = new List<SelectListItem>();
    }

    #endregion

    #region Properties

    [NopResourceDisplayName("Admin.Mahjar.PartnerRequests.List.SearchStatus")]
    public int StatusId { get; set; }

    [NopResourceDisplayName("Admin.Mahjar.PartnerRequests.List.SearchName")]
    public string SearchKeyword { get; set; }

    public IList<SelectListItem> AvailableStatuses { get; set; }

    #endregion
}

/// <summary>
/// Represents a partner request list model
/// </summary>
public partial record PartnerRequestListModel : BasePagedListModel<PartnerRequestModel>
{
}

/// <summary>
/// Represents a partner request model — one application, as the queue and the review screen show it
/// </summary>
public partial record PartnerRequestModel : BaseNopEntityModel
{
    #region Properties

    [NopResourceDisplayName("Admin.Mahjar.PartnerRequests.Fields.Name")]
    public string Name { get; set; }

    [NopResourceDisplayName("Admin.Mahjar.PartnerRequests.Fields.Email")]
    public string Email { get; set; }

    [NopResourceDisplayName("Admin.Mahjar.PartnerRequests.Fields.Description")]
    public string Description { get; set; }

    [NopResourceDisplayName("Admin.Mahjar.PartnerRequests.Fields.VendorType")]
    public string VendorTypeName { get; set; }

    [NopResourceDisplayName("Admin.Mahjar.PartnerRequests.Fields.Services")]
    public string ActivatedServicesName { get; set; }

    [NopResourceDisplayName("Admin.Mahjar.PartnerRequests.Fields.Status")]
    public string StatusName { get; set; }

    public int StatusId { get; set; }

    [NopResourceDisplayName("Admin.Mahjar.PartnerRequests.Fields.SubmittedOn")]
    public DateTime? SubmittedOnUtc { get; set; }

    [NopResourceDisplayName("Admin.Mahjar.PartnerRequests.Fields.ReviewedOn")]
    public DateTime? ReviewedOnUtc { get; set; }

    [NopResourceDisplayName("Admin.Mahjar.PartnerRequests.Fields.RejectionReason")]
    public string RejectionReason { get; set; }

    [NopResourceDisplayName("Admin.Mahjar.PartnerRequests.Fields.Customer")]
    public string CustomerEmail { get; set; }

    public int CustomerId { get; set; }

    #region The declared identity, as submitted

    [NopResourceDisplayName("Admin.Vendors.Fields.NationalId")]
    public string NationalId { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.CommercialRegistrationNumber")]
    public string CommercialRegistrationNumber { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.VatNumber")]
    public string VatNumber { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.AuthorizedSignatoryName")]
    public string AuthorizedSignatoryName { get; set; }

    [NopResourceDisplayName("Admin.Vendors.Fields.AuthorizedSignatoryCapacity")]
    public string AuthorizedSignatoryCapacity { get; set; }

    #endregion

    #region The acceptance record

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

    /// <summary>Whether a frozen copy of the accepted agreement exists and can be opened</summary>
    public bool HasAgreementBody { get; set; }

    #endregion

    /// <summary>Whether the application is still waiting, and so can be approved or rejected</summary>
    public bool CanReview { get; set; }

    #endregion
}
