using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Models.Vendors;

public partial record ApplyVendorModel : BaseNopModel
{
    public ApplyVendorModel()
    {
        VendorAttributes = new List<VendorAttributeModel>();

        //[Mahjar vendor-application] partner type options (individual / corporation)
        AvailableVendorTypes = new List<SelectListItem>();
    }

    [NopResourceDisplayName("Vendors.ApplyAccount.Name")]
    public string Name { get; set; }

    [DataType(DataType.EmailAddress)]
    [NopResourceDisplayName("Vendors.ApplyAccount.Email")]
    public string Email { get; set; }

    [NopResourceDisplayName("Vendors.ApplyAccount.Description")]
    public string Description { get; set; }

    //[Mahjar vendor-application] the applicant declares whether they are a private person or a registered
    //company, and supplies the matching KSA identity document; the store owner sees both with the request
    [NopResourceDisplayName("Vendors.ApplyAccount.VendorType")]
    public int VendorTypeId { get; set; }

    public IList<SelectListItem> AvailableVendorTypes { get; set; }

    [NopResourceDisplayName("Vendors.ApplyAccount.NationalId")]
    public string NationalId { get; set; }

    [NopResourceDisplayName("Vendors.ApplyAccount.CommercialRegistrationNumber")]
    public string CommercialRegistrationNumber { get; set; }

    [NopResourceDisplayName("Vendors.ApplyAccount.VatNumber")]
    public string VatNumber { get; set; }

    //[Mahjar partner-agreement] the corporate agreement names the person authorised to sign for the company,
    //so the corporation branch of the form collects them; both fields are ignored for an individual
    [NopResourceDisplayName("Vendors.ApplyAccount.AuthorizedSignatoryName")]
    public string AuthorizedSignatoryName { get; set; }

    [NopResourceDisplayName("Vendors.ApplyAccount.AuthorizedSignatoryCapacity")]
    public string AuthorizedSignatoryCapacity { get; set; }

    //[Mahjar partner-agreement] the "الخدمات المفعلة" row of the agreement: renting out, selling, or both.
    //At least one is required, and the pair decides which service annexes the applicant is accepting
    [NopResourceDisplayName("Vendors.ApplyAccount.ActivateRental")]
    public bool ActivateRental { get; set; }

    [NopResourceDisplayName("Vendors.ApplyAccount.ActivateSale")]
    public bool ActivateSale { get; set; }

    public IList<VendorAttributeModel> VendorAttributes { get; set; }

    public bool DisplayCaptcha { get; set; }

    public bool TermsOfServiceEnabled { get; set; }
    public bool TermsOfServicePopup { get; set; }

    //[Mahjar vendor-application] acceptance is bound to the model and enforced server-side (stock nopCommerce
    //only had a JavaScript alert); TermsOfServiceHtml is the admin-editable body rendered on the form
    [NopResourceDisplayName("Vendors.ApplyAccount.AcceptTermsOfService")]
    public bool AcceptTermsOfService { get; set; }

    public string TermsOfServiceHtml { get; set; }

    //[Mahjar partner-agreement] when the master agreements are enabled the form shows the whole document of the
    //selected partner type instead of the short terms body. Both are rendered server-side and the page swaps
    //between them as the applicant changes their type, so no round-trip is needed
    public bool AgreementEnabled { get; set; }

    public string AgreementVersion { get; set; }

    public string IndividualAgreementHtml { get; set; }

    public string CorporateAgreementHtml { get; set; }

    //[Mahjar partner-request] when a previous application was rejected the customer may apply again, and is
    //told why so they know what to correct rather than resubmitting the same thing
    public bool PreviousApplicationRejected { get; set; }

    public string PreviousApplicationRejectionReason { get; set; }

    public bool DisableFormInput { get; set; }
    public string Result { get; set; }
}
