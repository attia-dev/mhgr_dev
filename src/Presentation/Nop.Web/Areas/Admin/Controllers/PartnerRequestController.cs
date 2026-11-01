// [Mahjar partner-request]
using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Mahjar.Agreements;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Mahjar.Agreements;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Vendors;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Models.Mahjar;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Web.Areas.Admin.Controllers;

/// <summary>
/// [Mahjar partner-request] The queue of partner applications: what was submitted, the copy of the agreement
/// that was accepted, and the decision. Approving activates the partner exactly as the partner page does —
/// through the same event, so the Vendors role and the notification follow. Rejecting keeps the record, with
/// its reason, and releases the customer so they can apply again.
/// </summary>
public partial class PartnerRequestController : BaseAdminController
{
    #region Fields

    protected readonly ICustomerActivityService _customerActivityService;
    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationService _notificationService;
    protected readonly IPartnerRequestModelFactory _partnerRequestModelFactory;
    protected readonly IPartnerRequestService _partnerRequestService;
    protected readonly IVendorService _vendorService;

    #endregion

    #region Ctor

    public PartnerRequestController(ICustomerActivityService customerActivityService,
        ILocalizationService localizationService,
        INotificationService notificationService,
        IPartnerRequestModelFactory partnerRequestModelFactory,
        IPartnerRequestService partnerRequestService,
        IVendorService vendorService)
    {
        _customerActivityService = customerActivityService;
        _localizationService = localizationService;
        _notificationService = notificationService;
        _partnerRequestModelFactory = partnerRequestModelFactory;
        _partnerRequestService = partnerRequestService;
        _vendorService = vendorService;
    }

    #endregion

    #region Methods

    [CheckPermission(StandardPermission.Customers.VENDORS_VIEW)]
    public virtual async Task<IActionResult> Index()
    {
        return RedirectToAction("List");
    }

    [CheckPermission(StandardPermission.Customers.VENDORS_VIEW)]
    public virtual async Task<IActionResult> List()
    {
        var model = await _partnerRequestModelFactory.PreparePartnerRequestSearchModelAsync(new PartnerRequestSearchModel());

        return View(model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Customers.VENDORS_VIEW)]
    public virtual async Task<IActionResult> List(PartnerRequestSearchModel searchModel)
    {
        var model = await _partnerRequestModelFactory.PreparePartnerRequestListModelAsync(searchModel);

        return Json(model);
    }

    /// <summary>
    /// Where a "new partner request" notification leads. The decision is taken here rather than baked into the
    /// notification, because a row written months ago cannot know what became of the application: it may have
    /// been reviewed, or the partner may have been created straight in admin and never have been a request at
    /// all. Falls back to the partner page, and then to the list, so the link is never a dead end.
    /// </summary>
    [CheckPermission(StandardPermission.Customers.VENDORS_VIEW)]
    public virtual async Task<IActionResult> Open(int id)
    {
        var vendor = await _vendorService.GetVendorByIdAsync(id);
        if (vendor == null || vendor.Deleted)
            return RedirectToAction("List");

        if (vendor.ApplicationStatus != PartnerApplicationStatus.NotAnApplication)
            return RedirectToAction("Review", new { id = vendor.Id });

        return RedirectToAction("Edit", "Vendor", new { id = vendor.Id });
    }

    [CheckPermission(StandardPermission.Customers.VENDORS_VIEW)]
    public virtual async Task<IActionResult> Review(int id)
    {
        var vendor = await _vendorService.GetVendorByIdAsync(id);
        if (vendor == null || vendor.Deleted || vendor.ApplicationStatus == PartnerApplicationStatus.NotAnApplication)
            return RedirectToAction("List");

        var model = await _partnerRequestModelFactory.PreparePartnerRequestModelAsync(null, vendor);

        return View(model);
    }

    /// <summary>
    /// Opens the copy of the agreement the applicant accepted, frozen at that moment. It is served from the
    /// record rather than re-rendered, because the settings behind it may have changed since.
    /// </summary>
    [CheckPermission(StandardPermission.Customers.VENDORS_VIEW)]
    public virtual async Task<IActionResult> Agreement(int id)
    {
        var vendor = await _vendorService.GetVendorByIdAsync(id);
        if (vendor == null || vendor.Deleted || string.IsNullOrEmpty(vendor.AgreementBody))
            return RedirectToAction("List");

        return Content(vendor.AgreementBody, "text/html; charset=utf-8");
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Customers.VENDORS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> Approve(int id)
    {
        var vendor = await _vendorService.GetVendorByIdAsync(id);
        if (vendor == null || vendor.Deleted)
            return RedirectToAction("List");

        //a decision is taken once; a second one would re-notify the applicant
        if (vendor.ApplicationStatus != PartnerApplicationStatus.Pending)
        {
            _notificationService.WarningNotification(await _localizationService.GetResourceAsync("Admin.Mahjar.PartnerRequests.AlreadyReviewed"));

            return RedirectToAction("Review", new { id = vendor.Id });
        }

        await _partnerRequestService.ApproveAsync(vendor);

        await _customerActivityService.InsertActivityAsync("EditVendor",
            string.Format(await _localizationService.GetResourceAsync("ActivityLog.EditVendor"), vendor.Id), vendor);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Mahjar.PartnerRequests.Approved"));

        return RedirectToAction("Review", new { id = vendor.Id });
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Customers.VENDORS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> Reject(int id, string rejectionReason)
    {
        var vendor = await _vendorService.GetVendorByIdAsync(id);
        if (vendor == null || vendor.Deleted)
            return RedirectToAction("List");

        if (vendor.ApplicationStatus != PartnerApplicationStatus.Pending)
        {
            _notificationService.WarningNotification(await _localizationService.GetResourceAsync("Admin.Mahjar.PartnerRequests.AlreadyReviewed"));

            return RedirectToAction("Review", new { id = vendor.Id });
        }

        //the applicant is told why, so a reason is not optional
        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Admin.Mahjar.PartnerRequests.Reject.ReasonRequired"));

            return RedirectToAction("Review", new { id = vendor.Id });
        }

        await _partnerRequestService.RejectAsync(vendor, rejectionReason);

        await _customerActivityService.InsertActivityAsync("EditVendor",
            string.Format(await _localizationService.GetResourceAsync("ActivityLog.EditVendor"), vendor.Id), vendor);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Mahjar.PartnerRequests.Rejected"));

        return RedirectToAction("Review", new { id = vendor.Id });
    }

    #endregion
}
