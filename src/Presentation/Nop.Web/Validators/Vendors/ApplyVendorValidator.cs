using FluentValidation;
using Nop.Core.Domain.Vendors;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;
using Nop.Web.Models.Vendors;

namespace Nop.Web.Validators.Vendors;

public partial class ApplyVendorValidator : BaseNopValidator<ApplyVendorModel>
{
    public ApplyVendorValidator(ILocalizationService localizationService)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessageAwait(localizationService.GetResourceAsync("Vendors.ApplyAccount.Name.Required"));

        RuleFor(x => x.Email).NotEmpty().WithMessageAwait(localizationService.GetResourceAsync("Vendors.ApplyAccount.Email.Required"));
        RuleFor(x => x.Email)
            .IsEmailAddress()
            .WithMessageAwait(localizationService.GetResourceAsync("Common.WrongEmail"));

        //[Mahjar vendor-application] the applicant must declare a partner type, and the identity document that
        //goes with it: a National ID / Iqama for an individual, a commercial registration for a corporation.
        //Terms acceptance is NOT checked here — it depends on VendorSettings.TermsOfServiceEnabled, which only
        //the controller can read safely (a posted model flag would be client-controlled).
        RuleFor(x => x.VendorTypeId)
            .Must(vendorTypeId => vendorTypeId == (int)VendorType.Individual || vendorTypeId == (int)VendorType.Corporation)
            .WithMessageAwait(localizationService.GetResourceAsync("Vendors.ApplyAccount.VendorType.Required"));

        RuleFor(x => x.NationalId)
            .NotEmpty()
            .WithMessageAwait(localizationService.GetResourceAsync("Vendors.ApplyAccount.NationalId.Required"))
            .When(x => x.VendorTypeId == (int)VendorType.Individual);

        RuleFor(x => x.CommercialRegistrationNumber)
            .NotEmpty()
            .WithMessageAwait(localizationService.GetResourceAsync("Vendors.ApplyAccount.CommercialRegistrationNumber.Required"))
            .When(x => x.VendorTypeId == (int)VendorType.Corporation);
    }
}
