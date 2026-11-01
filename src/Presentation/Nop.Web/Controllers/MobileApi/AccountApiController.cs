using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MHGR.Api.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Services.Authentication;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Mahjar.HomeSlides;
using Nop.Services.Media;
using Nop.Services.Orders;
using ILogger = Nop.Services.Logging.ILogger;

namespace Nop.Web.Controllers;

/// <summary>
/// Customer account API (bearer-token authenticated): read/update profile, change password, and manage the saved
/// address book so the checkout can reuse addresses. Returns shared MHGR.Api.Contracts DTOs. [Mahjar mobile-api]
/// [Mahjar app-batch3] Also the customer's profile photo (the website's avatar): upload / remove, its url in the profile.
/// </summary>
[IgnoreAntiforgeryToken]
public partial class AccountApiController : Controller
{
    #region Fields

    protected readonly IAddressService _addressService;
    protected readonly IApiTokenService _apiTokenService;
    protected readonly ICurrencyService _currencyService;
    protected readonly ICustomerRegistrationService _customerRegistrationService;
    protected readonly ICustomerService _customerService;
    protected readonly IGenericAttributeService _genericAttributeService; //[Mahjar app-batch3]
    protected readonly IGiftCardService _giftCardService;
    protected readonly IHomeSlideService _homeSlideService; //[Mahjar app-batch3] its "picture in use" check guards avatar deletes
    protected readonly ILocalizationService _localizationService; //[Mahjar i18n]
    protected readonly ILogger _logger; //[Mahjar app-batch3]
    protected readonly IPictureService _pictureService; //[Mahjar app-batch3]
    protected readonly IPriceFormatter _priceFormatter;
    protected readonly IRewardPointService _rewardPointService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;
    protected readonly CustomerSettings _customerSettings; //[Mahjar app-batch3]
    protected readonly MediaSettings _mediaSettings; //[Mahjar app-batch3]
    protected readonly RewardPointsSettings _rewardPointsSettings;

    #endregion

    #region Ctor

    public AccountApiController(IAddressService addressService,
        IApiTokenService apiTokenService,
        ICurrencyService currencyService,
        ICustomerRegistrationService customerRegistrationService,
        ICustomerService customerService,
        IGenericAttributeService genericAttributeService,
        IGiftCardService giftCardService,
        IHomeSlideService homeSlideService,
        ILocalizationService localizationService,
        ILogger logger,
        IPictureService pictureService,
        IPriceFormatter priceFormatter,
        IRewardPointService rewardPointService,
        IStoreContext storeContext,
        IWorkContext workContext,
        CustomerSettings customerSettings,
        MediaSettings mediaSettings,
        RewardPointsSettings rewardPointsSettings)
    {
        _addressService = addressService;
        _apiTokenService = apiTokenService;
        _currencyService = currencyService;
        _customerRegistrationService = customerRegistrationService;
        _customerService = customerService;
        _genericAttributeService = genericAttributeService;
        _giftCardService = giftCardService;
        _homeSlideService = homeSlideService;
        _localizationService = localizationService;
        _logger = logger;
        _pictureService = pictureService;
        _priceFormatter = priceFormatter;
        _rewardPointService = rewardPointService;
        _storeContext = storeContext;
        _workContext = workContext;
        _customerSettings = customerSettings;
        _mediaSettings = mediaSettings;
        _rewardPointsSettings = rewardPointsSettings;
    }

    #endregion

    #region Utilities

    protected virtual async Task<Customer> GetTokenCustomerAsync()
    {
        return await _apiTokenService.GetAuthenticatedCustomerAsync(Request.Headers["Authorization"].ToString());
    }

    protected static AddressDto MapAddress(Address a)
    {
        return new AddressDto
        {
            Id = a.Id,
            FirstName = a.FirstName,
            LastName = a.LastName,
            Email = a.Email,
            PhoneNumber = a.PhoneNumber,
            Company = a.Company,
            CountryId = a.CountryId,
            StateProvinceId = a.StateProvinceId,
            City = a.City,
            Address1 = a.Address1,
            Address2 = a.Address2,
            ZipPostalCode = a.ZipPostalCode
        };
    }

    protected static void ApplyAddress(Address a, AddressApiRequest r)
    {
        a.FirstName = r.FirstName;
        a.LastName = r.LastName;
        a.Email = r.Email;
        a.PhoneNumber = r.PhoneNumber;
        a.Company = r.Company;
        a.CountryId = r.CountryId > 0 ? r.CountryId : null;
        a.StateProvinceId = r.StateProvinceId > 0 ? r.StateProvinceId : null;
        a.City = r.City;
        a.Address1 = r.Address1;
        a.Address2 = r.Address2;
        a.ZipPostalCode = r.ZipPostalCode;
    }

    protected static CustomerProfileDto MapProfile(Customer c)
    {
        return new CustomerProfileDto
        {
            Id = c.Id,
            Email = c.Email,
            FirstName = c.FirstName,
            LastName = c.LastName,
            Phone = c.Phone
        };
    }

    /// <summary>
    /// The profile + the profile photo: its ABSOLUTE 320 px url (off the request host; null when there is none) and
    /// whether the store lets customers have one at all (CustomerSettings.AllowCustomersToUploadAvatars, the website's
    /// switch too; when it is off the app shows the monogram only). [Mahjar app-batch3]
    /// </summary>
    protected virtual async Task<CustomerProfileDto> PrepareProfileAsync(Customer customer)
    {
        var profile = MapProfile(customer);
        profile.CanChangeAvatar = _customerSettings.AllowCustomersToUploadAvatars;
        if (profile.CanChangeAvatar)
            profile.AvatarUrl = await CustomerAvatarHelper.GetUrlAsync(customer, CustomerAvatarHelper.AppAvatarSize,
                _genericAttributeService, _pictureService);

        return profile;
    }

    #endregion

    #region Profile

    // GET /api/customer/profile
    [HttpGet]
    [ProducesResponseType(typeof(CustomerProfileDto), 200)]
    public virtual async Task<IActionResult> Profile()
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        return Json(await PrepareProfileAsync(customer)); //[Mahjar app-batch3] + the profile photo
    }

    // PUT /api/customer/profile   { firstName, lastName, phone }
    [HttpPut]
    [ProducesResponseType(typeof(CustomerProfileDto), 200)]
    public virtual async Task<IActionResult> UpdateProfile([FromBody] ProfileApiRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });
        if (request == null)
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidrequest") });

        customer.FirstName = request.FirstName;
        customer.LastName = request.LastName;
        customer.Phone = request.Phone;
        await _customerService.UpdateCustomerAsync(customer);

        return Json(await PrepareProfileAsync(customer)); //[Mahjar app-batch3] + the profile photo
    }

    // POST /api/customer/change-password   { currentPassword, newPassword }
    [HttpPost]
    [ProducesResponseType(typeof(MessageResponseDto), 200)]
    public virtual async Task<IActionResult> ChangePassword([FromBody] ChangePasswordApiRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });
        if (request == null || string.IsNullOrEmpty(request.NewPassword))
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.account.newpasswordrequired") });

        var result = await _customerRegistrationService.ChangePasswordAsync(
            new ChangePasswordRequest(customer.Email, true, PasswordFormat.Hashed, request.NewPassword, request.CurrentPassword));

        if (!result.Success)
            return BadRequest(new { success = false, errors = result.Errors });

        return Json(new MessageResponseDto { Success = true, Message = await _localizationService.GetResourceAsync("mahjar.api.account.passwordchanged") });
    }

    #endregion

    #region Profile photo [Mahjar app-batch3]

    // POST /api/customer/avatar   multipart/form-data, field "file": a JPEG, PNG or WebP photo up to 5 MB.
    // Checked by its declared type AND its bytes, turned upright, cropped square and re-encoded (CustomerAvatarHelper),
    // stored as the customer's avatar (the website's My account / Avatar), the previous one deleted when it was their own
    // (nothing else uses it). Returns the profile.
    [HttpPost]
    [RequestSizeLimit(CustomerAvatarHelper.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = CustomerAvatarHelper.MaxRequestBytes)]
    [ProducesResponseType(typeof(CustomerProfileDto), 200)]
    public virtual async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });
        if (!_customerSettings.AllowCustomersToUploadAvatars)
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.account.avatardisabled") });
        //a body over the request cap never binds a file: say why
        if ((file == null || file.Length == 0) && Request.ContentLength > CustomerAvatarHelper.MaxUploadBytes)
            return BadRequest(new { error = await GetAvatarTooLargeMessageAsync() });
        if (file == null || file.Length == 0)
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.account.avatarnofile") });
        if (file.Length > CustomerAvatarHelper.MaxUploadBytes)
            return BadRequest(new { error = await GetAvatarTooLargeMessageAsync() });
        if (!CustomerAvatarHelper.IsAllowedContentType(file.ContentType, allowGif: false))
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.account.avatarinvalid") });

        byte[] bytes;
        await using (var stream = file.OpenReadStream())
        using (var buffer = new MemoryStream((int)file.Length))
        {
            await stream.CopyToAsync(buffer);
            bytes = buffer.ToArray();
        }

        switch (CustomerAvatarHelper.Normalize(bytes, allowGif: false, _mediaSettings, out var jpeg))
        {
            case CustomerAvatarHelper.AvatarImageError.TooLarge:
                return BadRequest(new { error = await GetAvatarTooLargeMessageAsync() });
            case CustomerAvatarHelper.AvatarImageError.NotAnImage:
                return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.account.avatarinvalid") });
        }

        try
        {
            await CustomerAvatarHelper.ReplaceAsync(customer, jpeg, _genericAttributeService, _pictureService, _homeSlideService, _logger);
        }
        catch (Exception exc)
        {
            await _logger.ErrorAsync("Mahjar: a customer avatar could not be stored", exc, customer);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = await _localizationService.GetResourceAsync("mahjar.api.account.avatarfailed") });
        }

        return Json(await PrepareProfileAsync(customer));
    }

    // DELETE /api/customer/avatar   Returns the profile (AvatarUrl null). Always allowed, even with photos switched off.
    [HttpDelete]
    [ProducesResponseType(typeof(CustomerProfileDto), 200)]
    public virtual async Task<IActionResult> DeleteAvatar()
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        await CustomerAvatarHelper.RemoveAsync(customer, _genericAttributeService, _homeSlideService, _logger);

        return Json(await PrepareProfileAsync(customer));
    }

    /// <summary>"This photo is too large. Choose one under 5 MB." in the request language.</summary>
    protected virtual async Task<string> GetAvatarTooLargeMessageAsync()
    {
        return string.Format(await _localizationService.GetResourceAsync("mahjar.api.account.avatartoolarge"),
            CustomerAvatarHelper.MaxUploadBytes / (1024 * 1024));
    }

    #endregion

    #region Address book

    // GET /api/customer/addresses
    [HttpGet]
    [ProducesResponseType(typeof(ListResultDto<AddressDto>), 200)]
    public virtual async Task<IActionResult> Addresses()
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var addresses = await _customerService.GetAddressesByCustomerIdAsync(customer.Id);
        return Json(new ListResultDto<AddressDto> { Items = addresses.Select(MapAddress).ToList() });
    }

    // POST /api/customer/addresses
    [HttpPost]
    [ProducesResponseType(typeof(AddressDto), 200)]
    public virtual async Task<IActionResult> AddAddress([FromBody] AddressApiRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });
        if (request == null)
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidrequest") });

        var address = new Address { CreatedOnUtc = DateTime.UtcNow };
        ApplyAddress(address, request);
        if (string.IsNullOrEmpty(address.Email))
            address.Email = customer.Email;

        await _addressService.InsertAddressAsync(address);
        await _customerService.InsertCustomerAddressAsync(customer, address);

        return Json(MapAddress(address));
    }

    // PUT /api/customer/addresses/{id}
    [HttpPut]
    [ProducesResponseType(typeof(AddressDto), 200)]
    public virtual async Task<IActionResult> UpdateAddress(int id, [FromBody] AddressApiRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });
        if (request == null)
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidrequest") });

        //ownership: only addresses already linked to this customer
        var address = (await _customerService.GetAddressesByCustomerIdAsync(customer.Id)).FirstOrDefault(a => a.Id == id);
        if (address == null)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.account.addressnotfound") });

        ApplyAddress(address, request);
        await _addressService.UpdateAddressAsync(address);

        return Json(MapAddress(address));
    }

    // DELETE /api/customer/addresses/{id}
    [HttpDelete]
    [ProducesResponseType(typeof(MessageResponseDto), 200)]
    public virtual async Task<IActionResult> DeleteAddress(int id)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var address = (await _customerService.GetAddressesByCustomerIdAsync(customer.Id)).FirstOrDefault(a => a.Id == id);
        if (address == null)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.account.addressnotfound") });

        await _customerService.RemoveCustomerAddressAsync(customer, address);
        await _addressService.DeleteAddressAsync(address);

        return Json(new MessageResponseDto { Success = true, Message = await _localizationService.GetResourceAsync("mahjar.api.account.addressdeleted") });
    }

    #endregion

    #region Loyalty & gift cards

    // GET /api/customer/reward-points?page=&pageSize=
    [HttpGet]
    [ProducesResponseType(typeof(RewardPointsResultDto), 200)]
    public virtual async Task<IActionResult> RewardPoints(int page = 0, int pageSize = 20)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var currency = (await _workContext.GetWorkingCurrencyAsync()).CurrencyCode;
        if (!_rewardPointsSettings.Enabled)
            return Json(new RewardPointsResultDto { Balance = 0, Currency = currency });

        var store = await _storeContext.GetCurrentStoreAsync();
        var balance = await _rewardPointService.GetRewardPointsBalanceAsync(customer.Id, store.Id);
        var history = await _rewardPointService.GetRewardPointsHistoryAsync(customer.Id, store.Id,
            pageIndex: page, pageSize: Math.Clamp(pageSize, 1, 100));

        return Json(new RewardPointsResultDto
        {
            Balance = balance,
            Currency = currency,
            Page = history.PageIndex,
            PageSize = history.PageSize,
            TotalItems = history.TotalCount,
            TotalPages = history.TotalPages,
            Items = history.Select(h => new RewardPointDto
            {
                Points = h.Points,
                PointsBalance = h.PointsBalance,
                Message = h.Message,
                CreatedOn = h.CreatedOnUtc.ToString("yyyy-MM-dd")
            }).ToList()
        });
    }

    // GET /api/gift-cards/balance?code=
    [HttpGet]
    [ProducesResponseType(typeof(GiftCardBalanceDto), 200)]
    public virtual async Task<IActionResult> GiftCardBalance(string code)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.coderequired") });

        var currency = await _workContext.GetWorkingCurrencyAsync();
        var card = (await _giftCardService.GetAllGiftCardsAsync(isGiftCardActivated: true, giftCardCouponCode: code.Trim())).FirstOrDefault();
        if (card == null || !await _giftCardService.IsGiftCardValidAsync(card))
            return Json(new GiftCardBalanceDto { Valid = false, Currency = currency.CurrencyCode });

        var remaining = await _giftCardService.GetGiftCardRemainingAmountAsync(card);
        var converted = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(remaining, currency);
        return Json(new GiftCardBalanceDto
        {
            Valid = true,
            RemainingAmount = converted,
            RemainingAmountFormatted = await _priceFormatter.FormatPriceAsync(converted),
            Currency = currency.CurrencyCode
        });
    }

    #endregion
}
