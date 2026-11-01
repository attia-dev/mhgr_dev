using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MHGR.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Mahjar;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Services.Authentication;
using Nop.Services.Catalog;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Mahjar.Contracts;
using Nop.Services.Mahjar.Handover;
using Nop.Services.Mahjar.ItemStatus;
using Nop.Services.Media;
using Nop.Services.Orders;

namespace Nop.Web.Controllers;

/// <summary>
/// Mobile/headless contract API (bearer-token authenticated) — the app side of the Mahjar contract-approval stage:
/// the buyer submits a contract request for the current cart, signs the sent contract and tracks its status. The
/// authoritative payment gate lives in OrderProcessingService, so this API can never bypass it. [Mahjar contracts]
/// </summary>
[IgnoreAntiforgeryToken]
public partial class ContractApiController : Controller
{
    #region Fields

    protected readonly IApiTokenService _apiTokenService;
    protected readonly IConditionCheckService _conditionCheckService; //[Mahjar handover]
    protected readonly IContractService _contractService;
    protected readonly IDateTimeHelper _dateTimeHelper; //[Mahjar handover]
    protected readonly ILanguageService _languageService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IOrderService _orderService; //[Mahjar handover]
    protected readonly IPictureService _pictureService; //[Mahjar handover]
    protected readonly IProductService _productService; //[Mahjar handover]
    protected readonly IShoppingCartService _shoppingCartService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWebHelper _webHelper; //[Mahjar handover]
    protected readonly MediaSettings _mediaSettings; //[Mahjar handover]

    #endregion

    #region Ctor

    public ContractApiController(IApiTokenService apiTokenService,
        IConditionCheckService conditionCheckService,
        IContractService contractService,
        IDateTimeHelper dateTimeHelper,
        ILanguageService languageService,
        ILocalizationService localizationService,
        IOrderService orderService,
        IPictureService pictureService,
        IProductService productService,
        IShoppingCartService shoppingCartService,
        IStoreContext storeContext,
        IWebHelper webHelper,
        MediaSettings mediaSettings)
    {
        _apiTokenService = apiTokenService;
        _conditionCheckService = conditionCheckService;
        _contractService = contractService;
        _dateTimeHelper = dateTimeHelper;
        _languageService = languageService;
        _localizationService = localizationService;
        _orderService = orderService;
        _pictureService = pictureService;
        _productService = productService;
        _shoppingCartService = shoppingCartService;
        _storeContext = storeContext;
        _webHelper = webHelper;
        _mediaSettings = mediaSettings;
    }

    #endregion

    #region Utilities

    protected virtual async Task<Customer> GetTokenCustomerAsync()
    {
        return await _apiTokenService.GetAuthenticatedCustomerAsync(Request.Headers["Authorization"].ToString());
    }

    /// <summary>
    /// [Mahjar handover] The condition checks the customer may see (before signing once sent, delivery, return —
    /// confirmed only). Media URLs are made ABSOLUTE off the request host (never Store.Url), so the emulator and the
    /// LAN hosts resolve them too; an external video URL is re-validated on the way out.
    /// </summary>
    protected virtual async Task<List<ConditionCheckDto>> BuildConditionChecksAsync(ContractRequest request)
    {
        var result = new List<ConditionCheckDto>();
        var storeLocation = _webHelper.GetStoreLocation().TrimEnd('/');

        foreach (var check in await _conditionCheckService.GetCustomerVisibleChecksAsync(request))
        {
            var typeName = check.CheckType.ToString();
            var product = await _productService.GetProductByIdAsync(check.ProductId);
            var isReturn = check.CheckType == ConditionCheckType.Return;

            var dto = new ConditionCheckDto
            {
                Id = check.Id,
                Type = char.ToLowerInvariant(typeName[0]) + typeName[1..],
                TypeLabel = await _localizationService.GetResourceAsync($"mahjar.contracts.condition.type.{typeName.ToLowerInvariant()}"),
                ProductId = check.ProductId,
                ProductName = product == null ? null : await _localizationService.GetLocalizedAsync(product, p => p.Name),
                //the customer's calendar day, not the UTC one (a late-evening handover in KSA is already tomorrow in UTC terms)
                RecordedOn = (await _dateTimeHelper.ConvertToUserTimeAsync(check.ConfirmedOnUtc ?? check.UpdatedOnUtc, DateTimeKind.Utc))
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                GradeText = check.ConditionGrade == ItemConditionGrade.NotSpecified
                    ? null
                    : await _localizationService.GetLocalizedEnumAsync(check.ConditionGrade),
                Notes = string.IsNullOrWhiteSpace(check.ConditionNotes) ? null : check.ConditionNotes,
                HasDamage = check.HasDamage && !string.IsNullOrWhiteSpace(check.DamageDescription),
                DamageSeverityText = check.HasDamage && check.DamageSeverityId > 0
                    ? await _localizationService.GetLocalizedEnumAsync(check.DamageSeverity)
                    : null,
                DamageDescription = check.HasDamage ? check.DamageDescription : null,
                DamageDeduction = isReturn ? check.DamageDeduction : null,
                DepositRefund = isReturn ? Math.Max(check.DepositAmount - check.DamageDeduction, decimal.Zero) : null
            };

            if (!string.IsNullOrEmpty(check.VideoFile))
            {
                dto.VideoUrl = storeLocation + check.VideoFile;
                dto.VideoIsFile = true;
            }
            else if (ItemStatusVideoUrlHelper.IsAllowed(check.VideoUrl))
            {
                dto.VideoUrl = check.VideoUrl.Trim();
                dto.VideoIsFile = false;
            }

            foreach (var picture in await _conditionCheckService.GetPicturesAsync(check.Id))
            {
                dto.Photos.Add(new ItemStatusPhotoDto
                {
                    Url = await _pictureService.GetPictureUrlAsync(picture.PictureId),
                    //thumbnails are painted at ~96dp on high-density phones — the 100px gallery thumb would blur them
                    ThumbUrl = await _pictureService.GetPictureUrlAsync(picture.PictureId, _mediaSettings.ProductDetailsPictureSize),
                    Caption = string.IsNullOrWhiteSpace(picture.Caption) ? null : picture.Caption
                });
            }

            result.Add(dto);
        }

        return result;
    }

    protected virtual async Task<ContractRequestDto> BuildContractDtoAsync(ContractRequest request)
    {
        var snapshot = await _contractService.DeserializeSnapshotAsync(request);
        var language = await _languageService.GetLanguageByIdAsync(request.LanguageId);
        var isArabic = language?.UniqueSeoCode?.Equals("ar", StringComparison.InvariantCultureIgnoreCase) == true;

        var statusName = request.Status.ToString();
        var dto = new ContractRequestDto
        {
            Id = request.Id,
            Number = $"MHGR-C-{request.Id:D5}",
            Status = char.ToLowerInvariant(statusName[0]) + statusName.Substring(1),
            StatusLabel = await _localizationService.GetResourceAsync($"mahjar.contracts.status.{statusName.ToLowerInvariant()}"),
            CreatedOnUtc = request.CreatedOnUtc,
            SentOnUtc = request.SentOnUtc,
            SignedOnUtc = request.SignedOnUtc,
            SignedName = request.SignedOnUtc.HasValue ? request.SignedName : null, //[Mahjar handover]
            //the rendered contract document is only exposed once it has been sent for signing
            ContractBodyHtml = request.StatusId >= (int)ContractRequestStatus.ContractSent ? request.ContractBody : null,
            RejectionReason = request.RejectionReason,
            CanSign = request.Status == ContractRequestStatus.ContractSent,
            CanCancel = request.Status == ContractRequestStatus.Pending || request.Status == ContractRequestStatus.ContractSent,
            CanCheckout = request.Status == ContractRequestStatus.Approved,
            SubTotal = snapshot?.SubTotal ?? decimal.Zero,
            DepositTotal = snapshot?.DepositTotal ?? decimal.Zero,
            GrandTotal = snapshot?.GrandTotal ?? decimal.Zero,
            Currency = snapshot?.CurrencyCode,
            //[Mahjar handover]
            OrderId = request.OrderId,
            OrderNumber = request.OrderId > 0 ? (await _orderService.GetOrderByIdAsync(request.OrderId))?.CustomOrderNumber : null,
            ConditionChecks = await BuildConditionChecksAsync(request)
        };

        if (snapshot?.Lines != null)
        {
            foreach (var line in snapshot.Lines)
            {
                dto.Lines.Add(new ContractLineDto
                {
                    ProductId = line.ProductId,
                    Name = isArabic && !string.IsNullOrEmpty(line.ProductNameAr) ? line.ProductNameAr : line.ProductName,
                    Sku = line.Sku,
                    IsRental = line.IsRental,
                    Quantity = line.Quantity,
                    RentalStart = line.RentalStartUtc,
                    RentalEnd = line.RentalEndUtc,
                    RentalDays = line.RentalDays,
                    UnitPrice = line.UnitPrice,
                    Deposit = line.Deposit,
                    LineTotal = line.LineTotal
                });
            }
        }

        return dto;
    }

    #endregion

    #region Methods

    // GET /api/contract   -> the active contract request of the token customer (newest non-terminal one)
    [HttpGet]
    [ProducesResponseType(typeof(ContractRequestDto), 200)]
    public virtual async Task<IActionResult> Current()
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var request = await _contractService.GetActiveRequestAsync(customer.Id, store.Id);
        if (request == null)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.contracts.notfound"), code = "notFound" }); //[Mahjar contracts] machine-readable code for the app

        return Json(await BuildContractDtoAsync(request));
    }

    // POST /api/contract/request   -> submit a contract request for the current cart
    [HttpPost]
    [ProducesResponseType(typeof(ContractRequestDto), 200)]
    public virtual async Task<IActionResult> Submit()
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        if (!cart.Any())
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.contracts.cartempty"), code = "cartEmpty" }); //[Mahjar contracts] machine-readable code for the app

        if (!await _contractService.IsContractRequiredAsync(cart))
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.contracts.notrequired"), code = "notRequired" }); //[Mahjar contracts] machine-readable code for the app

        try
        {
            var request = await _contractService.SubmitRequestAsync(customer, cart, store);
            return Json(await BuildContractDtoAsync(request));
        }
        catch (NopException exc)
        {
            //e.g. the submit flood throttle
            return BadRequest(new { error = exc.Message, code = "rejected" }); //[Mahjar contracts] machine-readable code for the app
        }
    }

    // POST /api/contract/sign   { "signatureDataUrl": "data:image/png;base64,...", "signedName": "..." }
    [HttpPost]
    [ProducesResponseType(typeof(ContractRequestDto), 200)]
    public virtual async Task<IActionResult> Sign([FromBody] ContractSignRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var contractRequest = await _contractService.GetActiveRequestAsync(customer.Id, store.Id);
        if (contractRequest == null)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.contracts.notfound"), code = "notFound" }); //[Mahjar contracts] machine-readable code for the app

        if (contractRequest.Status != ContractRequestStatus.ContractSent)
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.contracts.notsignable"), code = "notSignable" }); //[Mahjar contracts] machine-readable code for the app

        try
        {
            await _contractService.SignContractAsync(contractRequest, request?.SignatureDataUrl, request?.SignedName,
                HttpContext.Connection.RemoteIpAddress?.ToString());
        }
        catch (NopException)
        {
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.contracts.invalidsignature"), code = "invalidSignature" }); //[Mahjar contracts] machine-readable code for the app
        }

        return Json(await BuildContractDtoAsync(contractRequest));
    }

    // POST /api/contract/cancel   -> cancel the active request (only before it is signed)
    [HttpPost]
    public virtual async Task<IActionResult> Cancel()
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var request = await _contractService.GetActiveRequestAsync(customer.Id, store.Id);
        if (request == null || (request.Status != ContractRequestStatus.Pending && request.Status != ContractRequestStatus.ContractSent))
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.contracts.cannotcancel"), code = "cannotCancel" }); //[Mahjar contracts] machine-readable code for the app

        await _contractService.CancelAsync(request);
        return Json(new { success = true, message = await _localizationService.GetResourceAsync("mahjar.api.contracts.cancelled") });
    }

    // GET /api/contract/{id}   -> one of the token customer's own contracts (any status), with its condition checks
    // [Mahjar handover] the app's condition-report screen needs finished (Completed) contracts too, which
    // GET api/contract (the newest ACTIVE one) never returns
    [HttpGet]
    [ProducesResponseType(typeof(ContractRequestDto), 200)]
    public virtual async Task<IActionResult> ById(int id)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var request = await _contractService.GetRequestByIdAsync(id);
        if (request == null || request.CustomerId != customer.Id)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.contracts.notfound"), code = "notFound" });

        return Json(await BuildContractDtoAsync(request));
    }

    // GET /api/contract/history?page=&pageSize=
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ContractRequestDto>), 200)]
    public virtual async Task<IActionResult> History(int page = 1, int pageSize = 20)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var requests = await _contractService.SearchRequestsAsync(customerId: customer.Id, storeId: store.Id,
            pageIndex: Math.Max(page, 1) - 1, pageSize: Math.Clamp(pageSize, 1, 100));

        var items = new List<ContractRequestDto>();
        foreach (var request in requests)
            items.Add(await BuildContractDtoAsync(request));

        return Json(new PagedResultDto<ContractRequestDto>
        {
            Page = requests.PageIndex + 1,
            PageSize = requests.PageSize,
            TotalItems = requests.TotalCount,
            TotalPages = requests.TotalPages,
            Items = items
        });
    }

    #endregion
}
