// [Mahjar handover]
using System.Globalization;
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
using Nop.Services.Mahjar.Handover;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Vendors;
using Nop.Web.Areas.Admin.Models.Mahjar;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Web.Areas.Admin.Factories;

/// <summary>
/// Represents the handover desk model factory implementation (admin area) [Mahjar handover]
/// </summary>
public partial class HandoverModelFactory : IHandoverModelFactory
{
    #region Fields

    protected readonly CurrencySettings _currencySettings;
    protected readonly IConditionCheckService _conditionCheckService;
    protected readonly IContractService _contractService;
    protected readonly ICurrencyService _currencyService;
    protected readonly ICustomerService _customerService;
    protected readonly IDateTimeHelper _dateTimeHelper;
    protected readonly IHandoverAssignmentService _assignmentService; //[Mahjar handover]
    protected readonly ILocalizationService _localizationService;
    protected readonly IOrderService _orderService;
    protected readonly IPictureService _pictureService;
    protected readonly IPriceFormatter _priceFormatter;
    protected readonly IProductService _productService;
    protected readonly IVendorService _vendorService;
    protected readonly IWorkContext _workContext; //[Mahjar handover]
    protected readonly ItemStatusSettings _itemStatusSettings;

    #endregion

    #region Ctor

    public HandoverModelFactory(CurrencySettings currencySettings,
        IConditionCheckService conditionCheckService,
        IContractService contractService,
        ICurrencyService currencyService,
        ICustomerService customerService,
        IDateTimeHelper dateTimeHelper,
        IHandoverAssignmentService assignmentService,
        ILocalizationService localizationService,
        IOrderService orderService,
        IPictureService pictureService,
        IPriceFormatter priceFormatter,
        IProductService productService,
        IVendorService vendorService,
        IWorkContext workContext,
        ItemStatusSettings itemStatusSettings)
    {
        _currencySettings = currencySettings;
        _conditionCheckService = conditionCheckService;
        _contractService = contractService;
        _currencyService = currencyService;
        _customerService = customerService;
        _dateTimeHelper = dateTimeHelper;
        _assignmentService = assignmentService;
        _localizationService = localizationService;
        _orderService = orderService;
        _pictureService = pictureService;
        _priceFormatter = priceFormatter;
        _productService = productService;
        _vendorService = vendorService;
        _workContext = workContext;
        _itemStatusSettings = itemStatusSettings;
    }

    #endregion

    #region Utilities

    protected virtual Task<string> ResourceAsync(string key)
    {
        return _localizationService.GetResourceAsync(key);
    }

    protected virtual Task<string> GetTypeTextAsync(ConditionCheckType type)
    {
        return ResourceAsync($"admin.mahjar.handover.type.{type.ToString().ToLowerInvariant()}");
    }

    protected virtual Task<string> GetStatusTextAsync(ConditionCheck check)
    {
        return ResourceAsync(check == null
            ? "admin.mahjar.handover.status.notstarted"
            : check.Status == ConditionCheckStatus.Confirmed ? "admin.mahjar.handover.status.confirmed" : "admin.mahjar.handover.status.draft");
    }

    /// <summary>"3 photo(s) + video", "No media", or empty for a check not started.</summary>
    protected virtual async Task<(string Text, bool HasMedia)> GetMediaSummaryAsync(ConditionCheck check)
    {
        if (check == null)
            return (string.Empty, false);

        var photoCount = (await _conditionCheckService.GetPicturesAsync(check.Id)).Count;
        var hasVideo = !string.IsNullOrEmpty(check.VideoFile) || !string.IsNullOrEmpty(check.VideoUrl);
        if (photoCount == 0 && !hasVideo)
            return (await ResourceAsync("admin.mahjar.handover.nomedia"), false);

        var parts = new List<string>();
        if (photoCount > 0)
            parts.Add(string.Format(await ResourceAsync("admin.mahjar.handover.photosfmt"), photoCount));
        if (hasVideo)
            parts.Add(await ResourceAsync("admin.mahjar.handover.withvideo"));

        return (string.Join(" + ", parts), true);
    }

    protected virtual async Task<string> GetProductNameAsync(int productId)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        return product == null ? $"#{productId}" : await _localizationService.GetLocalizedAsync(product, p => p.Name);
    }

    protected virtual async Task<string> GetCustomerInfoAsync(int customerId)
    {
        if (customerId <= 0)
            return string.Empty;

        var customer = await _customerService.GetCustomerByIdAsync(customerId);
        if (customer == null)
            return string.Empty;

        var fullName = await _customerService.GetCustomerFullNameAsync(customer);
        return string.IsNullOrWhiteSpace(fullName) ? customer.Email : $"{fullName} ({customer.Email})";
    }

    protected static string FormatPeriod(DateTime? startUtc, DateTime? endUtc)
    {
        if (!startUtc.HasValue || !endUtc.HasValue)
            return string.Empty;

        return $"{startUtc.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} - {endUtc.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
    }

    protected static string GetContractNumber(int contractRequestId)
    {
        return contractRequestId > 0 ? $"MHGR-C-{contractRequestId:D5}" : null;
    }

    protected virtual async Task<string> GetReferenceAsync(int contractRequestId, string orderNumber)
    {
        var parts = new List<string>();
        if (contractRequestId > 0)
            parts.Add(GetContractNumber(contractRequestId));
        if (!string.IsNullOrEmpty(orderNumber))
            parts.Add(string.Format(await ResourceAsync("admin.mahjar.handover.order"), orderNumber));

        return string.Join(" · ", parts);
    }

    protected virtual async Task<string> FormatAmountAsync(decimal amount)
    {
        var currency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
        return await _priceFormatter.FormatPriceAsync(amount, true, currency);
    }

    /// <summary>
    /// [Mahjar handover] Turns what the desk typed into something the queue can match on. The catalogue search knows
    /// localized item names and SKUs, and the customer search knows names — which live in generic attributes rather
    /// than columns — so both halves are resolved to ids here and the queue itself stays a plain join. Both lookups
    /// are capped: a keyword like "a" must not drag the whole catalogue into an IN clause.
    /// </summary>
    protected virtual async Task<HandoverTaskFilter> BuildFilterAsync(HandoverSearchModel searchModel)
    {
        const int MAX_MATCHES = 500;

        var filter = new HandoverTaskFilter
        {
            AssignedCustomerId = searchModel.SearchAssignedCustomerId,
            StatusId = searchModel.SearchStatusId,
            OverdueOnly = searchModel.SearchOverdueOnly
        };

        //the desk picks days in its own time zone; the queue stores instants
        var timeZone = await _dateTimeHelper.GetCurrentTimeZoneAsync();
        if (searchModel.SearchStartDate.HasValue)
            filter.FromUtc = _dateTimeHelper.ConvertToUtcTime(searchModel.SearchStartDate.Value, timeZone);
        if (searchModel.SearchEndDate.HasValue)
            filter.ToUtc = _dateTimeHelper.ConvertToUtcTime(searchModel.SearchEndDate.Value.AddDays(1), timeZone);

        var keyword = searchModel.SearchKeyword?.Trim();
        if (string.IsNullOrEmpty(keyword))
            return filter;

        filter.Keyword = keyword;

        //The queue shows every item's name in the reader's own language, so the search has to look at THAT name.
        //Without a language id the stock catalogue search skips its localized-name union altogether and matches only
        //the base name — on this Arabic-first store that means the one name the desk can see is the one name it
        //could never find. (The SKU half stays an exact match, exactly as the stock product search behaves.)
        var languageId = (await _workContext.GetWorkingLanguageAsync()).Id;
        var products = await _productService.SearchProductsAsync(pageIndex: 0, pageSize: MAX_MATCHES,
            keywords: keyword, searchSku: true, showHidden: true, languageId: languageId);
        filter.KeywordProductIds = products.Select(product => product.Id).ToList();

        var customerIds = new HashSet<int>();
        foreach (var matches in new[]
                 {
                     await _customerService.GetAllCustomersAsync(email: keyword, pageSize: MAX_MATCHES),
                     await _customerService.GetAllCustomersAsync(username: keyword, pageSize: MAX_MATCHES),
                     await _customerService.GetAllCustomersAsync(firstName: keyword, pageSize: MAX_MATCHES),
                     await _customerService.GetAllCustomersAsync(lastName: keyword, pageSize: MAX_MATCHES)
                 })
        {
            foreach (var customer in matches)
                customerIds.Add(customer.Id);
        }

        //Each probe above tests the WHOLE keyword against ONE column, so "محمد العتيبي" matches neither the first
        //name nor the last — yet the queue's customer column shows precisely that "First Last" string. Probe the two
        //ends of a multi-word keyword as a pair too (one call: the two arguments are AND-ed), splitting on any
        //whitespace so the non-breaking spaces that come out of an RTL grid are handled as well.
        var nameParts = keyword.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        if (nameParts.Length > 1)
        {
            foreach (var customer in await _customerService.GetAllCustomersAsync(
                         firstName: nameParts[0], lastName: nameParts[^1], pageSize: MAX_MATCHES))
            {
                customerIds.Add(customer.Id);
            }
        }

        filter.KeywordCustomerIds = customerIds.ToList();

        //"MHGR-C-01069", "C-01069" and plain digits all name the same contract
        var digits = new string(keyword.Where(char.IsDigit).ToArray());
        if (digits.Length is > 0 and <= 9 && int.TryParse(digits, out var contractRequestId))
            filter.KeywordContractRequestId = contractRequestId;

        return filter;
    }

    #endregion

    #region Methods

    public virtual async Task<HandoverSearchModel> PrepareHandoverSearchModelAsync(HandoverSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        foreach (var stage in Enum.GetValues<HandoverStage>())
        {
            searchModel.AvailableStages.Add(new SelectListItem
            {
                Value = ((int)stage).ToString(),
                Text = await ResourceAsync($"admin.mahjar.handover.stage.{stage.ToString().ToLowerInvariant()}")
            });
        }

        if (!Enum.IsDefined(typeof(HandoverStage), searchModel.StageId))
            searchModel.StageId = (int)HandoverStage.AwaitingPreContract;

        //[Mahjar handover] whose work to show: anyone, the orders nobody owns yet, or one employee of the desk
        searchModel.AvailableAssignees.Add(new SelectListItem
        {
            Value = "0",
            Text = await ResourceAsync("admin.mahjar.handover.list.assignee.all")
        });
        searchModel.AvailableAssignees.Add(new SelectListItem
        {
            Value = HandoverTaskFilter.UNASSIGNED.ToString(),
            Text = await ResourceAsync("admin.mahjar.handover.assign.none")
        });
        foreach (var employee in await _assignmentService.GetAssignableStaffAsync())
        {
            searchModel.AvailableAssignees.Add(new SelectListItem
            {
                Value = employee.Id.ToString(),
                Text = await GetCustomerInfoAsync(employee.Id)
            });
        }

        //[Mahjar handover] how far along the check is
        searchModel.AvailableStatuses.Add(new SelectListItem
        {
            Value = "0",
            Text = await ResourceAsync("admin.mahjar.handover.list.status.all")
        });
        searchModel.AvailableStatuses.Add(new SelectListItem
        {
            Value = HandoverTaskFilter.NOT_STARTED.ToString(),
            Text = await ResourceAsync("admin.mahjar.handover.status.notstarted")
        });
        searchModel.AvailableStatuses.Add(new SelectListItem
        {
            Value = ((int)ConditionCheckStatus.Draft).ToString(),
            Text = await ResourceAsync("admin.mahjar.handover.status.draft")
        });
        searchModel.AvailableStatuses.Add(new SelectListItem
        {
            Value = ((int)ConditionCheckStatus.Confirmed).ToString(),
            Text = await ResourceAsync("admin.mahjar.handover.status.confirmed")
        });

        searchModel.SetGridPageSize();

        return searchModel;
    }

    public virtual async Task<HandoverTaskListModel> PrepareHandoverTaskListModelAsync(HandoverSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        var stage = Enum.IsDefined(typeof(HandoverStage), searchModel.StageId)
            ? (HandoverStage)searchModel.StageId
            : HandoverStage.AwaitingPreContract;

        var filter = await BuildFilterAsync(searchModel);
        var tasks = await _conditionCheckService.SearchTasksAsync(stage, filter, searchModel.Page - 1, searchModel.PageSize);
        var today = DateTime.UtcNow.Date;

        var model = await new HandoverTaskListModel().PrepareToGridAsync(searchModel, tasks, () =>
        {
            return tasks.SelectAwait(async task =>
            {
                var (media, _) = await GetMediaSummaryAsync(task.Check);
                return new HandoverTaskModel
                {
                    CheckId = task.Check?.Id ?? 0,
                    CheckTypeId = (int)task.CheckType,
                    CheckType = await GetTypeTextAsync(task.CheckType),
                    ProductId = task.ProductId,
                    ProductName = await GetProductNameAsync(task.ProductId),
                    ContractRequestId = task.ContractRequestId,
                    OrderId = task.OrderId,
                    OrderItemId = task.OrderItemId,
                    Reference = await GetReferenceAsync(task.ContractRequestId, task.OrderNumber),
                    CustomerId = task.CustomerId,
                    CustomerInfo = await GetCustomerInfoAsync(task.CustomerId),
                    //[Mahjar handover] who owns this order's handover
                    AssignedCustomerId = task.AssignedCustomerId,
                    AssigneeInfo = task.AssignedCustomerId > 0
                        ? await GetCustomerInfoAsync(task.AssignedCustomerId)
                        : await _localizationService.GetResourceAsync("admin.mahjar.handover.assign.none"),
                    Period = FormatPeriod(task.RentalStartUtc, task.RentalEndUtc),
                    IsOverdue = stage == HandoverStage.AwaitingReturn && task.RentalEndUtc.HasValue && task.RentalEndUtc.Value.Date < today,
                    Status = await GetStatusTextAsync(task.Check),
                    Media = media,
                    Date = await _dateTimeHelper.ConvertToUserTimeAsync(task.DateUtc, DateTimeKind.Utc)
                };
            });
        });

        return model;
    }

    public virtual async Task<ConditionCheckModel> PrepareConditionCheckModelAsync(ConditionCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);

        var isEditable = _conditionCheckService.IsEditable(check);
        var model = new ConditionCheckModel
        {
            Id = check.Id,
            CheckTypeId = check.CheckTypeId,
            CheckType = await GetTypeTextAsync(check.CheckType),
            StatusId = check.StatusId,
            Status = await GetStatusTextAsync(check),
            IsEditable = isEditable,
            ProductId = check.ProductId,
            ContractRequestId = check.ContractRequestId,
            ContractNumber = GetContractNumber(check.ContractRequestId),
            OrderId = check.OrderId,
            OrderItemId = check.OrderItemId,
            ConditionGradeId = check.ConditionGradeId,
            ConditionNotes = check.ConditionNotes,
            HasDamage = check.HasDamage,
            DamageSeverityId = check.DamageSeverityId > 0 ? check.DamageSeverityId : (int)ItemStatusNoteSeverity.Minor,
            DamageDescription = check.DamageDescription,
            InternalComment = check.InternalComment,
            DamageDeduction = check.DamageDeduction,
            VideoFile = check.VideoFile,
            VideoUrl = check.VideoUrl,
            MaxVideoSizeMb = _itemStatusSettings.MaxVideoSizeMb,
            AllowedVideoExtensions = _conditionCheckService.GetAllowedVideoExtensions(),
            CanConfirm = isEditable && check.CheckType != ConditionCheckType.PreContract,
            CanImportFromProduct = isEditable && check.CheckType is ConditionCheckType.PreContract or ConditionCheckType.Delivery,
            Hint = await ResourceAsync($"admin.mahjar.handover.{check.CheckType.ToString().ToLowerInvariant()}.hint"),
            ConfirmButtonText = check.CheckType switch
            {
                ConditionCheckType.Intake => await ResourceAsync("admin.mahjar.handover.buttons.confirmintake"),
                ConditionCheckType.Delivery => await ResourceAsync("admin.mahjar.handover.buttons.confirmdelivery"),
                ConditionCheckType.Return => await ResourceAsync("admin.mahjar.handover.buttons.confirmreturn"),
                _ => null
            }
        };

        //the item
        var product = await _productService.GetProductByIdAsync(check.ProductId);
        model.ProductName = product == null ? $"#{check.ProductId}" : await _localizationService.GetLocalizedAsync(product, p => p.Name);
        model.ProductSku = product?.Sku;
        if (product is { VendorId: > 0 })
            model.VendorName = (await _vendorService.GetVendorByIdAsync(product.VendorId))?.Name;

        //the customer, the order / contract and the rental period
        var order = check.OrderId > 0 ? await _orderService.GetOrderByIdAsync(check.OrderId) : null;
        var orderItem = check.OrderItemId > 0 ? await _orderService.GetOrderItemByIdAsync(check.OrderItemId) : null;
        model.OrderNumber = order?.CustomOrderNumber;
        model.Reference = await GetReferenceAsync(check.ContractRequestId, model.OrderNumber);

        var customerId = order?.CustomerId ?? 0;
        if (customerId == 0 && check.ContractRequestId > 0)
            customerId = (await _contractService.GetRequestByIdAsync(check.ContractRequestId))?.CustomerId ?? 0;
        model.CustomerId = customerId;
        model.CustomerInfo = await GetCustomerInfoAsync(customerId);
        model.Period = FormatPeriod(orderItem?.RentalStartDateUtc, orderItem?.RentalEndDateUtc);

        //the deposit of a rental return (frozen once confirmed)
        if (check.CheckType == ConditionCheckType.Return && orderItem != null)
        {
            var deposit = isEditable ? await _conditionCheckService.GetRentalDepositAsync(orderItem) : check.DepositAmount;
            model.DepositText = await FormatAmountAsync(deposit);
            model.RefundText = await FormatAmountAsync(Math.Max(deposit - check.DamageDeduction, decimal.Zero));
        }

        //who and when
        model.CreatedBy = await GetCustomerInfoAsync(check.CreatedByCustomerId);
        model.CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(check.CreatedOnUtc, DateTimeKind.Utc);
        if (check.ConfirmedOnUtc.HasValue)
        {
            model.ConfirmedBy = await GetCustomerInfoAsync(check.ConfirmedByCustomerId);
            model.ConfirmedOn = await _dateTimeHelper.ConvertToUserTimeAsync(check.ConfirmedOnUtc.Value, DateTimeKind.Utc);
        }

        //media
        model.PictureSearchModel.ConditionCheckId = check.Id;
        model.PictureSearchModel.IsEditable = isEditable;
        model.PictureSearchModel.SetGridPageSize();
        model.AddPictureModel.ConditionCheckId = check.Id;
        foreach (var picture in await _conditionCheckService.GetPicturesAsync(check.Id))
        {
            model.Pictures.Add(new ConditionCheckPictureModel
            {
                Id = picture.Id,
                ConditionCheckId = picture.ConditionCheckId,
                PictureId = picture.PictureId,
                PictureUrl = await _pictureService.GetPictureUrlAsync(picture.PictureId, 200),
                FullSizeUrl = await _pictureService.GetPictureUrlAsync(picture.PictureId),
                Caption = picture.Caption,
                DisplayOrder = picture.DisplayOrder
            });
        }

        return model;
    }

    public virtual async Task<ConditionCheckPictureListModel> PrepareConditionCheckPictureListModelAsync(ConditionCheckPictureSearchModel searchModel,
        ConditionCheck check)
    {
        ArgumentNullException.ThrowIfNull(searchModel);
        ArgumentNullException.ThrowIfNull(check);

        var pictures = (await _conditionCheckService.GetPicturesAsync(check.Id)).ToPagedList(searchModel);

        var model = await new ConditionCheckPictureListModel().PrepareToGridAsync(searchModel, pictures, () =>
        {
            return pictures.SelectAwait(async picture => new ConditionCheckPictureModel
            {
                Id = picture.Id,
                ConditionCheckId = picture.ConditionCheckId,
                PictureId = picture.PictureId,
                PictureUrl = await _pictureService.GetPictureUrlAsync(picture.PictureId, 200),
                FullSizeUrl = await _pictureService.GetPictureUrlAsync(picture.PictureId),
                Caption = picture.Caption,
                DisplayOrder = picture.DisplayOrder
            });
        });

        return model;
    }

    public virtual async Task<(IList<ContractConditionCheckRowModel> Rows, bool PreContractReady)> PrepareContractConditionChecksAsync(ContractRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var rows = new List<ContractConditionCheckRowModel>();
        var preContractReady = true;
        var isPending = request.Status == ContractRequestStatus.Pending;

        //before signing: one row per item of the frozen cart, started or not
        var preContractChecks = (await _conditionCheckService.GetChecksByContractRequestIdAsync(request.Id))
            .Where(check => check.CheckType == ConditionCheckType.PreContract)
            .ToList();
        var snapshot = await _contractService.DeserializeSnapshotAsync(request);
        foreach (var productId in snapshot.Lines.Select(line => line.ProductId).Distinct())
        {
            var check = preContractChecks.Where(c => c.ProductId == productId).OrderByDescending(c => c.Id).FirstOrDefault();
            var (media, hasMedia) = await GetMediaSummaryAsync(check);
            if (!hasMedia)
                preContractReady = false;

            rows.Add(new ContractConditionCheckRowModel
            {
                CheckId = check?.Id ?? 0,
                CheckTypeId = (int)ConditionCheckType.PreContract,
                CheckType = await GetTypeTextAsync(ConditionCheckType.PreContract),
                ProductId = productId,
                ProductName = await GetProductNameAsync(productId),
                Status = await GetStatusTextAsync(check),
                Media = hasMedia ? media : await ResourceAsync("admin.mahjar.handover.precontract.missingshort"),
                HasMedia = hasMedia,
                Date = check?.ConfirmedOnUtc.HasValue == true
                    ? await _dateTimeHelper.ConvertToUserTimeAsync(check.ConfirmedOnUtc.Value, DateTimeKind.Utc)
                    : null,
                CanStart = check == null && isPending
            });
        }

        //after payment: the delivery / return checks of the order placed against the contract
        foreach (var check in (await _conditionCheckService.GetChecksByOrderIdAsync(request.OrderId))
                     .Where(c => c.CheckType is ConditionCheckType.Delivery or ConditionCheckType.Return))
        {
            var (media, hasMedia) = await GetMediaSummaryAsync(check);
            rows.Add(new ContractConditionCheckRowModel
            {
                CheckId = check.Id,
                CheckTypeId = check.CheckTypeId,
                CheckType = await GetTypeTextAsync(check.CheckType),
                ProductId = check.ProductId,
                ProductName = await GetProductNameAsync(check.ProductId),
                Status = await GetStatusTextAsync(check),
                Media = media,
                HasMedia = hasMedia,
                Date = check.ConfirmedOnUtc.HasValue
                    ? await _dateTimeHelper.ConvertToUserTimeAsync(check.ConfirmedOnUtc.Value, DateTimeKind.Utc)
                    : null
            });
        }

        return (rows, preContractReady);
    }

    #endregion
}
