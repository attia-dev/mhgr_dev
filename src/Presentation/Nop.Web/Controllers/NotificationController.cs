// [Mahjar notifications]
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Mahjar;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Mahjar.Notifications;
using Nop.Services.Messages;
using Nop.Web.Factories;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Models.Notification;

namespace Nop.Web.Controllers;

/// <summary>
/// Storefront (buyer) in-app notifications: the My-Account feed page + settings, plus the AJAX endpoints the
/// header bell uses (flyout feed, mark read / mark all). [Mahjar notifications]
/// </summary>
[AutoValidateAntiforgeryToken]
public partial class NotificationController : BasePublicController
{
    #region Fields

    protected readonly ICustomerService _customerService;
    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationFeedService _feedService;
    protected readonly INotificationModelFactory _notificationModelFactory;
    protected readonly INotificationService _notificationService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public NotificationController(ICustomerService customerService,
        ILocalizationService localizationService,
        INotificationFeedService feedService,
        INotificationModelFactory notificationModelFactory,
        INotificationService notificationService,
        IStoreContext storeContext,
        IWorkContext workContext)
    {
        _customerService = customerService;
        _localizationService = localizationService;
        _feedService = feedService;
        _notificationModelFactory = notificationModelFactory;
        _notificationService = notificationService;
        _storeContext = storeContext;
        _workContext = workContext;
    }

    #endregion

    #region Utilities

    protected virtual async Task<int> BuyerUnreadCountAsync()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        return await _feedService.GetUnreadCountAsync(customer.Id, NotificationRecipientType.Buyer, 0, store.Id);
    }

    #endregion

    #region Methods

    public virtual async Task<IActionResult> Index(int pageNumber = 0)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (!await _customerService.IsRegisteredAsync(customer))
            return Challenge();

        var model = await _notificationModelFactory.PrepareListModelAsync(pageNumber);
        return View(model);
    }

    [HttpGet]
    public virtual async Task<IActionResult> Feed(int page = 0)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (!await _customerService.IsRegisteredAsync(customer))
            return Json(new { success = false });

        var model = await _notificationModelFactory.PrepareListModelAsync(page);
        return Json(new { success = true, model });
    }

    [HttpPost]
    public virtual async Task<IActionResult> MarkRead(int id)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (!await _customerService.IsRegisteredAsync(customer))
            return Json(new { success = false });

        await _feedService.MarkAsReadAsync(id, customer.Id);
        return Json(new { success = true, unread = await BuyerUnreadCountAsync() });
    }

    [HttpPost]
    public virtual async Task<IActionResult> MarkAllRead()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (!await _customerService.IsRegisteredAsync(customer))
            return Json(new { success = false });

        var store = await _storeContext.GetCurrentStoreAsync();
        await _feedService.MarkAllAsReadAsync(customer.Id, NotificationRecipientType.Buyer, 0, store.Id);
        return Json(new { success = true, unread = 0 });
    }

    public virtual async Task<IActionResult> Settings()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (!await _customerService.IsRegisteredAsync(customer))
            return Challenge();

        var model = await _notificationModelFactory.PreparePreferenceModelAsync();
        return View(model);
    }

    [HttpPost]
    public virtual async Task<IActionResult> SaveSettings(NotificationPreferenceModel model)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (!await _customerService.IsRegisteredAsync(customer))
            return Challenge();

        await _notificationModelFactory.SavePreferencesAsync(model);
        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("mahjar.notifications.settings.saved"));
        return RedirectToRoute("CustomerNotificationSettings");
    }

    #endregion
}
