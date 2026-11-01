// [Mahjar contracts]
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Mahjar;
using Nop.Services.Configuration;
using Nop.Services.Helpers; //[Mahjar contracts]
using Nop.Services.Localization;
using Nop.Services.Mahjar.Contracts;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Models.Mahjar;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Web.Areas.Admin.Controllers;

/// <summary>
/// Admin management of the contract-approval stage: review contract requests, send/approve/reject
/// contracts, edit the contract template and the contract settings. [Mahjar contracts]
/// </summary>
public partial class ContractRequestController : BaseAdminController
{
    #region Fields

    protected readonly IContractRequestModelFactory _contractRequestModelFactory;
    protected readonly IContractService _contractService;
    protected readonly IDateTimeHelper _dateTimeHelper; //[Mahjar contracts]
    protected readonly ILocalizationService _localizationService;
    protected readonly ILocalizedEntityService _localizedEntityService;
    protected readonly INotificationService _notificationService;
    protected readonly ISettingService _settingService;

    #endregion

    #region Ctor

    public ContractRequestController(IContractRequestModelFactory contractRequestModelFactory,
        IContractService contractService,
        IDateTimeHelper dateTimeHelper,
        ILocalizationService localizationService,
        ILocalizedEntityService localizedEntityService,
        INotificationService notificationService,
        ISettingService settingService)
    {
        _contractRequestModelFactory = contractRequestModelFactory;
        _contractService = contractService;
        _dateTimeHelper = dateTimeHelper;
        _localizationService = localizationService;
        _localizedEntityService = localizedEntityService;
        _notificationService = notificationService;
        _settingService = settingService;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Persist an admin comment edited in the same Edit form a transition button was posted from,
    /// so pressing Send/Approve/Reject never silently discards it.
    /// </summary>
    protected virtual async Task SaveAdminCommentAsync(ContractRequest contractRequest, ContractRequestModel model)
    {
        var commentChanged = !string.Equals(contractRequest.AdminComment ?? string.Empty, model.AdminComment ?? string.Empty, StringComparison.Ordinal);

        //[Mahjar contracts] the contract values of this transaction are edited in the same form, and Send renders the
        //body from them, so they must be persisted before the transition runs
        var termsChanged = ApplyTransactionTerms(contractRequest, model);

        if (!commentChanged && !termsChanged)
            return;

        contractRequest.AdminComment = model.AdminComment;
        await _contractService.UpdateRequestAsync(contractRequest);
    }

    /// <summary>
    /// [Mahjar contracts] Copy the per-transaction contract values onto the request. They override the contract
    /// settings for this contract only, and are accepted only while it has not been sent (sending freezes the body).
    /// </summary>
    /// <returns>Whether any value changed.</returns>
    protected virtual bool ApplyTransactionTerms(ContractRequest contractRequest, ContractRequestModel model)
    {
        if (contractRequest.Status != ContractRequestStatus.Pending)
            return false;

        static string trim(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        var changed = false;

        void setText(Func<ContractRequest, string> current, Action<string> assign, string value)
        {
            var newValue = trim(value);
            if (string.Equals(current(contractRequest) ?? string.Empty, newValue ?? string.Empty, StringComparison.Ordinal))
                return;

            assign(newValue);
            changed = true;
        }

        void setAmount(Func<ContractRequest, decimal?> current, Action<decimal?> assign, decimal? value)
        {
            if (current(contractRequest) == value)
                return;

            assign(value);
            changed = true;
        }

        setText(r => r.DeliveryDetails, value => contractRequest.DeliveryDetails = value, model.DeliveryDetails);
        setText(r => r.ReturnDetails, value => contractRequest.ReturnDetails = value, model.ReturnDetails);
        setText(r => r.VerificationReference, value => contractRequest.VerificationReference = value, model.VerificationReference);
        setText(r => r.GuaranteeType, value => contractRequest.GuaranteeType = value, model.GuaranteeType);
        setText(r => r.ExtraFees, value => contractRequest.ExtraFees = value, model.ExtraFees);
        setText(r => r.LateReturnFee, value => contractRequest.LateReturnFee = value, model.LateReturnFee);
        setAmount(r => r.GuaranteeAmount, value => contractRequest.GuaranteeAmount = value, model.GuaranteeAmount);
        setAmount(r => r.TaxAmount, value => contractRequest.TaxAmount = value, model.TaxAmount);

        return changed;
    }

    #endregion

    #region Contract requests

    public virtual IActionResult Index()
    {
        return RedirectToAction("List");
    }

    [CheckPermission(StandardPermission.Mahjar.CONTRACTS_VIEW)]
    public virtual async Task<IActionResult> List()
    {
        //prepare model
        var model = await _contractRequestModelFactory.PrepareContractRequestSearchModelAsync(new ContractRequestSearchModel());

        return View(model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Mahjar.CONTRACTS_VIEW)]
    public virtual async Task<IActionResult> List(ContractRequestSearchModel searchModel)
    {
        //prepare model
        var model = await _contractRequestModelFactory.PrepareContractRequestListModelAsync(searchModel);

        return Json(model);
    }

    [CheckPermission(StandardPermission.Mahjar.CONTRACTS_VIEW)]
    public virtual async Task<IActionResult> Edit(int id)
    {
        //try to get a contract request with the specified id
        var contractRequest = await _contractService.GetRequestByIdAsync(id);
        if (contractRequest == null)
            return RedirectToAction("List");

        //prepare model
        var model = await _contractRequestModelFactory.PrepareContractRequestModelAsync(contractRequest);

        //[Mahjar handover] the condition checks card, and what this viewer may do (handover staff only view contracts)
        await _contractRequestModelFactory.PrepareConditionChecksAsync(model, contractRequest);

        return View(model);
    }

    [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
    [FormValueRequired("save", "save-continue")]
    [CheckPermission(StandardPermission.Mahjar.CONTRACTS_MANAGE)]
    public virtual async Task<IActionResult> Edit(ContractRequestModel model, bool continueEditing)
    {
        //try to get a contract request with the specified id
        var contractRequest = await _contractService.GetRequestByIdAsync(model.Id);
        if (contractRequest == null)
            return RedirectToAction("List");

        //only the admin comment and the per-transaction contract values are editable here; all other transitions
        //have dedicated buttons
        contractRequest.AdminComment = model.AdminComment;
        ApplyTransactionTerms(contractRequest, model); //[Mahjar contracts]
        await _contractService.UpdateRequestAsync(contractRequest);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("admin.mahjar.contracts.saved"));

        return continueEditing ? RedirectToAction("Edit", new { id = contractRequest.Id }) : RedirectToAction("List");
    }

    [HttpPost, ActionName("Edit")]
    [FormValueRequired("send-contract")]
    [CheckPermission(StandardPermission.Mahjar.CONTRACTS_MANAGE)]
    public virtual async Task<IActionResult> SendContract(ContractRequestModel model)
    {
        //try to get a contract request with the specified id
        var contractRequest = await _contractService.GetRequestByIdAsync(model.Id);
        if (contractRequest == null)
            return RedirectToAction("List");

        //a comment edited in the same form must survive the transition
        await SaveAdminCommentAsync(contractRequest, model);

        try
        {
            await _contractService.SendContractAsync(contractRequest);
            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("admin.mahjar.contracts.sent"));
        }
        catch (NopException exc)
        {
            _notificationService.ErrorNotification(exc.Message);
        }

        return RedirectToAction("Edit", new { id = contractRequest.Id });
    }

    [HttpPost, ActionName("Edit")]
    [FormValueRequired("approve")]
    [CheckPermission(StandardPermission.Mahjar.CONTRACTS_MANAGE)]
    public virtual async Task<IActionResult> Approve(ContractRequestModel model)
    {
        //try to get a contract request with the specified id
        var contractRequest = await _contractService.GetRequestByIdAsync(model.Id);
        if (contractRequest == null)
            return RedirectToAction("List");

        //a comment edited in the same form must survive the transition
        await SaveAdminCommentAsync(contractRequest, model);

        try
        {
            await _contractService.ApproveAsync(contractRequest);
            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("admin.mahjar.contracts.approved"));
        }
        catch (NopException exc)
        {
            _notificationService.ErrorNotification(exc.Message);
        }

        return RedirectToAction("Edit", new { id = contractRequest.Id });
    }

    [HttpPost, ActionName("Edit")]
    [FormValueRequired("reject")]
    [CheckPermission(StandardPermission.Mahjar.CONTRACTS_MANAGE)]
    public virtual async Task<IActionResult> Reject(ContractRequestModel model)
    {
        //try to get a contract request with the specified id
        var contractRequest = await _contractService.GetRequestByIdAsync(model.Id);
        if (contractRequest == null)
            return RedirectToAction("List");

        //a rejection reason is required (posted with the same Edit form)
        var reason = model.RejectionReason;
        if (string.IsNullOrWhiteSpace(reason))
            reason = Request.Form["RejectionReason"];

        if (string.IsNullOrWhiteSpace(reason))
        {
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("admin.mahjar.contracts.rejectionreasonrequired"));
            return RedirectToAction("Edit", new { id = contractRequest.Id });
        }

        //a comment edited in the same form must survive the transition
        await SaveAdminCommentAsync(contractRequest, model);

        try
        {
            await _contractService.RejectAsync(contractRequest, reason);
            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("admin.mahjar.contracts.rejected"));
        }
        catch (NopException exc)
        {
            _notificationService.ErrorNotification(exc.Message);
        }

        return RedirectToAction("Edit", new { id = contractRequest.Id });
    }

    #endregion

    #region Contract template

    [CheckPermission(StandardPermission.Mahjar.CONTRACTS_MANAGE)]
    public virtual async Task<IActionResult> Templates()
    {
        //prepare model
        var model = await _contractRequestModelFactory.PrepareTemplateSearchModelAsync(new ContractTemplateSearchModel());

        return View(model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Mahjar.CONTRACTS_MANAGE)]
    public virtual async Task<IActionResult> Templates(ContractTemplateSearchModel searchModel)
    {
        //prepare model
        var model = await _contractRequestModelFactory.PrepareTemplateListModelAsync(searchModel);

        return Json(model);
    }

    /// <summary>
    /// The screen used to be a single-template editor at this URL; keep it working for bookmarks and for any
    /// link that still points here. [Mahjar contracts]
    /// </summary>
    [CheckPermission(StandardPermission.Mahjar.CONTRACTS_MANAGE)]
    public virtual IActionResult Template()
    {
        return RedirectToAction("Templates");
    }

    [CheckPermission(StandardPermission.Mahjar.CONTRACTS_MANAGE)]
    public virtual async Task<IActionResult> EditTemplate(int id)
    {
        //try to get a contract template with the specified id
        var template = await _contractService.GetTemplateByIdAsync(id);
        if (template == null)
            return RedirectToAction("Templates");

        //prepare model
        var model = await _contractRequestModelFactory.PrepareTemplateModelAsync(template);

        return View(model);
    }

    [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
    [FormValueRequired("save", "save-continue")]
    [CheckPermission(StandardPermission.Mahjar.CONTRACTS_MANAGE)]
    public virtual async Task<IActionResult> EditTemplate(ContractTemplateContentModel model, bool continueEditing)
    {
        //try to get a contract template with the specified id
        var template = await _contractService.GetTemplateByIdAsync(model.Id);
        if (template == null)
        {
            _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("admin.mahjar.contracts.template.notfound"));

            return RedirectToAction("Templates");
        }

        //the kind is what binds a template to a cart, so it is fixed — only the name and the bodies are editable
        template.Name = model.Name;
        template.Body = model.Body;
        await _contractService.UpdateTemplateAsync(template);

        //locales
        foreach (var localized in model.Locales)
        {
            await _localizedEntityService.SaveLocalizedValueAsync(template,
                x => x.Body,
                localized.Body,
                localized.LanguageId);
        }

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("admin.mahjar.contracts.template.updated"));

        if (!continueEditing)
            return RedirectToAction("Templates");

        return RedirectToAction("EditTemplate", new { id = template.Id });
    }

    #endregion

    #region Contract settings

    [CheckPermission(StandardPermission.Mahjar.CONTRACTS_MANAGE)]
    public virtual async Task<IActionResult> Settings()
    {
        //prepare model
        var model = await _contractRequestModelFactory.PrepareSettingsModelAsync();

        return View(model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Mahjar.CONTRACTS_MANAGE)]
    public virtual async Task<IActionResult> Settings(ContractSettingsModel model)
    {
        //load and update the settings
        var contractSettings = await _settingService.LoadSettingAsync<ContractSettings>();
        contractSettings.Enabled = model.Enabled;
        contractSettings.RequireOnlyForRentals = model.RequireOnlyForRentals;

        //[Mahjar contracts] the values the contract templates print through their tokens
        contractSettings.CommercialRegistrationNumber = model.CommercialRegistrationNumber;
        contractSettings.AgreementVersion = model.AgreementVersion;
        //the admin types the dates in their own time zone
        contractSettings.AgreementEffectiveDateUtc = model.AgreementEffectiveDate.HasValue
            ? _dateTimeHelper.ConvertToUtcTime(model.AgreementEffectiveDate.Value)
            : (DateTime?)null;
        contractSettings.AgreementUpdatedDateUtc = model.AgreementUpdatedDate.HasValue
            ? _dateTimeHelper.ConvertToUtcTime(model.AgreementUpdatedDate.Value)
            : (DateTime?)null;
        contractSettings.RentalItemValueCap = model.RentalItemValueCap;
        contractSettings.LateReturnGraceHours = model.LateReturnGraceHours;
        contractSettings.RefundProcessingDays = model.RefundProcessingDays;
        contractSettings.DamageObjectionDays = model.DamageObjectionDays;
        contractSettings.ComplaintHandlingDays = model.ComplaintHandlingDays;
        contractSettings.AmicableSettlementDays = model.AmicableSettlementDays;

        //the "Standard" tab of the localized editor feeds the setting itself (the fallback of every language)
        contractSettings.OperatorLegalName = model.OperatorLegalName;
        contractSettings.SupportChannel = model.SupportChannel;
        contractSettings.LateReturnFeeTerms = model.LateReturnFeeTerms;
        contractSettings.CancellationFeeTerms = model.CancellationFeeTerms;
        contractSettings.GuaranteeType = model.GuaranteeType;
        contractSettings.DeliveryDetails = model.DeliveryDetails;
        contractSettings.ReturnDetails = model.ReturnDetails;
        contractSettings.ExtraFeesTerms = model.ExtraFeesTerms;
        contractSettings.TaxTerms = model.TaxTerms;

        await _settingService.SaveSettingAsync(contractSettings);

        //settings are cached
        await _settingService.ClearCacheAsync();

        //[Mahjar contracts] one text per language. MUST run after the setting itself has been saved and the cache
        //cleared: SaveLocalizedSettingAsync resolves the Setting row through the cache and silently does nothing
        //when it cannot find one.
        foreach (var localized in model.Locales)
        {
            await _localizationService.SaveLocalizedSettingAsync(contractSettings, s => s.OperatorLegalName, localized.LanguageId, localized.OperatorLegalName);
            await _localizationService.SaveLocalizedSettingAsync(contractSettings, s => s.SupportChannel, localized.LanguageId, localized.SupportChannel);
            await _localizationService.SaveLocalizedSettingAsync(contractSettings, s => s.LateReturnFeeTerms, localized.LanguageId, localized.LateReturnFeeTerms);
            await _localizationService.SaveLocalizedSettingAsync(contractSettings, s => s.CancellationFeeTerms, localized.LanguageId, localized.CancellationFeeTerms);
            await _localizationService.SaveLocalizedSettingAsync(contractSettings, s => s.GuaranteeType, localized.LanguageId, localized.GuaranteeType);
            await _localizationService.SaveLocalizedSettingAsync(contractSettings, s => s.DeliveryDetails, localized.LanguageId, localized.DeliveryDetails);
            await _localizationService.SaveLocalizedSettingAsync(contractSettings, s => s.ReturnDetails, localized.LanguageId, localized.ReturnDetails);
            await _localizationService.SaveLocalizedSettingAsync(contractSettings, s => s.ExtraFeesTerms, localized.LanguageId, localized.ExtraFeesTerms);
            await _localizationService.SaveLocalizedSettingAsync(contractSettings, s => s.TaxTerms, localized.LanguageId, localized.TaxTerms);
        }

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("admin.mahjar.contracts.settings.updated"));

        return RedirectToAction("Settings");
    }

    #endregion
}
