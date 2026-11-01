using System.Threading.Tasks;
using MHGR.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Vendors;
using Nop.Services.Authentication;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Vendors;

namespace Nop.Web.Controllers;

/// <summary>
/// Support API for the buyer app: send a "contact us" message to the store, or a "contact vendor" message to a
/// seller. Bearer-token authenticated so the sender can be prefilled. Returns shared MHGR.Api.Contracts DTOs. [Mahjar mobile-api]
/// </summary>
[IgnoreAntiforgeryToken]
public partial class SupportApiController : Controller
{
    #region Fields

    protected readonly IApiTokenService _apiTokenService;
    protected readonly ILocalizationService _localizationService; // [Mahjar i18n]
    protected readonly IVendorService _vendorService;
    protected readonly IWorkContext _workContext;
    protected readonly IWorkflowMessageService _workflowMessageService;
    protected readonly VendorSettings _vendorSettings;

    #endregion

    #region Ctor

    public SupportApiController(IApiTokenService apiTokenService,
        ILocalizationService localizationService,
        IVendorService vendorService,
        IWorkContext workContext,
        IWorkflowMessageService workflowMessageService,
        VendorSettings vendorSettings)
    {
        _apiTokenService = apiTokenService;
        _localizationService = localizationService;
        _vendorService = vendorService;
        _workContext = workContext;
        _workflowMessageService = workflowMessageService;
        _vendorSettings = vendorSettings;
    }

    #endregion

    #region Utilities

    protected virtual async Task<Customer> GetTokenCustomerAsync()
    {
        return await _apiTokenService.GetAuthenticatedCustomerAsync(Request.Headers["Authorization"].ToString());
    }

    // resolve sender name/email from the request, falling back to the authenticated customer
    protected static (string email, string name) ResolveSender(ContactApiRequest request, Customer customer)
    {
        var email = string.IsNullOrWhiteSpace(request?.Email) ? customer?.Email : request.Email.Trim();
        var name = string.IsNullOrWhiteSpace(request?.Name)
            ? $"{customer?.FirstName} {customer?.LastName}".Trim()
            : request.Name.Trim();
        return (email, name);
    }

    #endregion

    #region Methods

    // POST /api/contact-us   { name, email, subject, body }
    [HttpPost]
    [ProducesResponseType(typeof(MessageResponseDto), 200)]
    public virtual async Task<IActionResult> ContactUs([FromBody] ContactApiRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });
        if (request == null || string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.support.bodyrequired") });

        var (email, name) = ResolveSender(request, customer);
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.emailrequired") });

        var languageId = (await _workContext.GetWorkingLanguageAsync()).Id;
        await _workflowMessageService.SendContactUsMessageAsync(languageId, email, name, request.Subject, request.Body);

        return Json(new MessageResponseDto { Success = true, Message = await _localizationService.GetResourceAsync("mahjar.api.support.sent") });
    }

    // POST /api/vendors/{id}/contact   { name, email, subject, body }
    [HttpPost]
    [ProducesResponseType(typeof(MessageResponseDto), 200)]
    public virtual async Task<IActionResult> ContactVendor(int id, [FromBody] ContactApiRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });
        if (!_vendorSettings.AllowCustomersToContactVendors)
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.support.vendordisabled") });
        if (request == null || string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.support.bodyrequired") });

        var vendor = await _vendorService.GetVendorByIdAsync(id);
        if (vendor == null || vendor.Deleted || !vendor.Active)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.vendornotfound") });

        var (email, name) = ResolveSender(request, customer);
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.emailrequired") });

        var languageId = (await _workContext.GetWorkingLanguageAsync()).Id;
        await _workflowMessageService.SendContactVendorMessageAsync(vendor, languageId, email, name, request.Subject, request.Body);

        return Json(new MessageResponseDto { Success = true, Message = await _localizationService.GetResourceAsync("mahjar.api.support.senttovendor") });
    }

    #endregion
}
