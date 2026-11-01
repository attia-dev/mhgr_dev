// [Mahjar contracts]
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Mahjar;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Mahjar.Contracts;
using Nop.Services.Mahjar.Handover;
using Nop.Services.Mahjar.ItemStatus;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Web.Models.Contract;

namespace Nop.Web.Factories;

/// <summary>Storefront (customer) contract-request model factory. [Mahjar contracts]</summary>
public partial class ContractModelFactory : IContractModelFactory
{
    #region Fields

    protected readonly CurrencySettings _currencySettings;
    protected readonly IConditionCheckService _conditionCheckService; //[Mahjar handover]
    protected readonly IContractService _contractService;
    protected readonly ICurrencyService _currencyService;
    protected readonly IDateTimeHelper _dateTimeHelper;
    protected readonly ILocalizationService _localizationService;
    protected readonly IPictureService _pictureService; //[Mahjar handover]
    protected readonly IPriceFormatter _priceFormatter;
    protected readonly IProductService _productService; //[Mahjar handover]
    protected readonly IShoppingCartService _shoppingCartService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;
    protected readonly MediaSettings _mediaSettings; //[Mahjar handover]

    #endregion

    #region Ctor

    public ContractModelFactory(CurrencySettings currencySettings,
        IConditionCheckService conditionCheckService,
        IContractService contractService,
        ICurrencyService currencyService,
        IDateTimeHelper dateTimeHelper,
        ILocalizationService localizationService,
        IPictureService pictureService,
        IPriceFormatter priceFormatter,
        IProductService productService,
        IShoppingCartService shoppingCartService,
        IStoreContext storeContext,
        IWorkContext workContext,
        MediaSettings mediaSettings)
    {
        _currencySettings = currencySettings;
        _conditionCheckService = conditionCheckService;
        _contractService = contractService;
        _currencyService = currencyService;
        _dateTimeHelper = dateTimeHelper;
        _localizationService = localizationService;
        _pictureService = pictureService;
        _priceFormatter = priceFormatter;
        _productService = productService;
        _shoppingCartService = shoppingCartService;
        _storeContext = storeContext;
        _workContext = workContext;
        _mediaSettings = mediaSettings;
    }

    #endregion

    #region Methods

    public virtual async Task<ContractListModel> PrepareContractListModelAsync()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var language = await _workContext.GetWorkingLanguageAsync();
        var isArabic = language.UniqueSeoCode?.Equals("ar", StringComparison.InvariantCultureIgnoreCase) ?? false;
        var culture = CultureInfo.GetCultureInfo(language.LanguageCulture);
        var primaryCurrency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
        var daysFormat = await _localizationService.GetResourceAsync("mahjar.contracts.snapshot.days");

        //an Approved request only really unlocks checkout while its frozen hash still matches the
        //customer's CURRENT cart — computed once, it is the same answer for every row
        var currentCart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
        var checkoutAllowed = await _contractService.IsCheckoutAllowedAsync(customer, currentCart, store.Id);

        var model = new ContractListModel();
        var requests = await _contractService.SearchRequestsAsync(customerId: customer.Id, storeId: store.Id, pageSize: 50);
        foreach (var request in requests)
        {
            var item = new ContractItemModel
            {
                Id = request.Id,
                Number = $"MHGR-C-{request.Id:D5}",
                CreatedOn = (await _dateTimeHelper.ConvertToUserTimeAsync(request.CreatedOnUtc, DateTimeKind.Utc)).ToString("g"),
                Status = await _localizationService.GetResourceAsync(
                    $"mahjar.contracts.status.{request.Status.ToString().ToLowerInvariant()}"),
                StatusClass = $"contract-status-{request.Status.ToString().ToLowerInvariant()}",
                CanSign = request.Status == ContractRequestStatus.ContractSent,
                Token = request.Token,
                CanCancel = request.Status == ContractRequestStatus.Pending || request.Status == ContractRequestStatus.ContractSent,
                CanCheckout = request.Status == ContractRequestStatus.Approved && checkoutAllowed,
                //[Mahjar handover] once a document exists the customer can always read it back, with its condition checks
                CanView = !string.IsNullOrEmpty(request.ContractBody),
                RejectionReason = request.RejectionReason,
                SignedOn = request.SignedOnUtc.HasValue
                    ? (await _dateTimeHelper.ConvertToUserTimeAsync(request.SignedOnUtc.Value, DateTimeKind.Utc)).ToString("g")
                    : null
            };

            var snapshot = await _contractService.DeserializeSnapshotAsync(request);
            if (snapshot != null)
            {
                item.GrandTotal = await _priceFormatter.FormatPriceAsync(snapshot.GrandTotal, true, primaryCurrency);
                foreach (var line in snapshot.Lines)
                {
                    var name = isArabic && !string.IsNullOrEmpty(line.ProductNameAr) ? line.ProductNameAr : line.ProductName;
                    var period = string.Empty;
                    if (line.IsRental && line.RentalStartUtc.HasValue && line.RentalEndUtc.HasValue)
                    {
                        period = string.Format("{0} – {1} ({2})",
                            line.RentalStartUtc.Value.ToString("d", culture),
                            line.RentalEndUtc.Value.ToString("d", culture),
                            string.Format(daysFormat, line.RentalDays));
                    }

                    item.Lines.Add(new ContractItemLineModel
                    {
                        Name = name,
                        IsRental = line.IsRental,
                        Quantity = line.Quantity,
                        Period = period
                    });
                }
            }

            model.Items.Add(item);
        }

        return model;
    }

    public virtual async Task<ContractSignModel> PrepareContractSignModelAsync(ContractRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ContractSignModel
        {
            Id = request.Id,
            Number = $"MHGR-C-{request.Id:D5}",
            Token = request.Token,
            BodyHtml = request.ContractBody,
            SignedName = request.SignedName,
            //[Mahjar handover]
            ConditionChecks = await PrepareConditionChecksAsync(request)
        };
    }

    /// <summary>[Mahjar handover] The read-only contract page.</summary>
    public virtual async Task<ContractViewModel> PrepareContractViewModelAsync(ContractRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ContractViewModel
        {
            Id = request.Id,
            Number = $"MHGR-C-{request.Id:D5}",
            Status = await _localizationService.GetResourceAsync($"mahjar.contracts.status.{request.Status.ToString().ToLowerInvariant()}"),
            BodyHtml = request.ContractBody,
            SignedName = request.SignedName,
            SignedOn = request.SignedOnUtc.HasValue
                ? (await _dateTimeHelper.ConvertToUserTimeAsync(request.SignedOnUtc.Value, DateTimeKind.Utc)).ToString("g")
                : null,
            SignatureDataUrl = request.SignatureDataUrl,
            ConditionChecks = await PrepareConditionChecksAsync(request)
        };
    }

    /// <summary>
    /// [Mahjar handover] The condition checks a customer may see: the before-signing checks (locked when the
    /// contract was sent) and the delivery / return checks of the order — confirmed ones only.
    /// </summary>
    public virtual async Task<IList<ContractConditionCheckModel>> PrepareConditionChecksAsync(ContractRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var primaryCurrency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
        var result = new List<ContractConditionCheckModel>();

        foreach (var check in await _conditionCheckService.GetCustomerVisibleChecksAsync(request))
        {
            var typeKey = check.CheckType.ToString().ToLowerInvariant();
            var product = await _productService.GetProductByIdAsync(check.ProductId);

            var model = new ContractConditionCheckModel
            {
                Id = check.Id,
                CheckTypeKey = typeKey,
                CheckType = await _localizationService.GetResourceAsync($"mahjar.contracts.condition.type.{typeKey}"),
                ProductName = product == null ? string.Empty : await _localizationService.GetLocalizedAsync(product, p => p.Name),
                RecordedOn = (await _dateTimeHelper.ConvertToUserTimeAsync(check.ConfirmedOnUtc ?? check.UpdatedOnUtc, DateTimeKind.Utc)).ToString("g"),
                GradeText = check.ConditionGrade == ItemConditionGrade.NotSpecified
                    ? null
                    : await _localizationService.GetLocalizedEnumAsync(check.ConditionGrade),
                Notes = check.ConditionNotes,
                HasDamage = check.HasDamage && !string.IsNullOrWhiteSpace(check.DamageDescription),
                DamageSeverityText = check.HasDamage && check.DamageSeverityId > 0
                    ? await _localizationService.GetLocalizedEnumAsync(check.DamageSeverity)
                    : null,
                DamageDescription = check.DamageDescription
            };

            if (check.CheckType == ConditionCheckType.Return)
            {
                if (check.DamageDeduction > decimal.Zero)
                    model.DeductionText = await _priceFormatter.FormatPriceAsync(check.DamageDeduction, true, primaryCurrency);
                model.RefundText = await _priceFormatter.FormatPriceAsync(
                    Math.Max(check.DepositAmount - check.DamageDeduction, decimal.Zero), true, primaryCurrency);
            }

            //an uploaded file we host wins over an external URL, which is re-validated on the way out
            if (!string.IsNullOrEmpty(check.VideoFile))
            {
                model.VideoUrl = check.VideoFile;
                model.VideoIsFile = true;
            }
            else if (ItemStatusVideoUrlHelper.IsAllowed(check.VideoUrl))
                model.VideoUrl = check.VideoUrl.Trim();

            foreach (var picture in await _conditionCheckService.GetPicturesAsync(check.Id))
            {
                model.Photos.Add(new ContractConditionPhotoModel
                {
                    ImageUrl = await _pictureService.GetPictureUrlAsync(picture.PictureId, _mediaSettings.ProductDetailsPictureSize),
                    FullSizeImageUrl = await _pictureService.GetPictureUrlAsync(picture.PictureId),
                    Caption = picture.Caption
                });
            }

            result.Add(model);
        }

        return result;
    }

    #endregion
}
