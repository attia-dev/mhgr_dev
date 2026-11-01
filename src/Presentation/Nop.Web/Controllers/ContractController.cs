// [Mahjar contracts]
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Mahjar;
using Nop.Core.Domain.Orders;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Mahjar.Contracts;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Web.Factories;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Web.Controllers;

/// <summary>
/// Storefront (customer) contract-approval pages: the "My contracts" account page, the sign page
/// (canvas signature) and the submit/cancel posts. [Mahjar contracts]
/// </summary>
[AutoValidateAntiforgeryToken]
public partial class ContractController : BasePublicController
{
    #region Fields

    protected readonly IContractModelFactory _contractModelFactory;
    protected readonly IContractService _contractService;
    protected readonly ICustomerService _customerService;
    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationService _notificationService;
    protected readonly IShoppingCartService _shoppingCartService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public ContractController(IContractModelFactory contractModelFactory,
        IContractService contractService,
        ICustomerService customerService,
        ILocalizationService localizationService,
        INotificationService notificationService,
        IShoppingCartService shoppingCartService,
        IStoreContext storeContext,
        IWorkContext workContext)
    {
        _contractModelFactory = contractModelFactory;
        _contractService = contractService;
        _customerService = customerService;
        _localizationService = localizationService;
        _notificationService = notificationService;
        _shoppingCartService = shoppingCartService;
        _storeContext = storeContext;
        _workContext = workContext;
    }

    #endregion

    #region Methods

    public virtual async Task<IActionResult> Index()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (!await _customerService.IsRegisteredAsync(customer))
            return Challenge();

        var model = await _contractModelFactory.PrepareContractListModelAsync();
        return View(model);
    }

    [HttpPost]
    public virtual async Task<IActionResult> Submit()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (!await _customerService.IsRegisteredAsync(customer))
            return Challenge();

        var store = await _storeContext.GetCurrentStoreAsync();
        var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        if (!cart.Any())
            return RedirectToRoute("ShoppingCart");

        if (!await _contractService.IsContractRequiredAsync(cart))
            return RedirectToRoute("ShoppingCart");

        var existingRequest = await _contractService.GetActiveRequestAsync(customer.Id, store.Id);
        try
        {
            var contractRequest = await _contractService.SubmitRequestAsync(customer, cart, store);
            if (existingRequest != null && existingRequest.Id != contractRequest.Id)
                _notificationService.WarningNotification(await _localizationService.GetResourceAsync("mahjar.contracts.cartchanged"));
            else if (existingRequest == null)
                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("mahjar.contracts.submitted"));
            else
                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("mahjar.contracts.cart.pendingnote"));
        }
        catch (NopException exc)
        {
            //e.g. the submit flood throttle
            _notificationService.ErrorNotification(exc.Message);
        }

        return RedirectToRoute("CustomerContracts");
    }

    [HttpGet]
    public virtual async Task<IActionResult> Sign(Guid token)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (!await _customerService.IsRegisteredAsync(customer))
            return Challenge();

        var request = await _contractService.GetRequestByTokenAsync(token);
        if (request == null || request.CustomerId != customer.Id)
            return NotFound();

        if (request.Status != ContractRequestStatus.ContractSent)
        {
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("mahjar.contracts.sign.notsignable"));
            return RedirectToRoute("CustomerContracts");
        }

        var model = await _contractModelFactory.PrepareContractSignModelAsync(request);
        return View(model);
    }

    [HttpPost]
    public virtual async Task<IActionResult> SignPost(Guid token, string signatureDataUrl, string signedName, bool agree)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (!await _customerService.IsRegisteredAsync(customer))
            return Challenge();

        var request = await _contractService.GetRequestByTokenAsync(token);
        if (request == null || request.CustomerId != customer.Id)
            return NotFound();

        if (!agree)
        {
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("mahjar.contracts.sign.mustagree"));
            return RedirectToRoute("ContractSign", new { token });
        }

        try
        {
            await _contractService.SignContractAsync(request, signatureDataUrl, signedName,
                HttpContext.Connection.RemoteIpAddress?.ToString());
        }
        catch (NopException exc)
        {
            _notificationService.ErrorNotification(exc.Message);
            return RedirectToRoute("ContractSign", new { token });
        }

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("mahjar.contracts.sign.success"));
        return RedirectToRoute("CustomerContracts");
    }

    /// <summary>
    /// [Mahjar handover] The read-only contract page: the frozen document, the signature and the documented condition
    /// of each item before signing, at delivery and at a rental return. Only the owner, and only once a document exists.
    /// </summary>
    [HttpGet]
    public virtual async Task<IActionResult> Details(int id)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (!await _customerService.IsRegisteredAsync(customer))
            return Challenge();

        var request = await _contractService.GetRequestByIdAsync(id);
        if (request == null || request.CustomerId != customer.Id)
            return NotFound();

        if (string.IsNullOrEmpty(request.ContractBody))
        {
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("mahjar.contracts.view.notavailable"));
            return RedirectToRoute("CustomerContracts");
        }

        var model = await _contractModelFactory.PrepareContractViewModelAsync(request);
        return View(model);
    }

    [HttpPost]
    public virtual async Task<IActionResult> Cancel(int id)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (!await _customerService.IsRegisteredAsync(customer))
            return Challenge();

        var request = await _contractService.GetRequestByIdAsync(id);
        if (request == null || request.CustomerId != customer.Id)
            return NotFound();

        if (request.Status == ContractRequestStatus.Pending || request.Status == ContractRequestStatus.ContractSent)
        {
            await _contractService.CancelAsync(request);
            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("mahjar.contracts.cancelled"));
        }
        else
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("mahjar.api.contracts.cannotcancel"));

        return RedirectToRoute("CustomerContracts");
    }

    #endregion
}
