// [Mahjar partner-request]
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Mahjar.Agreements;
using Nop.Core.Domain.Vendors;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Mahjar.Agreements;
using Nop.Web.Areas.Admin.Models.Mahjar;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Web.Areas.Admin.Factories;

/// <summary>
/// [Mahjar partner-request] Builds the partner request queue and the review screen
/// </summary>
public partial class PartnerRequestModelFactory : IPartnerRequestModelFactory
{
    #region Fields

    protected readonly ICustomerService _customerService;
    protected readonly IDateTimeHelper _dateTimeHelper;
    protected readonly ILocalizationService _localizationService;
    protected readonly IPartnerAgreementService _partnerAgreementService;
    protected readonly IPartnerRequestService _partnerRequestService;
    protected readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public PartnerRequestModelFactory(ICustomerService customerService,
        IDateTimeHelper dateTimeHelper,
        ILocalizationService localizationService,
        IPartnerAgreementService partnerAgreementService,
        IPartnerRequestService partnerRequestService,
        IWorkContext workContext)
    {
        _customerService = customerService;
        _dateTimeHelper = dateTimeHelper;
        _localizationService = localizationService;
        _partnerAgreementService = partnerAgreementService;
        _partnerRequestService = partnerRequestService;
        _workContext = workContext;
    }

    #endregion

    #region Utilities

    /// <summary>The localized caption of an application state.</summary>
    protected virtual async Task<string> GetStatusNameAsync(PartnerApplicationStatus status, int languageId)
    {
        var resourceName = status switch
        {
            PartnerApplicationStatus.Pending => "Admin.Mahjar.PartnerRequests.Status.Pending",
            PartnerApplicationStatus.Approved => "Admin.Mahjar.PartnerRequests.Status.Approved",
            PartnerApplicationStatus.Rejected => "Admin.Mahjar.PartnerRequests.Status.Rejected",
            _ => null
        };

        return resourceName == null
            ? string.Empty
            : await _localizationService.GetResourceAsync(resourceName, languageId);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Prepares the search model of the partner request queue
    /// </summary>
    public virtual async Task<PartnerRequestSearchModel> PreparePartnerRequestSearchModelAsync(PartnerRequestSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        var languageId = (await _workContext.GetWorkingLanguageAsync()).Id;

        //the queue opens on what needs a decision; "all" is one click away
        searchModel.StatusId = (int)PartnerApplicationStatus.Pending;
        searchModel.AvailableStatuses = new List<SelectListItem>
        {
            new()
            {
                Value = "0",
                Text = await _localizationService.GetResourceAsync("Admin.Mahjar.PartnerRequests.Status.All", languageId)
            }
        };

        foreach (var status in new[] { PartnerApplicationStatus.Pending, PartnerApplicationStatus.Approved, PartnerApplicationStatus.Rejected })
        {
            searchModel.AvailableStatuses.Add(new SelectListItem
            {
                Value = ((int)status).ToString(),
                Text = await GetStatusNameAsync(status, languageId),
                Selected = status == PartnerApplicationStatus.Pending
            });
        }

        searchModel.SetGridPageSize();

        return searchModel;
    }

    /// <summary>
    /// Prepares the paged list of partner requests
    /// </summary>
    public virtual async Task<PartnerRequestListModel> PreparePartnerRequestListModelAsync(PartnerRequestSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        var status = searchModel.StatusId > 0 ? (PartnerApplicationStatus)searchModel.StatusId : (PartnerApplicationStatus?)null;

        var requests = await _partnerRequestService.SearchRequestsAsync(status, searchModel.SearchKeyword,
            searchModel.Page - 1, searchModel.PageSize);

        var languageId = (await _workContext.GetWorkingLanguageAsync()).Id;

        return await new PartnerRequestListModel().PrepareToGridAsync(searchModel, requests, () =>
        {
            return requests.SelectAwait(async vendor =>
            {
                var model = new PartnerRequestModel
                {
                    Id = vendor.Id,
                    Name = vendor.Name,
                    Email = vendor.Email,
                    StatusId = vendor.ApplicationStatusId,
                    StatusName = await GetStatusNameAsync(vendor.ApplicationStatus, languageId),
                    VendorTypeName = await _localizationService.GetLocalizedEnumAsync(vendor.VendorType, languageId),
                    ActivatedServicesName = await _partnerAgreementService.GetServicesNameAsync(vendor.ActivatedServices, languageId),
                    CustomerId = vendor.ApplicationCustomerId
                };

                if (vendor.ApplicationSubmittedOnUtc.HasValue)
                    model.SubmittedOnUtc = await _dateTimeHelper.ConvertToUserTimeAsync(vendor.ApplicationSubmittedOnUtc.Value, DateTimeKind.Utc);

                if (vendor.ApplicationReviewedOnUtc.HasValue)
                    model.ReviewedOnUtc = await _dateTimeHelper.ConvertToUserTimeAsync(vendor.ApplicationReviewedOnUtc.Value, DateTimeKind.Utc);

                return model;
            });
        });
    }

    /// <summary>
    /// Prepares the review screen of one partner request
    /// </summary>
    public virtual async Task<PartnerRequestModel> PreparePartnerRequestModelAsync(PartnerRequestModel model, Vendor vendor)
    {
        ArgumentNullException.ThrowIfNull(vendor);

        var languageId = (await _workContext.GetWorkingLanguageAsync()).Id;

        model ??= new PartnerRequestModel();

        model.Id = vendor.Id;
        model.Name = vendor.Name;
        model.Email = vendor.Email;
        model.Description = vendor.Description;
        model.StatusId = vendor.ApplicationStatusId;
        model.StatusName = await GetStatusNameAsync(vendor.ApplicationStatus, languageId);
        model.VendorTypeName = await _localizationService.GetLocalizedEnumAsync(vendor.VendorType, languageId);
        model.ActivatedServicesName = await _partnerAgreementService.GetServicesNameAsync(vendor.ActivatedServices, languageId);
        model.RejectionReason = vendor.ApplicationRejectionReason;
        model.CanReview = vendor.ApplicationStatus == PartnerApplicationStatus.Pending;

        model.NationalId = vendor.NationalId;
        model.CommercialRegistrationNumber = vendor.CommercialRegistrationNumber;
        model.VatNumber = vendor.VatNumber;
        model.AuthorizedSignatoryName = vendor.AuthorizedSignatoryName;
        model.AuthorizedSignatoryCapacity = vendor.AuthorizedSignatoryCapacity;

        model.AgreementVersion = vendor.AgreementVersion;
        model.AgreementAcceptedIp = vendor.AgreementAcceptedIp;
        model.AgreementAcceptanceMethod = vendor.AgreementAcceptanceMethod;
        model.AgreementTechnicalRef = vendor.AgreementTechnicalRef;
        model.AgreementBodyHash = vendor.AgreementBodyHash;
        model.HasAgreementBody = !string.IsNullOrEmpty(vendor.AgreementBody);
        model.AgreementDocumentName = vendor.AgreementDocumentKey switch
        {
            PartnerAgreementDocument.Corporate =>
                await _localizationService.GetResourceAsync("Admin.Vendors.Fields.AgreementDocument.Corporate", languageId),
            PartnerAgreementDocument.Individual =>
                await _localizationService.GetResourceAsync("Admin.Vendors.Fields.AgreementDocument.Individual", languageId),
            _ => null
        };

        if (vendor.ApplicationSubmittedOnUtc.HasValue)
            model.SubmittedOnUtc = await _dateTimeHelper.ConvertToUserTimeAsync(vendor.ApplicationSubmittedOnUtc.Value, DateTimeKind.Utc);

        if (vendor.ApplicationReviewedOnUtc.HasValue)
            model.ReviewedOnUtc = await _dateTimeHelper.ConvertToUserTimeAsync(vendor.ApplicationReviewedOnUtc.Value, DateTimeKind.Utc);

        if (vendor.AgreementAcceptedOnUtc.HasValue)
            model.AgreementAcceptedOnUtc = await _dateTimeHelper.ConvertToUserTimeAsync(vendor.AgreementAcceptedOnUtc.Value, DateTimeKind.Utc);

        //the applicant's account, found through the column that survives a rejection releasing them
        var customer = vendor.ApplicationCustomerId > 0
            ? await _customerService.GetCustomerByIdAsync(vendor.ApplicationCustomerId)
            : (await _customerService.GetAllCustomersAsync(vendorId: vendor.Id)).FirstOrDefault();

        if (customer != null)
        {
            model.CustomerId = customer.Id;
            model.CustomerEmail = customer.Email;
        }

        return model;
    }

    #endregion
}
