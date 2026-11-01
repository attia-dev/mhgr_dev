// [Mahjar contracts]
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Mahjar;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Stores;
using Nop.Core.Events;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Mahjar.Handover;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Stores;

namespace Nop.Services.Mahjar.Contracts;

/// <summary>
/// The contract-approval stage service implementation. Freezes carts into <see cref="ContractRequest"/>
/// snapshots (primary store currency), renders the localized tokenized contract template, guards every
/// status transition and publishes the matching contract event. [Mahjar contracts]
/// </summary>
public partial class ContractService : IContractService
{
    #region Fields

    protected readonly ContractSettings _contractSettings;
    protected readonly CurrencySettings _currencySettings;
    protected readonly IConditionCheckService _conditionCheckService; //[Mahjar handover]
    protected readonly ICurrencyService _currencyService;
    protected readonly ICustomerService _customerService;
    protected readonly IDateTimeHelper _dateTimeHelper;
    protected readonly IEventPublisher _eventPublisher;
    protected readonly ILanguageService _languageService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IPriceFormatter _priceFormatter;
    protected readonly IProductAttributeFormatter _productAttributeFormatter;
    protected readonly IProductService _productService;
    protected readonly IRepository<ContractRequest> _contractRequestRepository;
    protected readonly IRepository<ContractTemplate> _contractTemplateRepository;
    protected readonly IShoppingCartService _shoppingCartService;
    protected readonly IStoreContext _storeContext;
    protected readonly IStoreService _storeService;
    protected readonly ITokenizer _tokenizer;
    protected readonly IWorkContext _workContext;

    /// <summary>The non-terminal statuses an "active" request can be in.</summary>
    protected static readonly int[] _activeStatusIds =
    {
        (int)ContractRequestStatus.Pending,
        (int)ContractRequestStatus.ContractSent,
        (int)ContractRequestStatus.Signed,
        (int)ContractRequestStatus.Approved
    };

    #endregion

    #region Ctor

    public ContractService(ContractSettings contractSettings,
        CurrencySettings currencySettings,
        IConditionCheckService conditionCheckService,
        ICurrencyService currencyService,
        ICustomerService customerService,
        IDateTimeHelper dateTimeHelper,
        IEventPublisher eventPublisher,
        ILanguageService languageService,
        ILocalizationService localizationService,
        IPriceFormatter priceFormatter,
        IProductAttributeFormatter productAttributeFormatter,
        IProductService productService,
        IRepository<ContractRequest> contractRequestRepository,
        IRepository<ContractTemplate> contractTemplateRepository,
        IShoppingCartService shoppingCartService,
        IStoreContext storeContext,
        IStoreService storeService,
        ITokenizer tokenizer,
        IWorkContext workContext)
    {
        _contractSettings = contractSettings;
        _currencySettings = currencySettings;
        _conditionCheckService = conditionCheckService;
        _currencyService = currencyService;
        _customerService = customerService;
        _dateTimeHelper = dateTimeHelper;
        _eventPublisher = eventPublisher;
        _languageService = languageService;
        _localizationService = localizationService;
        _priceFormatter = priceFormatter;
        _productAttributeFormatter = productAttributeFormatter;
        _productService = productService;
        _contractRequestRepository = contractRequestRepository;
        _contractTemplateRepository = contractTemplateRepository;
        _shoppingCartService = shoppingCartService;
        _storeContext = storeContext;
        _storeService = storeService;
        _tokenizer = tokenizer;
        _workContext = workContext;
    }

    #endregion

    #region Utilities

    /// <summary>The customer-facing contract number of a request.</summary>
    protected static string GetContractNumber(ContractRequest request)
    {
        return $"MHGR-C-{request.Id:D5}";
    }

    /// <summary>Whether a cart line is a rental (it carries both rental dates).</summary>
    protected static bool IsRentalLine(ShoppingCartItem shoppingCartItem)
    {
        return shoppingCartItem.RentalStartDateUtc.HasValue && shoppingCartItem.RentalEndDateUtc.HasValue;
    }

    /// <summary>Guards a status transition; throws the localized invalid-status error otherwise.</summary>
    protected virtual async Task EnsureStatusAsync(ContractRequest request, params ContractRequestStatus[] allowedStatuses)
    {
        if (!allowedStatuses.Contains(request.Status))
            throw new NopException(await _localizationService.GetResourceAsync("admin.mahjar.contracts.invalidstatus"));
    }

    /// <summary>The primary store currency (snapshot amounts are captured and formatted in it).</summary>
    protected virtual async Task<Currency> GetPrimaryStoreCurrencyAsync()
    {
        return await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
    }

    /// <summary>Whether the given language is Arabic.</summary>
    protected static bool IsArabic(Language language)
    {
        return language != null &&
               (string.Equals(language.UniqueSeoCode, "ar", StringComparison.InvariantCultureIgnoreCase) ||
                (language.LanguageCulture?.StartsWith("ar", StringComparison.InvariantCultureIgnoreCase) ?? false));
    }

    /// <summary>The culture of a language (invariant fallback), used for date formatting.</summary>
    protected static CultureInfo GetCulture(Language language)
    {
        try
        {
            return new CultureInfo(language?.LanguageCulture ?? string.Empty);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }

    /// <summary>Formats an amount in the primary store currency for the given language.</summary>
    protected virtual async Task<string> FormatAmountAsync(decimal amount, int languageId)
    {
        var primaryCurrency = await GetPrimaryStoreCurrencyAsync();
        return await _priceFormatter.FormatPriceAsync(amount, true, primaryCurrency, languageId, false);
    }

    /// <summary>
    /// Builds the inline-styled HTML items table rendered into the contract via %Contract.ItemsTable%.
    /// </summary>
    protected virtual async Task<string> BuildItemsTableAsync(ContractCartSnapshot snapshot, int languageId)
    {
        var language = await _languageService.GetLanguageByIdAsync(languageId);
        var isArabic = IsArabic(language);
        var culture = GetCulture(language);
        var dir = isArabic ? "rtl" : "ltr";
        var align = isArabic ? "right" : "left";

        var headers = new[]
        {
            "#",
            await _localizationService.GetResourceAsync("mahjar.contracts.snapshot.product", languageId),
            await _localizationService.GetResourceAsync("mahjar.contracts.snapshot.mode", languageId),
            await _localizationService.GetResourceAsync("mahjar.contracts.snapshot.qty", languageId),
            await _localizationService.GetResourceAsync("mahjar.contracts.snapshot.period", languageId),
            await _localizationService.GetResourceAsync("mahjar.contracts.snapshot.unitprice", languageId),
            await _localizationService.GetResourceAsync("mahjar.contracts.snapshot.deposit", languageId),
            await _localizationService.GetResourceAsync("mahjar.contracts.snapshot.linetotal", languageId)
        }.ToList();

        //[Mahjar contracts] the rental period and the security deposit are rental-only concepts: on a
        //purchase-only contract both columns would be an "—" down every row, so drop them entirely
        var hasRental = snapshot.Lines.Any(line => line.IsRental);
        const int periodColumn = 4;
        const int depositColumn = 6;
        if (!hasRental)
        {
            headers.RemoveAt(depositColumn);
            headers.RemoveAt(periodColumn);
        }

        var cellStyle = $"border:1px solid #d9d2c5;padding:8px;text-align:{align};vertical-align:top";
        var headStyle = $"{cellStyle};background-color:#f5f2ec;font-weight:bold";

        var sb = new StringBuilder();
        //[Mahjar contracts] class + data-label attributes let the mobile app restyle the table as stacked cards (inline styles keep web/email unchanged)
        sb.Append($"<table class=\"mhgr-contract-items\" dir=\"{dir}\" style=\"width:100%;border-collapse:collapse;font-size:14px\">");
        sb.Append("<thead><tr>");
        foreach (var header in headers)
            sb.Append($"<th style=\"{headStyle}\">{WebUtility.HtmlEncode(header)}</th>");
        sb.Append("</tr></thead><tbody>");

        var index = 0;
        foreach (var line in snapshot.Lines)
        {
            index++;

            var name = isArabic && !string.IsNullOrEmpty(line.ProductNameAr) ? line.ProductNameAr : line.ProductName;
            var item = WebUtility.HtmlEncode(name);
            if (!string.IsNullOrEmpty(line.AttributeDescription))
                item += $"<br /><small>{line.AttributeDescription}</small>";
            if (!string.IsNullOrEmpty(line.Sku))
                item += $"<br /><small>{WebUtility.HtmlEncode(line.Sku)}</small>";

            var mode = await _localizationService.GetResourceAsync(
                line.IsRental ? "mahjar.contracts.snapshot.mode.rent" : "mahjar.contracts.snapshot.mode.buy", languageId);

            var period = "&mdash;";
            if (line.IsRental && line.RentalStartUtc.HasValue && line.RentalEndUtc.HasValue)
            {
                var daysFormat = await _localizationService.GetResourceAsync("mahjar.contracts.snapshot.days", languageId);
                var daysText = string.Format(daysFormat, line.RentalDays);
                period = WebUtility.HtmlEncode(
                    $"{line.RentalStartUtc.Value.ToString("d", culture)} – {line.RentalEndUtc.Value.ToString("d", culture)} ({daysText})");
            }

            var deposit = line.IsRental && line.Deposit > decimal.Zero
                ? WebUtility.HtmlEncode(await FormatAmountAsync(line.Deposit, languageId))
                : "&mdash;";

            var cells = new[]
            {
                index.ToString(),
                item,
                WebUtility.HtmlEncode(mode),
                line.Quantity.ToString(),
                period,
                WebUtility.HtmlEncode(await FormatAmountAsync(line.UnitPrice, languageId)),
                deposit,
                WebUtility.HtmlEncode(await FormatAmountAsync(line.LineTotal, languageId))
            }.ToList();

            //keep the cells aligned with the (possibly trimmed) header row
            if (!hasRental)
            {
                cells.RemoveAt(depositColumn);
                cells.RemoveAt(periodColumn);
            }

            sb.Append("<tr class=\"item\">");
            for (var c = 0; c < cells.Count; c++)
                sb.Append($"<td data-label=\"{WebUtility.HtmlEncode(headers[c])}\" style=\"{cellStyle}\">{cells[c]}</td>");
            sb.Append("</tr>");
        }

        sb.Append("</tbody><tfoot>");
        var footerRows = new List<(string resourceKey, decimal amount)>
        {
            ("mahjar.contracts.snapshot.subtotal", snapshot.SubTotal),
            ("mahjar.contracts.snapshot.deposittotal", snapshot.DepositTotal),
            ("mahjar.contracts.snapshot.grandtotal", snapshot.GrandTotal)
        };
        //no rentals means no deposits, so the "security deposits: 0.00" row is just noise
        if (!hasRental)
            footerRows.RemoveAll(row => row.resourceKey == "mahjar.contracts.snapshot.deposittotal");

        //the label spans every column but the amount one
        var labelColspan = headers.Count - 1;
        foreach (var (resourceKey, amount) in footerRows)
        {
            var label = await _localizationService.GetResourceAsync(resourceKey, languageId);
            sb.Append("<tr class=\"total\">");
            sb.Append($"<td class=\"total-label\" colspan=\"{labelColspan}\" style=\"{cellStyle};font-weight:bold\">{WebUtility.HtmlEncode(label)}</td>");
            sb.Append($"<td class=\"total-amount\" style=\"{cellStyle};font-weight:bold\">{WebUtility.HtmlEncode(await FormatAmountAsync(amount, languageId))}</td>");
            sb.Append("</tr>");
        }

        sb.Append("</tfoot></table>");

        return sb.ToString();
    }

    /// <summary>Builds the full %Contract.*% / %Customer.*% / %Store.Name% token list for a request.</summary>
    protected virtual async Task<IList<Token>> BuildContractTokensAsync(ContractRequest request, int languageId)
    {
        //the request's own store (background/admin contexts may run under a different current store)
        var store = await _storeService.GetStoreByIdAsync(request.StoreId) ?? await _storeContext.GetCurrentStoreAsync();
        var customer = await _customerService.GetCustomerByIdAsync(request.CustomerId);
        var language = await _languageService.GetLanguageByIdAsync(languageId);
        var culture = GetCulture(language);
        var snapshot = await DeserializeSnapshotAsync(request);
        var createdOn = await _dateTimeHelper.ConvertToUserTimeAsync(request.CreatedOnUtc, DateTimeKind.Utc);

        //[Mahjar contracts] the rental agreement prints store-level and per-transaction values: the contract request's
        //own value wins, then the (localized) contract setting, and a dash when neither is filled in.
        const string dash = "—";
        string pick(string requestValue, string settingValue) =>
            !string.IsNullOrWhiteSpace(requestValue) ? requestValue.Trim()
            : !string.IsNullOrWhiteSpace(settingValue) ? settingValue.Trim()
            : dash;

        var operatorName = await _localizationService.GetLocalizedSettingAsync(_contractSettings, s => s.OperatorLegalName, languageId, store.Id, true, false);
        var supportChannel = await _localizationService.GetLocalizedSettingAsync(_contractSettings, s => s.SupportChannel, languageId, store.Id, true, false);
        var guaranteeType = await _localizationService.GetLocalizedSettingAsync(_contractSettings, s => s.GuaranteeType, languageId, store.Id, true, false);
        var deliveryDetails = await _localizationService.GetLocalizedSettingAsync(_contractSettings, s => s.DeliveryDetails, languageId, store.Id, true, false);
        var returnDetails = await _localizationService.GetLocalizedSettingAsync(_contractSettings, s => s.ReturnDetails, languageId, store.Id, true, false);
        var extraFeesTerms = await _localizationService.GetLocalizedSettingAsync(_contractSettings, s => s.ExtraFeesTerms, languageId, store.Id, true, false);
        var taxTerms = await _localizationService.GetLocalizedSettingAsync(_contractSettings, s => s.TaxTerms, languageId, store.Id, true, false);
        var lateFeeTerms = await _localizationService.GetLocalizedSettingAsync(_contractSettings, s => s.LateReturnFeeTerms, languageId, store.Id, true, false);
        var cancellationTerms = await _localizationService.GetLocalizedSettingAsync(_contractSettings, s => s.CancellationFeeTerms, languageId, store.Id, true, false);

        var guaranteeAmount = request.GuaranteeAmount ?? (snapshot.DepositTotal > decimal.Zero ? snapshot.DepositTotal : (decimal?)null);
        var storeName = await _localizationService.GetLocalizedAsync(store, s => s.Name, languageId);

        return new List<Token>
        {
            new("Contract.Number", GetContractNumber(request)),
            new("Contract.Date", createdOn.ToString("d", culture)),
            new("Customer.FullName", customer != null ? await _customerService.GetCustomerFullNameAsync(customer) : string.Empty),
            new("Customer.Email", customer?.Email ?? string.Empty),
            new("Store.Name", storeName),
            new("Contract.ItemsTable", await BuildItemsTableAsync(snapshot, languageId), true),
            new("Contract.SubTotal", await FormatAmountAsync(snapshot.SubTotal, languageId)),
            new("Contract.DepositTotal", await FormatAmountAsync(snapshot.DepositTotal, languageId)),
            new("Contract.GrandTotal", await FormatAmountAsync(snapshot.GrandTotal, languageId)),
            new("Contract.RejectReason", request.RejectionReason ?? string.Empty),
            new("Contract.SignURL", $"{store.Url?.TrimEnd('/')}/customer/contracts", true),

            //[Mahjar contracts] the platform as a contracting party
            new("Customer.Id", customer?.Id.ToString(culture) ?? dash),
            new("Store.LegalName", pick(operatorName, !string.IsNullOrWhiteSpace(store.CompanyName) ? store.CompanyName : storeName)),
            new("Store.CommercialRegistration", pick(_contractSettings.CommercialRegistrationNumber, null)),
            new("Store.SupportChannel", pick(supportChannel, store.CompanyPhoneNumber)),

            //the agreement text itself
            new("Contract.Version", pick(_contractSettings.AgreementVersion, "1.0")),
            new("Contract.EffectiveDate", _contractSettings.AgreementEffectiveDateUtc.HasValue
                ? (await _dateTimeHelper.ConvertToUserTimeAsync(_contractSettings.AgreementEffectiveDateUtc.Value, DateTimeKind.Utc)).ToString("d", culture)
                : dash),
            new("Contract.UpdatedDate", _contractSettings.AgreementUpdatedDateUtc.HasValue
                ? (await _dateTimeHelper.ConvertToUserTimeAsync(_contractSettings.AgreementUpdatedDateUtc.Value, DateTimeKind.Utc)).ToString("d", culture)
                : dash),

            //this transaction
            new("Contract.OrderNumber", request.OrderId > 0 ? request.OrderId.ToString(culture) : dash),
            new("Contract.RentalPeriod", await BuildRentalPeriodAsync(snapshot, languageId, culture) ?? dash),
            new("Contract.VerificationRef", pick(request.VerificationReference, null)),
            new("Contract.DeliveryDetails", pick(request.DeliveryDetails, deliveryDetails)),
            new("Contract.ReturnDetails", pick(request.ReturnDetails, returnDetails)),
            new("Contract.GuaranteeType", pick(request.GuaranteeType, guaranteeType)),
            new("Contract.GuaranteeAmount", guaranteeAmount.HasValue ? await FormatAmountAsync(guaranteeAmount.Value, languageId) : dash),
            new("Contract.ExtraFees", pick(request.ExtraFees, extraFeesTerms)),
            new("Contract.Tax", request.TaxAmount.HasValue ? await FormatAmountAsync(request.TaxAmount.Value, languageId) : pick(null, taxTerms)),
            new("Contract.LateFee", pick(request.LateReturnFee, lateFeeTerms)),

            //store policy values
            new("Contract.ItemValueCap", await FormatAmountAsync(_contractSettings.RentalItemValueCap, languageId)),
            new("Contract.LateGraceHours", _contractSettings.LateReturnGraceHours.ToString(culture)),
            new("Contract.CancellationFee", pick(null, cancellationTerms)),
            new("Contract.RefundDays", _contractSettings.RefundProcessingDays.ToString(culture)),
            new("Contract.ObjectionDays", _contractSettings.DamageObjectionDays.ToString(culture)),
            new("Contract.ComplaintDays", _contractSettings.ComplaintHandlingDays.ToString(culture)),
            new("Contract.SettlementDays", _contractSettings.AmicableSettlementDays.ToString(culture))
        };
    }

    /// <summary>
    /// [Mahjar contracts] The whole rental window of the frozen cart — the earliest start to the latest end, with the
    /// number of days — for the agreement's "rental period" row. Null when the cart holds no rental line.
    /// </summary>
    protected virtual async Task<string> BuildRentalPeriodAsync(ContractCartSnapshot snapshot, int languageId, CultureInfo culture)
    {
        var rentals = snapshot?.Lines?
            .Where(line => line.IsRental && line.RentalStartUtc.HasValue && line.RentalEndUtc.HasValue)
            .ToList();

        if (rentals == null || !rentals.Any())
            return null;

        var start = rentals.Min(line => line.RentalStartUtc.Value);
        var end = rentals.Max(line => line.RentalEndUtc.Value);
        var days = string.Format(await _localizationService.GetResourceAsync("mahjar.contracts.snapshot.days", languageId),
            rentals.Max(line => line.RentalDays));

        return $"{start.ToString("d", culture)} – {end.ToString("d", culture)} ({days})";
    }

    #endregion

    #region Methods

    /// <summary>Gets a contract request by identifier.</summary>
    public virtual async Task<ContractRequest> GetRequestByIdAsync(int id)
    {
        return await _contractRequestRepository.GetByIdAsync(id, cache => default);
    }

    /// <summary>Gets a contract request by its opaque sign-URL token.</summary>
    public virtual async Task<ContractRequest> GetRequestByTokenAsync(Guid token)
    {
        if (token == Guid.Empty)
            return null;

        return await _contractRequestRepository.Table.FirstOrDefaultAsync(request => request.Token == token);
    }

    /// <summary>Gets the customer's newest active request (Pending, ContractSent, Signed or Approved).</summary>
    public virtual async Task<ContractRequest> GetActiveRequestAsync(int customerId, int storeId)
    {
        if (customerId <= 0)
            return null;

        var query = _contractRequestRepository.Table
            .Where(request => request.CustomerId == customerId && _activeStatusIds.Contains(request.StatusId));

        if (storeId > 0)
            query = query.Where(request => request.StoreId == storeId);

        return await query
            .OrderByDescending(request => request.CreatedOnUtc)
            .ThenByDescending(request => request.Id)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Gets ALL of the customer's active requests (Pending, ContractSent, Signed or Approved), newest first.
    /// Duplicates can exist (e.g. concurrent submits), so drift/checkout logic must consider every one.
    /// </summary>
    protected virtual async Task<IList<ContractRequest>> GetActiveRequestsAsync(int customerId, int storeId)
    {
        if (customerId <= 0)
            return new List<ContractRequest>();

        var query = _contractRequestRepository.Table
            .Where(request => request.CustomerId == customerId && _activeStatusIds.Contains(request.StatusId));

        if (storeId > 0)
            query = query.Where(request => request.StoreId == storeId);

        return await query
            .OrderByDescending(request => request.CreatedOnUtc)
            .ThenByDescending(request => request.Id)
            .ToListAsync();
    }

    /// <summary>Searches contract requests (newest first).</summary>
    public virtual async Task<IPagedList<ContractRequest>> SearchRequestsAsync(int customerId = 0, int storeId = 0,
        ContractRequestStatus? status = null, DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        return await _contractRequestRepository.GetAllPagedAsync(query =>
        {
            if (customerId > 0)
                query = query.Where(request => request.CustomerId == customerId);
            if (storeId > 0)
                query = query.Where(request => request.StoreId == storeId);
            if (status.HasValue)
            {
                var statusId = (int)status.Value;
                query = query.Where(request => request.StatusId == statusId);
            }

            if (createdFromUtc.HasValue)
                query = query.Where(request => request.CreatedOnUtc >= createdFromUtc.Value);
            if (createdToUtc.HasValue)
                query = query.Where(request => request.CreatedOnUtc <= createdToUtc.Value);

            return query.OrderByDescending(request => request.CreatedOnUtc).ThenByDescending(request => request.Id);
        }, pageIndex, pageSize);
    }

    /// <summary>Counts the contract requests in a given status.</summary>
    public virtual async Task<int> GetCountByStatusAsync(ContractRequestStatus status, int storeId = 0)
    {
        var statusId = (int)status;
        var query = _contractRequestRepository.Table.Where(request => request.StatusId == statusId);

        if (storeId > 0)
            query = query.Where(request => request.StoreId == storeId);

        return await query.CountAsync();
    }

    /// <summary>Whether the given cart requires a contract before payment.</summary>
    public virtual Task<bool> IsContractRequiredAsync(IList<ShoppingCartItem> cart)
    {
        if (!_contractSettings.Enabled || cart == null || !cart.Any())
            return Task.FromResult(false);

        if (_contractSettings.RequireOnlyForRentals)
            return Task.FromResult(cart.Any(IsRentalLine));

        return Task.FromResult(true);
    }

    /// <summary>
    /// Formats one hashable line (invariant culture; round-trip "o" date format, null dates as empty string).
    /// Shared by the cart and the order hash so an order placed from a frozen cart produces the same hash.
    /// </summary>
    protected static string FormatHashLine(int productId, string attributesXml, int quantity,
        DateTime? rentalStartDateUtc, DateTime? rentalEndDateUtc)
    {
        static string FormatDate(DateTime? date) => date?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty;

        return string.Create(CultureInfo.InvariantCulture,
            $"{productId}|{attributesXml}|{quantity}|{FormatDate(rentalStartDateUtc)}|{FormatDate(rentalEndDateUtc)}");
    }

    /// <summary>
    /// Joins the hashable lines (deterministic content sort — ordinal, so the hash is independent of the
    /// source line ordering/ids) and computes the SHA256 hex hash.
    /// </summary>
    protected static string ComputeLinesHash(IEnumerable<string> lines)
    {
        var payload = string.Join("|;", lines.OrderBy(line => line, StringComparer.Ordinal));

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    /// <summary>
    /// Computes the deterministic SHA256 hex hash of the cart lines (content-sorted; invariant culture;
    /// round-trip "o" date format, null dates as empty string) used to detect cart drift.
    /// </summary>
    public virtual Task<string> ComputeCartHashAsync(IList<ShoppingCartItem> cart)
    {
        ArgumentNullException.ThrowIfNull(cart);

        var hash = ComputeLinesHash(cart.Select(sci =>
            FormatHashLine(sci.ProductId, sci.AttributesXml, sci.Quantity, sci.RentalStartDateUtc, sci.RentalEndDateUtc)));

        return Task.FromResult(hash);
    }

    /// <summary>
    /// Computes the deterministic SHA256 hex hash of the order items in the identical per-line format as
    /// <see cref="ComputeCartHashAsync"/>, so a placed order can be matched back to the contract request
    /// whose frozen cart it was gated on.
    /// </summary>
    public virtual Task<string> ComputeOrderHashAsync(IList<OrderItem> orderItems)
    {
        ArgumentNullException.ThrowIfNull(orderItems);

        var hash = ComputeLinesHash(orderItems.Select(item =>
            FormatHashLine(item.ProductId, item.AttributesXml, item.Quantity, item.RentalStartDateUtc, item.RentalEndDateUtc)));

        return Task.FromResult(hash);
    }

    /// <summary>[Mahjar handover] The contract request an order was placed against (the newest one), or null.</summary>
    public virtual async Task<ContractRequest> GetRequestByOrderIdAsync(int orderId)
    {
        if (orderId <= 0)
            return null;

        return await _contractRequestRepository.Table
            .Where(request => request.OrderId == orderId)
            .OrderByDescending(request => request.Id)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Submits a contract request from the current cart. An existing active request with the same cart hash
    /// is returned unchanged; one with a different hash is cancelled and superseded by a new request.
    /// </summary>
    public virtual async Task<ContractRequest> SubmitRequestAsync(Customer customer, IList<ShoppingCartItem> cart, Store store)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(cart);
        ArgumentNullException.ThrowIfNull(store);

        //[Mahjar rental-occupancy] never open a contract for a cart that cannot be checked out (e.g. buying an item that
        //is rented out): the same item validation checkout runs, and its first problem is the answer
        foreach (var sci in cart)
        {
            var product = await _productService.GetProductByIdAsync(sci.ProductId);
            if (product == null)
                continue;

            var warnings = await _shoppingCartService.GetShoppingCartItemWarningsAsync(customer, sci.ShoppingCartType, product,
                store.Id, sci.AttributesXml, sci.CustomerEnteredPrice, sci.RentalStartDateUtc, sci.RentalEndDateUtc,
                sci.Quantity, false, sci.Id);
            if (warnings.Any())
                throw new NopException(warnings.First());
        }

        var cartHash = await ComputeCartHashAsync(cart);

        //an unchanged cart keeps its (newest) matching active request; every drifted active request is
        //cancelled — considering ALL active requests, not just the newest, so no stale duplicate survives
        var activeRequests = await GetActiveRequestsAsync(customer.Id, store.Id);
        var matchingRequest = activeRequests
            .FirstOrDefault(request => string.Equals(request.CartHash, cartHash, StringComparison.Ordinal));
        if (matchingRequest != null)
        {
            foreach (var staleRequest in activeRequests.Where(request => request.Id != matchingRequest.Id &&
                         !string.Equals(request.CartHash, cartHash, StringComparison.Ordinal)))
                await CancelAsync(staleRequest);

            return matchingRequest;
        }

        //flood throttle (new requests only; the reuse path above is exempt): at most 10 requests per
        //24 hours and at least 60 seconds between two consecutive requests, whatever their status
        var utcNow = DateTime.UtcNow;
        var dayAgoUtc = utcNow.AddHours(-24);
        var recentCount = await _contractRequestRepository.Table
            .Where(request => request.CustomerId == customer.Id && request.CreatedOnUtc >= dayAgoUtc)
            .CountAsync();
        var newestRequest = await _contractRequestRepository.Table
            .Where(request => request.CustomerId == customer.Id)
            .OrderByDescending(request => request.CreatedOnUtc)
            .ThenByDescending(request => request.Id)
            .FirstOrDefaultAsync();
        if (recentCount >= 10 || (newestRequest != null && (utcNow - newestRequest.CreatedOnUtc).TotalSeconds < 60))
            throw new NopException(await _localizationService.GetResourceAsync("mahjar.contracts.toomanyrequests"));

        foreach (var staleRequest in activeRequests)
            await CancelAsync(staleRequest);

        //freeze the cart into a snapshot (amounts in the primary store currency)
        var arLanguage = (await _languageService.GetAllLanguagesAsync()).FirstOrDefault(IsArabic);
        var snapshot = new ContractCartSnapshot();
        foreach (var sci in cart)
        {
            var product = await _productService.GetProductByIdAsync(sci.ProductId);
            if (product == null)
                continue;

            var isRental = IsRentalLine(sci);
            var deposit = isRental ? product.RentalSecurityDeposit : decimal.Zero;
            var (unitPrice, _, _) = await _shoppingCartService.GetUnitPriceAsync(sci, true);
            //the fork's price calculation already folds the security deposit into the rental unit price
            //([Mahjar rental-deposit]); strip it so the snapshot shows price and deposit separately
            unitPrice = Math.Max(unitPrice - deposit, decimal.Zero);

            snapshot.Lines.Add(new ContractCartSnapshotLine
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductNameAr = arLanguage != null
                    ? await _localizationService.GetLocalizedAsync(product, p => p.Name, arLanguage.Id)
                    : product.Name,
                Sku = product.Sku,
                AttributeDescription = await _productAttributeFormatter.FormatAttributesAsync(product, sci.AttributesXml,
                    customer, store, renderPrices: false, allowHyperlinks: false),
                IsRental = isRental,
                Quantity = sci.Quantity,
                RentalStartUtc = sci.RentalStartDateUtc,
                RentalEndUtc = sci.RentalEndDateUtc,
                RentalDays = isRental ? Math.Max((sci.RentalEndDateUtc.Value - sci.RentalStartDateUtc.Value).Days, 1) : 0,
                UnitPrice = unitPrice,
                Deposit = deposit,
                LineTotal = unitPrice * sci.Quantity + (isRental ? deposit * sci.Quantity : decimal.Zero)
            });
        }

        //totals are the plain sums of the snapshot's own tax-exclusive lines (UnitPrice is already
        //deposit-free, see above), so the footer always adds up to the lines shown in the contract
        snapshot.SubTotal = snapshot.Lines.Sum(line => line.UnitPrice * line.Quantity);
        snapshot.DepositTotal = snapshot.Lines.Sum(line => line.Deposit * line.Quantity);
        snapshot.GrandTotal = snapshot.SubTotal + snapshot.DepositTotal;
        snapshot.CurrencyCode = (await GetPrimaryStoreCurrencyAsync())?.CurrencyCode;

        var language = await _workContext.GetWorkingLanguageAsync();
        var request = new ContractRequest
        {
            CustomerId = customer.Id,
            StoreId = store.Id,
            Status = ContractRequestStatus.Pending,
            Token = Guid.NewGuid(),
            LanguageId = language.Id,
            CartSnapshotJson = JsonSerializer.Serialize(snapshot),
            CartHash = cartHash,
            CreatedOnUtc = DateTime.UtcNow,
            UpdatedOnUtc = DateTime.UtcNow
        };

        await _contractRequestRepository.InsertAsync(request);

        await _eventPublisher.PublishAsync(new ContractRequestedEvent(request));

        return request;
    }

    /// <summary>
    /// Whether the customer may proceed to payment: either no contract is required, or an Approved request
    /// exists whose stored cart hash matches the current cart.
    /// </summary>
    public virtual async Task<bool> IsCheckoutAllowedAsync(Customer customer, IList<ShoppingCartItem> cart, int storeId)
    {
        ArgumentNullException.ThrowIfNull(customer);

        if (!await IsContractRequiredAsync(cart))
            return true;

        //ANY active Approved request matching the current cart unlocks checkout (a newer non-approved
        //duplicate must not shadow an approved one)
        var cartHash = await ComputeCartHashAsync(cart);
        var activeRequests = await GetActiveRequestsAsync(customer.Id, storeId);

        return activeRequests.Any(request => request.Status == ContractRequestStatus.Approved &&
            string.Equals(request.CartHash, cartHash, StringComparison.Ordinal));
    }

    /// <summary>Deserializes the frozen cart snapshot of a request (never null).</summary>
    public virtual Task<ContractCartSnapshot> DeserializeSnapshotAsync(ContractRequest request)
    {
        if (string.IsNullOrEmpty(request?.CartSnapshotJson))
            return Task.FromResult(new ContractCartSnapshot());

        return Task.FromResult(JsonSerializer.Deserialize<ContractCartSnapshot>(request.CartSnapshotJson)
                               ?? new ContractCartSnapshot());
    }

    /// <summary>
    /// Renders the localized contract template with the request's tokens, picking the template whose kind
    /// matches the request's frozen cart (purchase / rental / mixed).
    /// </summary>
    public virtual async Task<string> RenderContractBodyAsync(ContractRequest request, int languageId)
    {
        ArgumentNullException.ThrowIfNull(request);

        var templateType = await GetTemplateTypeAsync(request);
        var template = await GetTemplateByTypeAsync(templateType)
                       ?? throw new NopException("No contract template has been configured");

        var body = await _localizationService.GetLocalizedAsync(template, t => t.Body, languageId);
        var tokens = await BuildContractTokensAsync(request, languageId);

        return _tokenizer.Replace(body, tokens, true);
    }

    /// <summary>Renders and freezes the contract, then sends it to the customer (status ContractSent).</summary>
    public virtual async Task SendContractAsync(ContractRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        await EnsureStatusAsync(request, ContractRequestStatus.Pending);

        //[Mahjar handover] the customer signs against the condition photos/video shown next to the contract, so every
        //item must have them BEFORE the contract can go out — and they are locked the moment it does
        var missingProductIds = await _conditionCheckService.GetProductIdsMissingPreContractMediaAsync(request);
        if (missingProductIds.Any())
        {
            var isArabic = IsArabic(await _workContext.GetWorkingLanguageAsync());
            var missingNames = (await DeserializeSnapshotAsync(request)).Lines
                .Where(line => missingProductIds.Contains(line.ProductId))
                .Select(line => isArabic && !string.IsNullOrEmpty(line.ProductNameAr) ? line.ProductNameAr : line.ProductName)
                .Distinct();

            throw new NopException(string.Format(
                await _localizationService.GetResourceAsync("admin.mahjar.handover.precontract.missing"),
                string.Join(isArabic ? "، " : ", ", missingNames)));
        }

        request.ContractBody = await RenderContractBodyAsync(request, request.LanguageId);
        request.SentOnUtc = DateTime.UtcNow;
        request.Status = ContractRequestStatus.ContractSent;
        await UpdateRequestAsync(request);

        //[Mahjar handover] what the customer is about to sign against can no longer change
        await _conditionCheckService.LockPreContractChecksAsync(request, await _workContext.GetCurrentCustomerAsync());

        await _eventPublisher.PublishAsync(new ContractSentEvent(request));
    }

    /// <summary>Records the customer's signature on a sent contract (status Signed).</summary>
    public virtual async Task SignContractAsync(ContractRequest request, string signatureDataUrl, string signedName, string ip)
    {
        ArgumentNullException.ThrowIfNull(request);

        await EnsureStatusAsync(request, ContractRequestStatus.ContractSent);

        const string pngDataUrlPrefix = "data:image/png;base64,";
        signedName = signedName?.Trim();
        if (string.IsNullOrEmpty(signatureDataUrl) ||
            !signatureDataUrl.StartsWith(pngDataUrlPrefix, StringComparison.Ordinal) ||
            signatureDataUrl.Length > 300000 ||
            string.IsNullOrWhiteSpace(signedName) ||
            signedName.Length > 400)
        {
            throw new NopException(await _localizationService.GetResourceAsync("mahjar.contracts.sign.invalidsignature"));
        }

        //the payload must be valid base64 that decodes to a real PNG (magic bytes 89 50 4E 47 0D 0A 1A 0A)
        byte[] signatureBytes;
        try
        {
            signatureBytes = Convert.FromBase64String(signatureDataUrl[pngDataUrlPrefix.Length..]);
        }
        catch (FormatException)
        {
            throw new NopException(await _localizationService.GetResourceAsync("mahjar.contracts.sign.invalidsignature"));
        }

        var pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        if (signatureBytes.Length < pngSignature.Length || !signatureBytes.Take(pngSignature.Length).SequenceEqual(pngSignature))
            throw new NopException(await _localizationService.GetResourceAsync("mahjar.contracts.sign.invalidsignature"));

        request.SignatureDataUrl = signatureDataUrl;
        request.SignedName = signedName;
        request.SignedOnUtc = DateTime.UtcNow;
        request.SignedIp = ip;
        request.Status = ContractRequestStatus.Signed;
        await UpdateRequestAsync(request);

        await _eventPublisher.PublishAsync(new ContractSignedEvent(request));
    }

    /// <summary>Approves a signed contract (status Approved), unlocking checkout.</summary>
    public virtual async Task ApproveAsync(ContractRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        await EnsureStatusAsync(request, ContractRequestStatus.Signed);

        request.ReviewedOnUtc = DateTime.UtcNow;
        request.Status = ContractRequestStatus.Approved;
        await UpdateRequestAsync(request);

        await _eventPublisher.PublishAsync(new ContractApprovedEvent(request));
    }

    /// <summary>Rejects a contract request with a reason (status Rejected).</summary>
    public virtual async Task RejectAsync(ContractRequest request, string reason)
    {
        ArgumentNullException.ThrowIfNull(request);

        await EnsureStatusAsync(request, ContractRequestStatus.Pending, ContractRequestStatus.ContractSent,
            ContractRequestStatus.Signed);

        request.ReviewedOnUtc = DateTime.UtcNow;
        request.RejectionReason = reason;
        request.Status = ContractRequestStatus.Rejected;
        await UpdateRequestAsync(request);

        await _eventPublisher.PublishAsync(new ContractRejectedEvent(request));
    }

    /// <summary>Cancels a non-terminal contract request (no-op when already terminal).</summary>
    public virtual async Task CancelAsync(ContractRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_activeStatusIds.Contains(request.StatusId))
            return;

        request.Status = ContractRequestStatus.Cancelled;
        await UpdateRequestAsync(request);
    }

    /// <summary>Marks an approved request as Completed once an order was placed against it.</summary>
    public virtual async Task MarkCompletedAsync(ContractRequest request, int orderId)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.OrderId = orderId;
        request.Status = ContractRequestStatus.Completed;
        await UpdateRequestAsync(request);
    }

    /// <summary>Updates a contract request (stamps <see cref="ContractRequest.UpdatedOnUtc"/>).</summary>
    public virtual async Task UpdateRequestAsync(ContractRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.UpdatedOnUtc = DateTime.UtcNow;
        await _contractRequestRepository.UpdateAsync(request);
    }

    /// <summary>Gets every contract template, ordered by kind (purchase, rental, mixed).</summary>
    public virtual async Task<IList<ContractTemplate>> GetAllTemplatesAsync()
    {
        return await _contractTemplateRepository.GetAllAsync(
            query => query.OrderBy(template => template.TemplateTypeId).ThenBy(template => template.Id),
            cache => default);
    }

    /// <summary>Gets a contract template by identifier.</summary>
    public virtual async Task<ContractTemplate> GetTemplateByIdAsync(int id)
    {
        return await _contractTemplateRepository.GetByIdAsync(id, cache => default);
    }

    /// <summary>
    /// Gets the contract template for a kind, falling back to the mixed template (which carries the combined
    /// sales/rental wording) and then to any template, so a missing row can never break rendering.
    /// </summary>
    public virtual async Task<ContractTemplate> GetTemplateByTypeAsync(ContractTemplateType templateType)
    {
        var templates = await GetAllTemplatesAsync();

        return templates.FirstOrDefault(template => template.TemplateType == templateType)
               ?? templates.FirstOrDefault(template => template.TemplateType == ContractTemplateType.Mixed)
               ?? templates.FirstOrDefault();
    }

    /// <summary>
    /// Determines which contract kind a frozen cart calls for. An empty snapshot falls back to Mixed, whose
    /// wording covers both cases.
    /// </summary>
    public virtual ContractTemplateType GetTemplateType(ContractCartSnapshot snapshot)
    {
        var lines = snapshot?.Lines;
        if (lines == null || lines.Count == 0)
            return ContractTemplateType.Mixed;

        var hasRental = lines.Any(line => line.IsRental);
        var hasPurchase = lines.Any(line => !line.IsRental);

        if (hasRental && hasPurchase)
            return ContractTemplateType.Mixed;

        return hasRental ? ContractTemplateType.Rental : ContractTemplateType.Purchase;
    }

    /// <summary>Determines which contract kind a request calls for, from its frozen cart snapshot.</summary>
    public virtual async Task<ContractTemplateType> GetTemplateTypeAsync(ContractRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return GetTemplateType(await DeserializeSnapshotAsync(request));
    }

    /// <summary>Updates the contract template (stamps <see cref="ContractTemplate.UpdatedOnUtc"/>).</summary>
    public virtual async Task UpdateTemplateAsync(ContractTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        template.UpdatedOnUtc = DateTime.UtcNow;
        await _contractTemplateRepository.UpdateAsync(template);
    }

    #endregion
}
