using System.Net;
using System.Threading.Tasks;
using MHGR.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Mahjar.Agreements; //[Mahjar partner-agreement]
using Nop.Core.Domain.Vendors;
using Nop.Services.Authentication;
using Nop.Services.Customers;
using Nop.Services.Html;
using Nop.Services.Localization;
using Nop.Services.Mahjar.Agreements; //[Mahjar partner-agreement]
using Nop.Services.Messages;
using Nop.Services.Seo;
using Nop.Services.Vendors;

namespace Nop.Web.Controllers;

/// <summary>
/// "Become a partner" API (bearer-token authenticated) — lets a logged-in BUYER apply for a vendor account from the
/// app, exactly like the web /vendor/apply form. It creates a DISABLED vendor and emails the store owner; an admin
/// still activates it + grants the Vendors role (partners manage listings via the web vendor panel, by design).
/// The application mirrors the web form's required fields (Name, Email, Description) plus the partner type and its
/// KSA identity document (National ID / Iqama for an individual, commercial registration number for a corporation),
/// an optional VAT number, and — when the store enables them — acceptance of the terms and conditions, which the
/// store owner edits in Admin -> Settings -> Vendor settings and the app renders from GET /api/customer/apply-vendor.
/// Image/captcha/vendor-attributes are skipped (all optional on the web). [Mahjar mobile-api]
/// </summary>
[IgnoreAntiforgeryToken]
public partial class VendorApplyApiController : Controller
{
    #region Fields

    protected readonly IApiTokenService _apiTokenService;
    protected readonly ICustomerService _customerService;
    protected readonly IHtmlFormatter _htmlFormatter;
    protected readonly ILocalizationService _localizationService; //[Mahjar i18n]
    protected readonly IPartnerAgreementService _partnerAgreementService; //[Mahjar partner-agreement]
    protected readonly IUrlRecordService _urlRecordService;
    protected readonly IVendorService _vendorService;
    protected readonly IWorkContext _workContext; //[Mahjar vendor-application] caller's language (Accept-Language)
    protected readonly IWorkflowMessageService _workflowMessageService;
    protected readonly LocalizationSettings _localizationSettings;
    protected readonly PartnerAgreementSettings _partnerAgreementSettings; //[Mahjar partner-agreement]
    protected readonly VendorSettings _vendorSettings;

    #endregion

    #region Ctor

    public VendorApplyApiController(IApiTokenService apiTokenService,
        ICustomerService customerService,
        IHtmlFormatter htmlFormatter,
        ILocalizationService localizationService,
        IPartnerAgreementService partnerAgreementService, //[Mahjar partner-agreement]
        IUrlRecordService urlRecordService,
        IVendorService vendorService,
        IWorkContext workContext,
        IWorkflowMessageService workflowMessageService,
        LocalizationSettings localizationSettings,
        PartnerAgreementSettings partnerAgreementSettings, //[Mahjar partner-agreement]
        VendorSettings vendorSettings)
    {
        _apiTokenService = apiTokenService;
        _customerService = customerService;
        _htmlFormatter = htmlFormatter;
        _localizationService = localizationService;
        _partnerAgreementService = partnerAgreementService;
        _urlRecordService = urlRecordService;
        _vendorService = vendorService;
        _workContext = workContext;
        _workflowMessageService = workflowMessageService;
        _localizationSettings = localizationSettings;
        _partnerAgreementSettings = partnerAgreementSettings;
        _vendorSettings = vendorSettings;
    }

    #endregion

    #region Utilities

    protected virtual async Task<Customer> GetTokenCustomerAsync()
    {
        return await _apiTokenService.GetAuthenticatedCustomerAsync(Request.Headers["Authorization"].ToString());
    }

    #endregion

    #region Methods

    // GET /api/customer/apply-vendor  -> can this customer apply, prefill email, terms + partner types
    [HttpGet]
    [ProducesResponseType(typeof(ApplyVendorStatusDto), 200)]
    public virtual async Task<IActionResult> Status()
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        //[Mahjar vendor-application] the app renders the terms body and the partner-type picker from this payload;
        //the working language honors the caller's Accept-Language header on /api requests (see WebWorkContext)
        var languageId = (await _workContext.GetWorkingLanguageAsync()).Id;

        var vendorTypes = new List<VendorTypeOptionDto>();
        foreach (var vendorType in new[] { VendorType.Individual, VendorType.Corporation })
            vendorTypes.Add(new VendorTypeOptionDto
            {
                Id = (int)vendorType,
                Name = await _localizationService.GetLocalizedEnumAsync(vendorType, languageId)
            });

        //[Mahjar partner-agreement] both documents are rendered so the app can swap between them offline; the
        //acceptance table stays blank here, it is filled on the copy frozen when the application is submitted
        var individualAgreement = (string)null;
        var corporateAgreement = (string)null;
        if (_vendorSettings.TermsOfServiceEnabled && _partnerAgreementSettings.Enabled)
        {
            var previewContext = new PartnerAgreementContext
            {
                Email = customer.Email,
                CustomerId = customer.Id
            };

            individualAgreement = await _partnerAgreementService.RenderAsync(
                previewContext with { VendorType = VendorType.Individual }, languageId);
            corporateAgreement = await _partnerAgreementService.RenderAsync(
                previewContext with { VendorType = VendorType.Corporation }, languageId);
        }

        var agreementEnabled = !string.IsNullOrWhiteSpace(individualAgreement) ||
                               !string.IsNullOrWhiteSpace(corporateAgreement);

        return Json(new ApplyVendorStatusDto
        {
            Enabled = _vendorSettings.AllowCustomersToApplyForVendorAccount,
            AlreadyApplied = customer.VendorId > 0,
            Email = customer.Email,
            //[Mahjar vendor-application] the terms body is a localized setting edited in admin
            TermsOfServiceEnabled = _vendorSettings.TermsOfServiceEnabled,
            TermsOfServiceHtml = _vendorSettings.TermsOfServiceEnabled
                ? await _localizationService.GetLocalizedSettingAsync(_vendorSettings, x => x.TermsOfService, languageId, storeId: 0)
                : null,
            VendorTypes = vendorTypes,
            //[Mahjar partner-agreement] the full master agreement of each type, with its commercial schedule
            //already filled in from the settings; the app shows the one matching the picked type and must send
            //back the activated services, which decide the annexes being accepted
            AgreementEnabled = agreementEnabled,
            AgreementVersion = agreementEnabled ? _partnerAgreementSettings.Version : null,
            IndividualAgreementHtml = individualAgreement,
            CorporateAgreementHtml = corporateAgreement
        });
    }

    // POST /api/customer/apply-vendor/agreement  { the draft application }  ->  { html }
    /// <summary>
    /// [Mahjar partner-agreement] Renders the agreement for an application that is still being filled in, so the
    /// app shows the applicant the document with THEIR details in it. The web form does the same thing in the
    /// browser, field by field; the app has no tokens to substitute because it is handed finished HTML, so the
    /// rendering happens here instead. Nothing is saved and nothing is validated — it is a preview.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AgreementPreviewDto), 200)]
    public virtual async Task<IActionResult> Agreement([FromBody] ApplyVendorApiRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        if (!_vendorSettings.TermsOfServiceEnabled || !_partnerAgreementSettings.Enabled)
            return Json(new AgreementPreviewDto { Html = null });

        var languageId = (await _workContext.GetWorkingLanguageAsync()).Id;
        var requestedVendorType = request?.VendorTypeId == (int)VendorType.Corporation
            ? VendorType.Corporation
            : VendorType.Individual;

        var context = new PartnerAgreementContext
        {
            VendorType = requestedVendorType,
            PartnerName = request?.Name?.Trim(),
            Email = string.IsNullOrWhiteSpace(request?.Email) ? customer.Email : request.Email.Trim(),
            CustomerId = customer.Id,
            //only the document that matches the declared type, exactly as the application itself stores them
            NationalId = requestedVendorType == VendorType.Individual ? request?.NationalId?.Trim() : null,
            CommercialRegistrationNumber = requestedVendorType == VendorType.Corporation ? request?.CommercialRegistrationNumber?.Trim() : null,
            VatNumber = request?.VatNumber?.Trim(),
            AuthorizedSignatoryName = requestedVendorType == VendorType.Corporation ? request?.AuthorizedSignatoryName?.Trim() : null,
            AuthorizedSignatoryCapacity = requestedVendorType == VendorType.Corporation ? request?.AuthorizedSignatoryCapacity?.Trim() : null,
            Services = ((request?.ActivateRental ?? false) ? PartnerServices.Rental : PartnerServices.None) |
                       ((request?.ActivateSale ?? false) ? PartnerServices.Sale : PartnerServices.None)
        };

        return Json(new AgreementPreviewDto { Html = await _partnerAgreementService.RenderAsync(context, languageId) });
    }

    // POST /api/customer/apply-vendor  { name, email, description, vendorTypeId, nationalId, commercialRegistrationNumber, vatNumber, acceptTermsOfService }
    [HttpPost]
    [ProducesResponseType(typeof(MessageResponseDto), 200)]
    public virtual async Task<IActionResult> Apply([FromBody] ApplyVendorApiRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        if (!_vendorSettings.AllowCustomersToApplyForVendorAccount)
            return BadRequest(new MessageResponseDto { Success = false, Message = await _localizationService.GetResourceAsync("mahjar.api.vendorapply.disabled") });

        if (await _customerService.IsAdminAsync(customer))
            return BadRequest(new MessageResponseDto { Success = false, Message = await _localizationService.GetResourceAsync("mahjar.api.vendorapply.adminsnotallowed") });

        if (customer.VendorId > 0)
            return BadRequest(new MessageResponseDto { Success = false, Message = await _localizationService.GetResourceAsync("mahjar.api.vendorapply.alreadyapplied") });

        if (request == null || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new MessageResponseDto { Success = false, Message = await _localizationService.GetResourceAsync("mahjar.api.vendorapply.namerequired") });

        if (string.IsNullOrWhiteSpace(request.Email) || !CommonHelper.IsValidEmail(request.Email))
            return BadRequest(new MessageResponseDto { Success = false, Message = await _localizationService.GetResourceAsync("mahjar.api.common.emailrequired") });

        //[Mahjar vendor-application] the declared partner type decides which KSA identity document is required
        //(exactly the rules the web /vendor/apply form enforces)
        if (request.VendorTypeId != (int)VendorType.Individual && request.VendorTypeId != (int)VendorType.Corporation)
            return BadRequest(new MessageResponseDto { Success = false, Message = await _localizationService.GetResourceAsync("mahjar.api.vendorapply.typerequired") });

        var requestedVendorType = (VendorType)request.VendorTypeId;

        if (requestedVendorType == VendorType.Individual && string.IsNullOrWhiteSpace(request.NationalId))
            return BadRequest(new MessageResponseDto { Success = false, Message = await _localizationService.GetResourceAsync("mahjar.api.vendorapply.nationalidrequired") });

        if (requestedVendorType == VendorType.Corporation && string.IsNullOrWhiteSpace(request.CommercialRegistrationNumber))
            return BadRequest(new MessageResponseDto { Success = false, Message = await _localizationService.GetResourceAsync("mahjar.api.vendorapply.crrequired") });

        //[Mahjar vendor-application] terms acceptance is enforced here, on the server (never trust the client)
        if (_vendorSettings.TermsOfServiceEnabled && !request.AcceptTermsOfService)
            return BadRequest(new MessageResponseDto { Success = false, Message = await _localizationService.GetResourceAsync("mahjar.api.vendorapply.termsrequired") });

        //[Mahjar partner-agreement] ...and so is activating at least one service: the agreement's annexes bind
        //the partner only for what they activated, so an application activating neither accepts nothing
        if (_partnerAgreementSettings.Enabled && !request.ActivateRental && !request.ActivateSale)
            return BadRequest(new MessageResponseDto { Success = false, Message = await _localizationService.GetResourceAsync("mahjar.api.vendorapply.servicesrequired") });

        var description = _htmlFormatter.FormatText(request.Description ?? string.Empty, false, false, true, false, false, false);

        //disabled by default — activating it in admin grants the Vendors role automatically ([Mahjar vendor-role])
        var vendor = new Vendor
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Active = false,
            PageSize = 6,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = _vendorSettings.DefaultVendorPageSizeOptions,
            Description = WebUtility.HtmlEncode(description),
            //[Mahjar vendor-application] store only the document that matches the declared partner type
            VendorTypeId = request.VendorTypeId,
            NationalId = requestedVendorType == VendorType.Individual ? request.NationalId.Trim() : null,
            CommercialRegistrationNumber = requestedVendorType == VendorType.Corporation ? request.CommercialRegistrationNumber.Trim() : null,
            VatNumber = string.IsNullOrWhiteSpace(request.VatNumber) ? null : request.VatNumber.Trim(),
            //[Mahjar partner-agreement] the corporate agreement names the signatory; an individual has none
            AuthorizedSignatoryName = requestedVendorType == VendorType.Corporation
                ? request.AuthorizedSignatoryName?.Trim()
                : null,
            AuthorizedSignatoryCapacity = requestedVendorType == VendorType.Corporation
                ? request.AuthorizedSignatoryCapacity?.Trim()
                : null,
            TermsAcceptedOnUtc = _vendorSettings.TermsOfServiceEnabled ? DateTime.UtcNow : null,
            //[Mahjar partner-request] queued for review in Admin -> Partner requests, exactly as the web form does
            ApplicationStatus = PartnerApplicationStatus.Pending,
            ApplicationCustomerId = customer.Id,
            ApplicationSubmittedOnUtc = DateTime.UtcNow
        };

        //[Mahjar partner-agreement] the same evidential acceptance record the web form writes: the document of
        //the declared type, its version, the moment, the IP, the method, a technical reference, and the text
        //itself frozen with all of those printed into its acceptance table
        if (_partnerAgreementSettings.Enabled && _vendorSettings.TermsOfServiceEnabled)
        {
            var agreementContext = PartnerAgreementContext.FromVendor(vendor, customer.Id) with
            {
                Services = (request.ActivateRental ? PartnerServices.Rental : PartnerServices.None) |
                           (request.ActivateSale ? PartnerServices.Sale : PartnerServices.None),
                AcceptedOnUtc = null,
                AcceptanceMethod = null,
                TechnicalRef = null
            };

            await _partnerAgreementService.RecordAcceptanceAsync(vendor, agreementContext,
                await _localizationService.GetResourceAsync("mahjar.api.vendorapply.acceptancemethod"),
                HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                (await _workContext.GetWorkingLanguageAsync()).Id);
        }

        await _vendorService.InsertVendorAsync(vendor);

        //search engine name (same as the vendor name)
        var seName = await _urlRecordService.ValidateSeNameAsync(vendor, vendor.Name, vendor.Name, true);
        await _urlRecordService.SaveSlugAsync(vendor, seName, 0);

        //associate to the current customer (the Vendors role follows when an admin activates the partner — [Mahjar vendor-role])
        customer.VendorId = vendor.Id;
        await _customerService.UpdateCustomerAsync(customer);

        //notify the store owner
        await _workflowMessageService.SendNewVendorAccountApplyStoreOwnerNotificationAsync(customer, vendor,
            _localizationSettings.DefaultAdminLanguageId);

        return Json(new MessageResponseDto { Success = true, Message = "submitted" });
    }

    #endregion
}
