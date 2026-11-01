// [Mahjar notifications]
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Mahjar;
using Nop.Services.Localization;
using Nop.Services.Mahjar.Notifications;
using Nop.Services.Messages;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Models.Notification;

namespace Nop.Web.Areas.Admin.Controllers;

/// <summary>
/// Admin/vendor in-app notifications: the full list page + settings, and the AJAX endpoints the navbar bell
/// uses. The feed is recipient-scoped (vendor → vendor's own; otherwise admin). [Mahjar notifications]
/// </summary>
public partial class NotificationController : BaseAdminController
{
    #region Fields

    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationFeedService _feedService;
    protected readonly INotificationModelFactory _notificationModelFactory;
    protected readonly INotificationService _notificationService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public NotificationController(ILocalizationService localizationService,
        INotificationFeedService feedService,
        INotificationModelFactory notificationModelFactory,
        INotificationService notificationService,
        IStoreContext storeContext,
        IWorkContext workContext)
    {
        _localizationService = localizationService;
        _feedService = feedService;
        _notificationModelFactory = notificationModelFactory;
        _notificationService = notificationService;
        _storeContext = storeContext;
        _workContext = workContext;
    }

    #endregion

    #region Utilities

    protected virtual async Task<(NotificationRecipientType type, int customerId, int vendorId)> ResolveRecipientAsync()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var vendor = await _workContext.GetCurrentVendorAsync();
        return vendor != null
            ? (NotificationRecipientType.Vendor, customer.Id, vendor.Id)
            : (NotificationRecipientType.Admin, customer.Id, 0);
    }

    #endregion

    #region Methods

    public virtual IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public virtual async Task<IActionResult> List(int pageNumber = 0)
    {
        var model = await _notificationModelFactory.PrepareListModelAsync(pageNumber);
        return View(model);
    }

    [HttpGet]
    public virtual async Task<IActionResult> Feed(int page = 0)
    {
        var model = await _notificationModelFactory.PrepareListModelAsync(page);
        return Json(new { success = true, model });
    }

    [HttpPost]
    public virtual async Task<IActionResult> MarkRead(int id)
    {
        var (type, customerId, vendorId) = await ResolveRecipientAsync();
        await _feedService.MarkAsReadAsync(id, customerId);
        var store = await _storeContext.GetCurrentStoreAsync();
        var unread = await _feedService.GetUnreadCountAsync(customerId, type, vendorId, store.Id);
        return Json(new { success = true, unread });
    }

    [HttpPost]
    public virtual async Task<IActionResult> MarkAllRead()
    {
        var (type, customerId, vendorId) = await ResolveRecipientAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        await _feedService.MarkAllAsReadAsync(customerId, type, vendorId, store.Id);
        return Json(new { success = true, unread = 0 });
    }

    public virtual async Task<IActionResult> Settings()
    {
        var model = await _notificationModelFactory.PreparePreferenceModelAsync();
        return View(model);
    }

    [HttpPost]
    public virtual async Task<IActionResult> Settings(NotificationPreferenceModel model)
    {
        await _notificationModelFactory.SavePreferencesAsync(model);
        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("mahjar.notifications.settings.saved"));
        return RedirectToAction("Settings");
    }

    #endregion
}
