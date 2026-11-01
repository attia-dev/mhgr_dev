using System;
using System.Threading.Tasks;
using MHGR.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Services.Authentication;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Messages;

namespace Nop.Web.Controllers;

/// <summary>
/// Mobile/headless authentication API: register, login, email verification, password recovery (forgot + reset),
/// bearer-token refresh, and server-side logout (token revocation via a per-customer epoch). Returns shared
/// MHGR.Api.Contracts DTOs. [Mahjar mobile-api]
/// </summary>
[IgnoreAntiforgeryToken]
public partial class AuthApiController : Controller
{
    #region Fields

    protected readonly CustomerSettings _customerSettings;
    protected readonly IApiTokenService _apiTokenService;
    protected readonly ICustomerRegistrationService _customerRegistrationService;
    protected readonly ICustomerService _customerService;
    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly ILocalizationService _localizationService; //[Mahjar i18n]
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;
    protected readonly IWorkflowMessageService _workflowMessageService;

    #endregion

    #region Ctor

    public AuthApiController(CustomerSettings customerSettings,
        IApiTokenService apiTokenService,
        ICustomerRegistrationService customerRegistrationService,
        ICustomerService customerService,
        IGenericAttributeService genericAttributeService,
        ILocalizationService localizationService,
        IStoreContext storeContext,
        IWorkContext workContext,
        IWorkflowMessageService workflowMessageService)
    {
        _customerSettings = customerSettings;
        _apiTokenService = apiTokenService;
        _customerRegistrationService = customerRegistrationService;
        _customerService = customerService;
        _genericAttributeService = genericAttributeService;
        _localizationService = localizationService;
        _storeContext = storeContext;
        _workContext = workContext;
        _workflowMessageService = workflowMessageService;
    }

    #endregion

    #region Utilities

    //issue a token stamped with the customer's current epoch (so logout/revocation works) [Mahjar mobile-api]
    protected virtual async Task<string> IssueTokenAsync(Customer customer)
    {
        var epoch = await _apiTokenService.GetCurrentTokenEpochAsync(customer);
        return _apiTokenService.CreateToken(customer.Id, customer.Email, epoch);
    }

    //build the standard auth payload (token + customer summary)
    protected virtual async Task<AuthResponseDto> BuildAuthAsync(Customer customer)
    {
        return new AuthResponseDto
        {
            Token = await IssueTokenAsync(customer),
            TokenType = "Bearer",
            ExpiresInDays = 7,
            Customer = new CustomerSummaryDto { Id = customer.Id, Email = customer.Email }
        };
    }

    #endregion

    #region Methods

    // POST /api/auth/register   { "email","password","firstName","lastName" }
    [HttpPost]
    [ProducesResponseType(typeof(RegisterResultDto), 200)]
    public virtual async Task<IActionResult> Register([FromBody] RegisterApiRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.auth.credentialsrequired") });

        if (await _customerService.GetCustomerByEmailAsync(request.Email) != null)
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.auth.emailexists") });

        var store = await _storeContext.GetCurrentStoreAsync();

        //honor the store's registration policy: only "Standard" activates immediately
        var isApproved = _customerSettings.UserRegistrationType == UserRegistrationType.Standard;

        //registration promotes a guest customer to a registered one
        var customer = await _customerService.InsertGuestCustomerAsync();
        var registrationRequest = new CustomerRegistrationRequest(customer, request.Email, request.Email,
            request.Password, PasswordFormat.Hashed, store.Id, isApproved);

        var result = await _customerRegistrationService.RegisterCustomerAsync(registrationRequest);
        if (!result.Success)
            return BadRequest(new { success = false, errors = result.Errors });

        //reload the customer and store the name (first/last name are entity properties in 4.80)
        customer = await _customerService.GetCustomerByIdAsync(customer.Id);
        if (!string.IsNullOrWhiteSpace(request.FirstName) || !string.IsNullOrWhiteSpace(request.LastName))
        {
            customer.FirstName = request.FirstName;
            customer.LastName = request.LastName;
            await _customerService.UpdateCustomerAsync(customer);
        }

        if (customer.Active)
            return Json(new RegisterResultDto { Status = "active", Auth = await BuildAuthAsync(customer) });

        //inactive: either email verification or admin approval is required
        if (_customerSettings.UserRegistrationType == UserRegistrationType.EmailValidation)
        {
            await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.AccountActivationTokenAttribute, Guid.NewGuid().ToString());
            await _workflowMessageService.SendCustomerEmailValidationMessageAsync(customer, (await _workContext.GetWorkingLanguageAsync()).Id);
            return Json(new RegisterResultDto
            {
                Status = "pendingActivation",
                Message = await _localizationService.GetResourceAsync("mahjar.api.auth.registered.checkemail")
            });
        }

        return Json(new RegisterResultDto { Status = "pendingApproval", Message = await _localizationService.GetResourceAsync("mahjar.api.auth.registered.pendingapproval") });
    }

    // POST /api/auth/verify-email   { "email","token" }
    [HttpPost]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    public virtual async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailApiRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.auth.activationrequest") });

        var customer = await _customerService.GetCustomerByEmailAsync(request.Email);
        if (customer == null)
            return BadRequest(new { success = false, error = await _localizationService.GetResourceAsync("mahjar.api.auth.invalidemailortoken") });

        var activationToken = await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.AccountActivationTokenAttribute);
        if (string.IsNullOrEmpty(activationToken))
            return BadRequest(new { success = false, error = await _localizationService.GetResourceAsync("mahjar.api.auth.alreadyactivated") });

        if (!activationToken.Equals(request.Token, StringComparison.InvariantCultureIgnoreCase))
            return BadRequest(new { success = false, error = await _localizationService.GetResourceAsync("mahjar.api.auth.invalidactivationtoken") });

        //activate
        customer.Active = true;
        await _customerService.UpdateCustomerAsync(customer);
        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.AccountActivationTokenAttribute, string.Empty);
        await _workflowMessageService.SendCustomerWelcomeMessageAsync(customer, (await _workContext.GetWorkingLanguageAsync()).Id);

        return Json(await BuildAuthAsync(customer));
    }

    // POST /api/auth/login   { "email","password" }
    [HttpPost]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    public virtual async Task<IActionResult> Login([FromBody] LoginApiRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.auth.credentialsrequired") });

        var result = await _customerRegistrationService.ValidateCustomerAsync(request.Email, request.Password);
        if (result != CustomerLoginResults.Successful)
        {
            var errorKey = result switch
            {
                CustomerLoginResults.WrongPassword => "account.login.wrongcredentials",
                CustomerLoginResults.CustomerNotExist => "account.login.wrongcredentials.customernotexist",
                CustomerLoginResults.NotActive => "account.login.wrongcredentials.notactive",
                CustomerLoginResults.Deleted => "account.login.wrongcredentials.deleted",
                CustomerLoginResults.NotRegistered => "account.login.wrongcredentials.notregistered",
                CustomerLoginResults.LockedOut => "account.login.wrongcredentials.lockedout",
                _ => "mahjar.api.auth.invalidcredentials"
            };
            return Unauthorized(new { error = await _localizationService.GetResourceAsync(errorKey), reason = result.ToString() });
        }

        var customer = await _customerService.GetCustomerByEmailAsync(request.Email);
        return Json(await BuildAuthAsync(customer));
    }

    // POST /api/auth/forgot-password   { "email" }
    [HttpPost]
    [ProducesResponseType(typeof(MessageResponseDto), 200)]
    public virtual async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordApiRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.emailrequired") });

        var customer = await _customerService.GetCustomerByEmailAsync(request.Email);
        if (customer != null && customer.Active && !customer.Deleted)
        {
            //save a recovery token + timestamp, then email the reset link (same flow as the website)
            var recoveryToken = Guid.NewGuid();
            await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.PasswordRecoveryTokenAttribute, recoveryToken.ToString());
            DateTime? generatedOn = DateTime.UtcNow;
            await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.PasswordRecoveryTokenDateGeneratedAttribute, generatedOn);
            await _workflowMessageService.SendCustomerPasswordRecoveryMessageAsync(customer, (await _workContext.GetWorkingLanguageAsync()).Id);
        }

        //always respond generically to avoid account enumeration
        return Json(new MessageResponseDto { Success = true, Message = await _localizationService.GetResourceAsync("mahjar.api.auth.resetsent") });
    }

    // POST /api/auth/reset-password   { "email","token","newPassword" }
    [HttpPost]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    public virtual async Task<IActionResult> ResetPassword([FromBody] ResetPasswordApiRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.auth.resetrequest") });

        var customer = await _customerService.GetCustomerByEmailAsync(request.Email);
        if (customer == null)
            return BadRequest(new { success = false, error = await _localizationService.GetResourceAsync("mahjar.api.auth.invalidemailortoken") });

        if (string.IsNullOrEmpty(await _genericAttributeService.GetAttributeAsync<string>(customer, NopCustomerDefaults.PasswordRecoveryTokenAttribute)))
            return BadRequest(new { success = false, error = await _localizationService.GetResourceAsync("mahjar.api.auth.noresetrequest") });

        if (!await _customerService.IsPasswordRecoveryTokenValidAsync(customer, request.Token))
            return BadRequest(new { success = false, error = await _localizationService.GetResourceAsync("mahjar.api.auth.invalidorexpiredtoken") });

        if (await _customerService.IsPasswordRecoveryLinkExpiredAsync(customer))
            return BadRequest(new { success = false, error = await _localizationService.GetResourceAsync("mahjar.api.auth.resetexpired") });

        var response = await _customerRegistrationService.ChangePasswordAsync(
            new ChangePasswordRequest(customer.Email, false, _customerSettings.DefaultPasswordFormat, request.NewPassword));
        if (!response.Success)
            return BadRequest(new { success = false, errors = response.Errors });

        //invalidate the recovery token
        await _genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.PasswordRecoveryTokenAttribute, string.Empty);

        //sign the user straight into the app
        return Json(await BuildAuthAsync(customer));
    }

    // POST /api/auth/refresh   (send the current, still-valid bearer token; get a fresh one)
    [HttpPost]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    public virtual async Task<IActionResult> Refresh()
    {
        var customer = await _apiTokenService.GetAuthenticatedCustomerAsync(Request.Headers["Authorization"].ToString());
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        return Json(await BuildAuthAsync(customer));
    }

    // POST /api/auth/logout   (revokes every token issued to this account)
    [HttpPost]
    [ProducesResponseType(typeof(MessageResponseDto), 200)]
    public virtual async Task<IActionResult> Logout()
    {
        var customer = await _apiTokenService.GetAuthenticatedCustomerAsync(Request.Headers["Authorization"].ToString());
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        await _apiTokenService.RevokeTokensAsync(customer);
        return Json(new MessageResponseDto { Success = true, Message = await _localizationService.GetResourceAsync("mahjar.api.auth.loggedout") });
    }

    #endregion
}
