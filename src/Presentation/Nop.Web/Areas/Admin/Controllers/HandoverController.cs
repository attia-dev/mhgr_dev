// [Mahjar handover]
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Mahjar;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Services.Mahjar.Handover;
using Nop.Services.Mahjar.ItemStatus;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Models.Mahjar;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Web.Areas.Admin.Controllers;

/// <summary>
/// The handover desk (Admin, Sales, Handovers): the work queues of the handover staff and the condition check
/// editor — photos, video, notes and damage at intake, before signing, at delivery and at a rental return.
/// Every action needs <see cref="StandardPermission.Mahjar.HANDOVERS_MANAGE"/> (Administrators + HandoverStaff).
/// [Mahjar handover]
/// </summary>
public partial class HandoverController : BaseAdminController
{
    #region Fields

    protected readonly IConditionCheckService _conditionCheckService;
    protected readonly IHandoverAssignmentService _assignmentService; //[Mahjar handover]
    protected readonly IHandoverModelFactory _handoverModelFactory;
    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationService _notificationService;
    protected readonly IProductService _productService;
    protected readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public HandoverController(IConditionCheckService conditionCheckService,
        IHandoverAssignmentService assignmentService,
        IHandoverModelFactory handoverModelFactory,
        ILocalizationService localizationService,
        INotificationService notificationService,
        IProductService productService,
        IWorkContext workContext)
    {
        _conditionCheckService = conditionCheckService;
        _assignmentService = assignmentService;
        _handoverModelFactory = handoverModelFactory;
        _localizationService = localizationService;
        _notificationService = notificationService;
        _productService = productService;
        _workContext = workContext;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Copies the editable fields posted with the check form onto a DRAFT check and saves it, so pressing Confirm or
    /// Copy never silently discards what the employee just typed. Returns false (with an error notification) when the
    /// external video URL is refused.
    /// </summary>
    protected virtual async Task<bool> SaveFieldsAsync(ConditionCheck check, ConditionCheckModel model)
    {
        if (!_conditionCheckService.IsEditable(check))
            return true;

        if (!ItemStatusVideoUrlHelper.TryNormalize(model.VideoUrl, out var videoUrl))
        {
            _notificationService.ErrorNotification(string.Format(
                await _localizationService.GetResourceAsync("Admin.Catalog.Products.ItemStatus.Alert.VideoUrlInvalid"),
                ItemStatusVideoUrlHelper.MAX_VIDEO_URL_LENGTH));
            return false;
        }

        check.ConditionGradeId = model.ConditionGradeId;
        check.ConditionNotes = string.IsNullOrWhiteSpace(model.ConditionNotes) ? null : model.ConditionNotes.Trim();
        check.HasDamage = model.HasDamage;
        check.DamageSeverityId = model.HasDamage ? model.DamageSeverityId : 0;
        check.DamageDescription = model.HasDamage && !string.IsNullOrWhiteSpace(model.DamageDescription) ? model.DamageDescription.Trim() : null;
        check.InternalComment = string.IsNullOrWhiteSpace(model.InternalComment) ? null : model.InternalComment.Trim();
        check.VideoUrl = videoUrl;
        check.DamageDeduction = check.CheckType == ConditionCheckType.Return && model.HasDamage
            ? Math.Round(Math.Max(model.DamageDeduction, decimal.Zero), 2)
            : decimal.Zero;

        await _conditionCheckService.UpdateCheckAsync(check);
        return true;
    }

    #endregion

    #region Queues

    public virtual IActionResult Index()
    {
        return RedirectToAction("List");
    }

    /// <summary>
    /// The desk's work queues. A task notification deep-links here carrying the order it is about, so the employee
    /// lands on that order's rows instead of the whole queue. It stops at the row rather than opening the check:
    /// starting one CREATES a record, which is a POST by design (see <see cref="Open"/>), so a link must not do it.
    /// [Mahjar handover]
    /// </summary>
    [CheckPermission(StandardPermission.Mahjar.HANDOVERS_MANAGE)]
    public virtual async Task<IActionResult> List(int stageId = 0, string keyword = null)
    {
        //prepare model
        var model = await _handoverModelFactory.PrepareHandoverSearchModelAsync(new HandoverSearchModel
        {
            StageId = stageId,
            SearchKeyword = keyword
        });

        return View(model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Mahjar.HANDOVERS_MANAGE)]
    public virtual async Task<IActionResult> List(HandoverSearchModel searchModel)
    {
        //prepare model
        var model = await _handoverModelFactory.PrepareHandoverTaskListModelAsync(searchModel);

        return Json(model);
    }

    /// <summary>
    /// Opens the check of a queue row, starting a draft when there is none yet. A POST (antiforgery-validated by
    /// BaseAdminController) because it can create a row: a GET would let any external link start drafts in the name of
    /// a logged-in employee. An already started check is opened with a plain GET of <see cref="Check(int)"/>.
    /// </summary>
    [HttpPost]
    [CheckPermission(StandardPermission.Mahjar.HANDOVERS_MANAGE)]
    public virtual async Task<IActionResult> Open(int type, int productId = 0, int contractRequestId = 0, int orderItemId = 0)
    {
        if (!Enum.IsDefined(typeof(ConditionCheckType), type))
            return RedirectToAction("List");

        try
        {
            var check = await _conditionCheckService.GetOrCreateCheckAsync((ConditionCheckType)type, productId, contractRequestId,
                orderItemId, await _workContext.GetCurrentCustomerAsync());

            return RedirectToAction("Check", new { id = check.Id });
        }
        catch (NopException exc)
        {
            _notificationService.ErrorNotification(exc.Message);
            return RedirectToAction("List");
        }
    }

    #endregion

    #region Intake

    [CheckPermission(StandardPermission.Mahjar.HANDOVERS_MANAGE)]
    public virtual IActionResult Intake()
    {
        return View(new HandoverIntakeModel());
    }

    /// <summary>Finds the product a partner delivered (name or SKU), for the intake picker.</summary>
    [HttpPost]
    [CheckPermission(StandardPermission.Mahjar.HANDOVERS_MANAGE)]
    public virtual async Task<IActionResult> SearchProducts(string term)
    {
        term = term?.Trim();
        if (string.IsNullOrEmpty(term) || term.Length < 2)
            return Json(Array.Empty<object>());

        var products = await _productService.SearchProductsAsync(pageIndex: 0, pageSize: 20, keywords: term,
            searchSku: true, showHidden: true);

        var result = new List<object>();
        foreach (var product in products)
        {
            result.Add(new
            {
                id = product.Id,
                name = await _localizationService.GetLocalizedAsync(product, p => p.Name),
                sku = product.Sku ?? string.Empty
            });
        }

        return Json(result);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Mahjar.HANDOVERS_MANAGE)]
    public virtual Task<IActionResult> StartIntake(int productId)
    {
        return Open((int)ConditionCheckType.Intake, productId);
    }

    #endregion

    #region Condition check

    [CheckPermission(StandardPermission.Mahjar.HANDOVERS_MANAGE)]
    public virtual async Task<IActionResult> Check(int id)
    {
        var check = await _conditionCheckService.GetCheckByIdAsync(id);
        if (check == null)
            return RedirectToAction("List");

        //[Mahjar handover] a delivery / return check belongs to the employee its order was assigned to. The service
        //refuses the work itself; this keeps the editor out of sight too, instead of showing a form that cannot save.
        if (check.CheckType is ConditionCheckType.Delivery or ConditionCheckType.Return && check.OrderId > 0 &&
            !await _assignmentService.MayWorkOrderAsync(check.OrderId, await _workContext.GetCurrentCustomerAsync()))
        {
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("admin.mahjar.handover.assign.notyours"));

            return RedirectToAction("List");
        }

        //prepare model
        var model = await _handoverModelFactory.PrepareConditionCheckModelAsync(check);

        return View(model);
    }

    [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
    [FormValueRequired("save", "save-continue")]
    [CheckPermission(StandardPermission.Mahjar.HANDOVERS_MANAGE)]
    public virtual async Task<IActionResult> Check(ConditionCheckModel model, bool continueEditing)
    {
        var check = await _conditionCheckService.GetCheckByIdAsync(model.Id);
        if (check == null)
            return RedirectToAction("List");

        try
        {
            if (!await SaveFieldsAsync(check, model))
                return RedirectToAction("Check", new { id = check.Id });

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("admin.mahjar.handover.saved"));
        }
        catch (NopException exc)
        {
            _notificationService.ErrorNotification(exc.Message);
        }

        return continueEditing ? RedirectToAction("Check", new { id = check.Id }) : RedirectToAction("List");
    }

    [HttpPost, ActionName("Check")]
    [FormValueRequired("confirm")]
    [CheckPermission(StandardPermission.Mahjar.HANDOVERS_MANAGE)]
    public virtual async Task<IActionResult> Confirm(ConditionCheckModel model)
    {
        var check = await _conditionCheckService.GetCheckByIdAsync(model.Id);
        if (check == null)
            return RedirectToAction("List");

        try
        {
            if (!await SaveFieldsAsync(check, model))
                return RedirectToAction("Check", new { id = check.Id });

            var result = await _conditionCheckService.ConfirmAsync(check, await _workContext.GetCurrentCustomerAsync());

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("admin.mahjar.handover.confirmed"));
            foreach (var message in result.Messages)
                _notificationService.SuccessNotification(message);
            foreach (var warning in result.Warnings)
                _notificationService.WarningNotification(warning);
        }
        catch (NopException exc)
        {
            _notificationService.ErrorNotification(exc.Message);
        }

        return RedirectToAction("Check", new { id = check.Id });
    }

    [HttpPost, ActionName("Check")]
    [FormValueRequired("import")]
    [CheckPermission(StandardPermission.Mahjar.HANDOVERS_MANAGE)]
    public virtual async Task<IActionResult> ImportFromProduct(ConditionCheckModel model)
    {
        var check = await _conditionCheckService.GetCheckByIdAsync(model.Id);
        if (check == null)
            return RedirectToAction("List");

        try
        {
            if (!await SaveFieldsAsync(check, model))
                return RedirectToAction("Check", new { id = check.Id });

            var copied = await _conditionCheckService.ImportFromProductStatusAsync(check);
            if (copied > 0)
                _notificationService.SuccessNotification(string.Format(await _localizationService.GetResourceAsync("admin.mahjar.handover.imported"), copied));
            else
                _notificationService.WarningNotification(await _localizationService.GetResourceAsync("admin.mahjar.handover.imported.none"));
        }
        catch (NopException exc)
        {
            _notificationService.ErrorNotification(exc.Message);
        }

        return RedirectToAction("Check", new { id = check.Id });
    }

    [HttpPost, ActionName("Check")]
    [FormValueRequired("delete-draft")]
    [CheckPermission(StandardPermission.Mahjar.HANDOVERS_MANAGE)]
    public virtual async Task<IActionResult> DeleteDraft(ConditionCheckModel model)
    {
        var check = await _conditionCheckService.GetCheckByIdAsync(model.Id);
        if (check == null)
            return RedirectToAction("List");

        try
        {
            await _conditionCheckService.DeleteDraftAsync(check);
            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("admin.mahjar.handover.deleted"));
        }
        catch (NopException exc)
        {
            _notificationService.ErrorNotification(exc.Message);
            return RedirectToAction("Check", new { id = check.Id });
        }

        return RedirectToAction("List");
    }

    #endregion

    #region Photos

    /// <summary>
    /// Adds photos through the FilePond uploader. FilePond cannot send an antiforgery token, hence
    /// [IgnoreAntiforgeryToken], and it reads the failure text from "message" — so the answer is always JSON.
    /// </summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    [CheckPermission(StandardPermission.Mahjar.HANDOVERS_MANAGE)]
    public virtual async Task<IActionResult> CheckPhotoAdd(int checkId, IFormCollection form)
    {
        var check = await _conditionCheckService.GetCheckByIdAsync(checkId);
        if (check == null)
            return Json(new { success = false, message = await _localizationService.GetResourceAsync("admin.mahjar.handover.notfound") });

        try
        {
            var error = await _conditionCheckService.AddPhotosAsync(check, form.Files.ToList());
            if (!string.IsNullOrEmpty(error))
                return Json(new { success = false, message = error });
        }
        catch (Exception exc)
        {
            return Json(new
            {
                success = false,
                message = $"{await _localizationService.GetResourceAsync("admin.mahjar.handover.photo.addfailed")} {exc.Message}"
            });
        }

        return Json(new { success = true });
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Mahjar.HANDOVERS_MANAGE)]
    public virtual async Task<IActionResult> CheckPhotoList(ConditionCheckPictureSearchModel searchModel)
    {
        var check = await _conditionCheckService.GetCheckByIdAsync(searchModel.ConditionCheckId)
            ?? throw new ArgumentException("No condition check found with the specified id");

        //prepare model
        var model = await _handoverModelFactory.PrepareConditionCheckPictureListModelAsync(searchModel, check);

        return Json(model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Mahjar.HANDOVERS_MANAGE)]
    public virtual async Task<IActionResult> CheckPhotoUpdate(ConditionCheckPictureModel model)
    {
        var picture = await _conditionCheckService.GetPictureByIdAsync(model.Id)
            ?? throw new ArgumentException("No condition check photo found with the specified id");

        try
        {
            picture.Caption = string.IsNullOrWhiteSpace(model.Caption) ? null : model.Caption.Trim();
            picture.DisplayOrder = model.DisplayOrder;
            await _conditionCheckService.UpdatePictureAsync(picture);
        }
        catch (NopException exc)
        {
            return ErrorJson(exc.Message);
        }

        return new NullJsonResult();
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Mahjar.HANDOVERS_MANAGE)]
    public virtual async Task<IActionResult> CheckPhotoDelete(int id)
    {
        var picture = await _conditionCheckService.GetPictureByIdAsync(id)
            ?? throw new ArgumentException("No condition check photo found with the specified id");

        try
        {
            await _conditionCheckService.DeletePictureAsync(picture);
        }
        catch (NopException exc)
        {
            return ErrorJson(exc.Message);
        }

        return new NullJsonResult();
    }

    #endregion

    #region Video

    /// <summary>
    /// Receives the recorded video. The two limit attributes raise the per-request ceiling for THIS action only; the
    /// web.config requestLimits entry raises the IIS half (200 MB), exactly as for the item-status video.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(209_715_200)]
    [RequestFormLimits(MultipartBodyLengthLimit = 209_715_200)]
    [CheckPermission(StandardPermission.Mahjar.HANDOVERS_MANAGE)]
    public virtual async Task<IActionResult> CheckVideoUpload(int checkId)
    {
        var check = await _conditionCheckService.GetCheckByIdAsync(checkId);
        if (check == null)
            return Json(new { success = false, error = await _localizationService.GetResourceAsync("admin.mahjar.handover.notfound") });

        try
        {
            var (videoFile, error) = await _conditionCheckService.UploadVideoAsync(check, Request.Form.Files.FirstOrDefault());
            if (!string.IsNullOrEmpty(error))
                return Json(new { success = false, error });

            return Json(new { success = true, videoFile });
        }
        catch (NopException exc)
        {
            return Json(new { success = false, error = exc.Message });
        }
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Mahjar.HANDOVERS_MANAGE)]
    public virtual async Task<IActionResult> CheckVideoDelete(int checkId)
    {
        var check = await _conditionCheckService.GetCheckByIdAsync(checkId);
        if (check == null)
            return Json(new { success = false, error = await _localizationService.GetResourceAsync("admin.mahjar.handover.notfound") });

        try
        {
            await _conditionCheckService.RemoveVideoAsync(check);
        }
        catch (NopException exc)
        {
            return Json(new { success = false, error = exc.Message });
        }

        return Json(new { success = true });
    }

    #endregion
}
