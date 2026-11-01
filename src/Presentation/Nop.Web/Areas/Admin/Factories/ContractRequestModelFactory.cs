// [Mahjar contracts]
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Mahjar;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Mahjar.Contracts;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Models.Mahjar;
using Nop.Web.Framework.Factories;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Web.Areas.Admin.Factories;

/// <summary>
/// Represents the contract request model factory implementation (admin area) [Mahjar contracts]
/// </summary>
public partial class ContractRequestModelFactory : IContractRequestModelFactory
{
    #region Fields

    protected readonly ContractSettings _contractSettings;
    protected readonly CurrencySettings _currencySettings;
    protected readonly IContractService _contractService;
    protected readonly IHandoverModelFactory _handoverModelFactory; //[Mahjar handover]
    protected readonly IPermissionService _permissionService; //[Mahjar handover]
    protected readonly ICurrencyService _currencyService;
    protected readonly ICustomerService _customerService;
    protected readonly IDateTimeHelper _dateTimeHelper;
    protected readonly ILanguageService _languageService;
    protected readonly ILocalizationService _localizationService;
    protected readonly ILocalizedModelFactory _localizedModelFactory;
    protected readonly IPriceFormatter _priceFormatter;
    protected readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public ContractRequestModelFactory(ContractSettings contractSettings,
        CurrencySettings currencySettings,
        IContractService contractService,
        IHandoverModelFactory handoverModelFactory,
        IPermissionService permissionService,
        ICurrencyService currencyService,
        ICustomerService customerService,
        IDateTimeHelper dateTimeHelper,
        ILanguageService languageService,
        ILocalizationService localizationService,
        ILocalizedModelFactory localizedModelFactory,
        IPriceFormatter priceFormatter,
        IWorkContext workContext)
    {
        _contractSettings = contractSettings;
        _currencySettings = currencySettings;
        _contractService = contractService;
        _handoverModelFactory = handoverModelFactory;
        _permissionService = permissionService;
        _currencyService = currencyService;
        _customerService = customerService;
        _dateTimeHelper = dateTimeHelper;
        _languageService = languageService;
        _localizationService = localizationService;
        _localizedModelFactory = localizedModelFactory;
        _priceFormatter = priceFormatter;
        _workContext = workContext;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Get the localized display text of a contract request status
    /// </summary>
    /// <param name="status">Contract request status</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the localized status text
    /// </returns>
    protected virtual async Task<string> GetStatusTextAsync(ContractRequestStatus status)
    {
        return await _localizationService.GetResourceAsync($"mahjar.contracts.status.{status.ToString().ToLowerInvariant()}");
    }

    /// <summary>
    /// Get the localized display text of a contract template kind [Mahjar contracts]
    /// </summary>
    /// <param name="templateType">Contract template kind</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the localized kind text
    /// </returns>
    protected virtual async Task<string> GetTemplateTypeTextAsync(ContractTemplateType templateType)
    {
        return await _localizationService.GetResourceAsync($"admin.mahjar.contracts.template.type.{templateType.ToString().ToLowerInvariant()}");
    }

    #endregion

    #region Methods

    /// <summary>
    /// Prepare contract request search model
    /// </summary>
    /// <param name="searchModel">Contract request search model</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the contract request search model
    /// </returns>
    public virtual async Task<ContractRequestSearchModel> PrepareContractRequestSearchModelAsync(ContractRequestSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        //prepare available statuses ("All" first, value 0)
        searchModel.AvailableStatuses.Add(new SelectListItem
        {
            Value = "0",
            Text = await _localizationService.GetResourceAsync("admin.mahjar.contracts.list.all")
        });
        foreach (var status in Enum.GetValues<ContractRequestStatus>())
        {
            searchModel.AvailableStatuses.Add(new SelectListItem
            {
                Value = ((int)status).ToString(),
                Text = await GetStatusTextAsync(status)
            });
        }

        //prepare page parameters
        searchModel.SetGridPageSize();

        return searchModel;
    }

    /// <summary>
    /// Prepare paged contract request list model
    /// </summary>
    /// <param name="searchModel">Contract request search model</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the contract request list model
    /// </returns>
    public virtual async Task<ContractRequestListModel> PrepareContractRequestListModelAsync(ContractRequestSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        //get parameters to filter contract requests
        var createdFromUtc = !searchModel.CreatedFrom.HasValue ? null
            : (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.CreatedFrom.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync());
        var createdToUtc = !searchModel.CreatedTo.HasValue ? null
            : (DateTime?)_dateTimeHelper.ConvertToUtcTime(searchModel.CreatedTo.Value, await _dateTimeHelper.GetCurrentTimeZoneAsync()).AddDays(1);
        var status = searchModel.StatusId > 0 ? (ContractRequestStatus?)searchModel.StatusId : null;

        //get contract requests
        var contractRequests = await _contractService.SearchRequestsAsync(
            status: status,
            createdFromUtc: createdFromUtc,
            createdToUtc: createdToUtc,
            pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

        //prepare list model
        var model = await new ContractRequestListModel().PrepareToGridAsync(searchModel, contractRequests, () =>
        {
            return contractRequests.SelectAwait(async contractRequest =>
            {
                //grid rows must stay light: the rendered contract HTML, the signature PNG data URL and
                //the snapshot lines are only needed on the Edit page, never in the list JSON payload
                var contractRequestModel = await PrepareContractRequestModelAsync(contractRequest);
                contractRequestModel.ContractBody = null;
                contractRequestModel.SignatureDataUrl = null;
                contractRequestModel.SnapshotLines.Clear();

                return contractRequestModel;
            });
        });

        return model;
    }

    /// <summary>
    /// Prepare contract request model
    /// </summary>
    /// <param name="request">Contract request</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the contract request model
    /// </returns>
    public virtual async Task<ContractRequestModel> PrepareContractRequestModelAsync(ContractRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var model = new ContractRequestModel
        {
            Id = request.Id,
            Number = $"MHGR-C-{request.Id:D5}",
            CustomerId = request.CustomerId,
            StatusId = request.StatusId,
            OrderId = request.OrderId,
            AdminComment = request.AdminComment,
            RejectionReason = request.RejectionReason,
            ContractBody = request.ContractBody,
            SignatureDataUrl = request.SignatureDataUrl,
            SignedName = request.SignedName,
            SignedIp = request.SignedIp,
            //[Mahjar contracts] the contract values of this transaction (empty = the contract setting is used)
            DeliveryDetails = request.DeliveryDetails,
            ReturnDetails = request.ReturnDetails,
            VerificationReference = request.VerificationReference,
            GuaranteeType = request.GuaranteeType,
            GuaranteeAmount = request.GuaranteeAmount,
            ExtraFees = request.ExtraFees,
            TaxAmount = request.TaxAmount,
            LateReturnFee = request.LateReturnFee,
            //sending the contract freezes its rendered body, so the values are editable only before that
            CanEditTerms = request.Status == ContractRequestStatus.Pending,
            CanSendContract = request.Status == ContractRequestStatus.Pending,
            CanApprove = request.Status == ContractRequestStatus.Signed,
            CanReject = request.Status == ContractRequestStatus.Pending ||
                        request.Status == ContractRequestStatus.ContractSent ||
                        request.Status == ContractRequestStatus.Signed
        };

        //dates (user time)
        model.CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(request.CreatedOnUtc, DateTimeKind.Utc);
        if (request.SignedOnUtc.HasValue)
            model.SignedOn = await _dateTimeHelper.ConvertToUserTimeAsync(request.SignedOnUtc.Value, DateTimeKind.Utc);

        //status
        model.Status = await GetStatusTextAsync(request.Status);

        //customer
        var customer = await _customerService.GetCustomerByIdAsync(request.CustomerId);
        if (customer != null)
        {
            model.CustomerEmail = customer.Email;
            model.CustomerFullName = await _customerService.GetCustomerFullNameAsync(customer);
        }

        //contract language
        var language = await _languageService.GetLanguageByIdAsync(request.LanguageId);
        model.LanguageName = language?.Name;

        //frozen cart snapshot lines (amounts in the primary store currency)
        var primaryStoreCurrency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
        var workingLanguage = await _workContext.GetWorkingLanguageAsync();
        var isArabic = workingLanguage?.UniqueSeoCode?.Equals("ar", StringComparison.InvariantCultureIgnoreCase) ?? false;
        var snapshot = await _contractService.DeserializeSnapshotAsync(request);

        //[Mahjar contracts] which template kind these items resolve to — i.e. which contract will be sent
        model.Kind = await GetTemplateTypeTextAsync(_contractService.GetTemplateType(snapshot));

        if (snapshot != null)
        {
            model.SubTotal = await _priceFormatter.FormatPriceAsync(snapshot.SubTotal, true, primaryStoreCurrency);
            model.DepositTotal = await _priceFormatter.FormatPriceAsync(snapshot.DepositTotal, true, primaryStoreCurrency);
            model.GrandTotal = await _priceFormatter.FormatPriceAsync(snapshot.GrandTotal, true, primaryStoreCurrency);

            foreach (var line in snapshot.Lines)
            {
                var lineModel = new ContractSnapshotLineModel
                {
                    ProductId = line.ProductId,
                    Name = isArabic && !string.IsNullOrEmpty(line.ProductNameAr) ? line.ProductNameAr : line.ProductName,
                    Sku = line.Sku,
                    Attributes = line.AttributeDescription,
                    Quantity = line.Quantity,
                    Mode = await _localizationService.GetResourceAsync(line.IsRental
                        ? "mahjar.contracts.snapshot.mode.rent"
                        : "mahjar.contracts.snapshot.mode.buy"),
                    UnitPrice = await _priceFormatter.FormatPriceAsync(line.UnitPrice, true, primaryStoreCurrency),
                    Deposit = await _priceFormatter.FormatPriceAsync(line.Deposit, true, primaryStoreCurrency),
                    LineTotal = await _priceFormatter.FormatPriceAsync(line.LineTotal, true, primaryStoreCurrency)
                };

                //rental period (localized dates + days)
                if (line.IsRental && line.RentalStartUtc.HasValue && line.RentalEndUtc.HasValue)
                {
                    var days = string.Format(await _localizationService.GetResourceAsync("mahjar.contracts.snapshot.days"), line.RentalDays);
                    lineModel.Period = $"{line.RentalStartUtc.Value:d} - {line.RentalEndUtc.Value:d} ({days})";
                }

                model.SnapshotLines.Add(lineModel);
            }
        }

        return model;
    }

    /// <summary>
    /// [Mahjar handover] Prepare the condition checks card of the Edit page and the viewer's rights
    /// </summary>
    /// <param name="model">Contract request model</param>
    /// <param name="request">Contract request</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task PrepareConditionChecksAsync(ContractRequestModel model, ContractRequest request)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(request);

        //handover staff hold only the VIEW permission: hide every transition and the admin-comment editor
        model.CanManage = await _permissionService.AuthorizeAsync(StandardPermission.Mahjar.CONTRACTS_MANAGE);
        model.CanManageHandovers = await _permissionService.AuthorizeAsync(StandardPermission.Mahjar.HANDOVERS_MANAGE);
        model.CanSendContract &= model.CanManage;
        model.CanApprove &= model.CanManage;
        model.CanReject &= model.CanManage;
        model.CanEditTerms &= model.CanManage; //[Mahjar contracts]

        var (rows, preContractReady) = await _handoverModelFactory.PrepareContractConditionChecksAsync(request);
        model.ConditionChecks = rows;
        model.PreContractReady = preContractReady;
    }

    /// <summary>
    /// Prepare contract template search model
    /// </summary>
    /// <param name="searchModel">Contract template search model</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the contract template search model
    /// </returns>
    public virtual Task<ContractTemplateSearchModel> PrepareTemplateSearchModelAsync(ContractTemplateSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        //prepare page parameters
        searchModel.SetGridPageSize();

        return Task.FromResult(searchModel);
    }

    /// <summary>
    /// Prepare paged contract template list model
    /// </summary>
    /// <param name="searchModel">Contract template search model</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the contract template list model
    /// </returns>
    public virtual async Task<ContractTemplateListModel> PrepareTemplateListModelAsync(ContractTemplateSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        //there is one template per contract kind, so the whole (tiny) set is paged in memory
        var templates = (await _contractService.GetAllTemplatesAsync()).ToPagedList(searchModel);

        //prepare list model
        var model = await new ContractTemplateListModel().PrepareToGridAsync(searchModel, templates, () =>
        {
            return templates.SelectAwait(async template =>
            {
                //grid rows must stay light: the tokenized HTML body is only needed on the Edit page
                var templateModel = await PrepareTemplateModelAsync(template);
                templateModel.Body = null;
                templateModel.Locales.Clear();

                return templateModel;
            });
        });

        return model;
    }

    /// <summary>
    /// Prepare contract template content model
    /// </summary>
    /// <param name="template">Contract template</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the contract template content model
    /// </returns>
    public virtual async Task<ContractTemplateContentModel> PrepareTemplateModelAsync(ContractTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        var model = new ContractTemplateContentModel
        {
            Id = template.Id,
            Name = template.Name,
            TemplateTypeId = template.TemplateTypeId,
            TemplateType = await GetTemplateTypeTextAsync(template.TemplateType),
            Body = template.Body
        };

        //last update (user time)
        model.UpdatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(template.UpdatedOnUtc, DateTimeKind.Utc);

        //prepare localized models
        model.Locales = await _localizedModelFactory.PrepareLocalizedModelsAsync<ContractTemplateLocalizedModel>(async (locale, languageId) =>
        {
            locale.Body = await _localizationService.GetLocalizedAsync(template, entity => entity.Body, languageId, false, false);
        });

        return model;
    }

    /// <summary>
    /// Prepare contract settings model
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the contract settings model
    /// </returns>
    public virtual async Task<ContractSettingsModel> PrepareSettingsModelAsync()
    {
        var model = new ContractSettingsModel
        {
            Enabled = _contractSettings.Enabled,
            RequireOnlyForRentals = _contractSettings.RequireOnlyForRentals,
            //[Mahjar contracts] the values the contract templates print through their tokens
            CommercialRegistrationNumber = _contractSettings.CommercialRegistrationNumber,
            AgreementVersion = _contractSettings.AgreementVersion,
            RentalItemValueCap = _contractSettings.RentalItemValueCap,
            LateReturnGraceHours = _contractSettings.LateReturnGraceHours,
            RefundProcessingDays = _contractSettings.RefundProcessingDays,
            DamageObjectionDays = _contractSettings.DamageObjectionDays,
            ComplaintHandlingDays = _contractSettings.ComplaintHandlingDays,
            AmicableSettlementDays = _contractSettings.AmicableSettlementDays,
            OperatorLegalName = _contractSettings.OperatorLegalName,
            SupportChannel = _contractSettings.SupportChannel,
            LateReturnFeeTerms = _contractSettings.LateReturnFeeTerms,
            CancellationFeeTerms = _contractSettings.CancellationFeeTerms,
            GuaranteeType = _contractSettings.GuaranteeType,
            DeliveryDetails = _contractSettings.DeliveryDetails,
            ReturnDetails = _contractSettings.ReturnDetails,
            ExtraFeesTerms = _contractSettings.ExtraFeesTerms,
            TaxTerms = _contractSettings.TaxTerms
        };

        //dates in the admin's own time zone
        if (_contractSettings.AgreementEffectiveDateUtc.HasValue)
            model.AgreementEffectiveDate = await _dateTimeHelper.ConvertToUserTimeAsync(_contractSettings.AgreementEffectiveDateUtc.Value, DateTimeKind.Utc);
        if (_contractSettings.AgreementUpdatedDateUtc.HasValue)
            model.AgreementUpdatedDate = await _dateTimeHelper.ConvertToUserTimeAsync(_contractSettings.AgreementUpdatedDateUtc.Value, DateTimeKind.Utc);

        //the contract is rendered in the customer's language, so every printed text has one value per language
        model.Locales = await _localizedModelFactory.PrepareLocalizedModelsAsync<ContractSettingsLocalizedModel>(async (locale, languageId) =>
        {
            locale.OperatorLegalName = await _localizationService.GetLocalizedSettingAsync(_contractSettings, s => s.OperatorLegalName, languageId, 0, false, false);
            locale.SupportChannel = await _localizationService.GetLocalizedSettingAsync(_contractSettings, s => s.SupportChannel, languageId, 0, false, false);
            locale.LateReturnFeeTerms = await _localizationService.GetLocalizedSettingAsync(_contractSettings, s => s.LateReturnFeeTerms, languageId, 0, false, false);
            locale.CancellationFeeTerms = await _localizationService.GetLocalizedSettingAsync(_contractSettings, s => s.CancellationFeeTerms, languageId, 0, false, false);
            locale.GuaranteeType = await _localizationService.GetLocalizedSettingAsync(_contractSettings, s => s.GuaranteeType, languageId, 0, false, false);
            locale.DeliveryDetails = await _localizationService.GetLocalizedSettingAsync(_contractSettings, s => s.DeliveryDetails, languageId, 0, false, false);
            locale.ReturnDetails = await _localizationService.GetLocalizedSettingAsync(_contractSettings, s => s.ReturnDetails, languageId, 0, false, false);
            locale.ExtraFeesTerms = await _localizationService.GetLocalizedSettingAsync(_contractSettings, s => s.ExtraFeesTerms, languageId, 0, false, false);
            locale.TaxTerms = await _localizationService.GetLocalizedSettingAsync(_contractSettings, s => s.TaxTerms, languageId, 0, false, false);
        });

        return model;
    }

    #endregion
}
