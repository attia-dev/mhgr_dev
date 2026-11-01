// [Mahjar contracts]
using System.Globalization;
using System.Text.Json;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Mahjar;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Stores;
using Nop.Core.Infrastructure;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.Helpers;

namespace Nop.Services.Messages;

/// <summary>
/// Mahjar contract-approval email notifications (partial additions to the workflow message service).
/// A partial class cannot add constructor parameters, so the services the stock ctor does not provide
/// are resolved via <see cref="EngineContext"/> inside the methods. [Mahjar contracts]
/// </summary>
public partial class WorkflowMessageService
{
    #region Utilities ([Mahjar contracts])

    /// <summary>
    /// Builds the common %Contract.*% + %Customer.*% tokens shared by all contract message templates.
    /// Amounts come from the request's frozen cart snapshot, formatted in the primary store currency.
    /// </summary>
    protected virtual async Task<List<Token>> BuildContractCommonTokensAsync(ContractRequest request, Customer customer,
        int languageId, Store store)
    {
        //resolve the services the stock ctor does not provide [Mahjar contracts]
        var priceFormatter = EngineContext.Current.Resolve<IPriceFormatter>();
        var dateTimeHelper = EngineContext.Current.Resolve<IDateTimeHelper>();
        var currencyService = EngineContext.Current.Resolve<ICurrencyService>();
        var currencySettings = EngineContext.Current.Resolve<CurrencySettings>();

        var snapshot = string.IsNullOrEmpty(request.CartSnapshotJson)
            ? new ContractCartSnapshot()
            : JsonSerializer.Deserialize<ContractCartSnapshot>(request.CartSnapshotJson) ?? new ContractCartSnapshot();

        var primaryCurrency = await currencyService.GetCurrencyByIdAsync(currencySettings.PrimaryStoreCurrencyId);

        async Task<string> formatAmountAsync(decimal amount) =>
            await priceFormatter.FormatPriceAsync(amount, true, primaryCurrency, languageId, false);

        var language = await _languageService.GetLanguageByIdAsync(languageId);
        CultureInfo culture;
        try
        {
            culture = new CultureInfo(language?.LanguageCulture ?? string.Empty);
        }
        catch (CultureNotFoundException)
        {
            culture = CultureInfo.InvariantCulture;
        }

        var createdOn = await dateTimeHelper.ConvertToUserTimeAsync(request.CreatedOnUtc, DateTimeKind.Utc);

        var tokens = new List<Token>
        {
            new("Contract.Number", $"MHGR-C-{request.Id:D5}"),
            new("Contract.Date", createdOn.ToString("d", culture)),
            new("Contract.RejectReason", request.RejectionReason ?? string.Empty),
            new("Contract.SignURL", $"{store.Url?.TrimEnd('/')}/customer/contracts", true),
            new("Contract.SubTotal", await formatAmountAsync(snapshot.SubTotal)),
            new("Contract.DepositTotal", await formatAmountAsync(snapshot.DepositTotal)),
            new("Contract.GrandTotal", await formatAmountAsync(snapshot.GrandTotal))
        };
        await _messageTokenProvider.AddCustomerTokensAsync(tokens, customer.Id);

        return tokens;
    }

    /// <summary>
    /// Sends a contract message-template notification either to the customer or to the store owner
    /// (same shape as the stock Send*NotificationAsync methods).
    /// </summary>
    protected virtual async Task<IList<int>> SendContractNotificationAsync(ContractRequest request, Customer customer,
        int languageId, string messageTemplateName, bool toStoreOwner)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(customer);

        var store = await _storeService.GetStoreByIdAsync(request.StoreId) ?? await _storeContext.GetCurrentStoreAsync();
        languageId = await EnsureLanguageIsActiveAsync(languageId, store.Id);

        var messageTemplates = await GetActiveMessageTemplatesAsync(messageTemplateName, store.Id);
        if (!messageTemplates.Any())
            return new List<int>();

        //tokens
        var commonTokens = await BuildContractCommonTokensAsync(request, customer, languageId, store);

        return await messageTemplates.SelectAwait(async messageTemplate =>
        {
            //email account
            var emailAccount = await GetEmailAccountOfMessageTemplateAsync(messageTemplate, languageId);

            var tokens = new List<Token>(commonTokens);
            await _messageTokenProvider.AddStoreTokensAsync(tokens, store, emailAccount, languageId);

            //event notification
            await _eventPublisher.MessageTokensAddedAsync(messageTemplate, tokens);

            var (toEmail, toName) = toStoreOwner
                ? await GetStoreOwnerNameAndEmailAsync(emailAccount)
                : (customer.Email, await _customerService.GetCustomerFullNameAsync(customer));

            return await SendNotificationAsync(messageTemplate, emailAccount, languageId, tokens, toEmail, toName);
        }).ToListAsync();
    }

    #endregion

    #region Methods ([Mahjar contracts])

    /// <summary>
    /// Sends a "new contract request" notification to the store owner
    /// </summary>
    /// <param name="request">Contract request</param>
    /// <param name="customer">The customer who submitted the request</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the queued email identifiers
    /// </returns>
    public virtual async Task<IList<int>> SendContractRequestedStoreOwnerNotificationAsync(ContractRequest request,
        Customer customer, int languageId)
    {
        return await SendContractNotificationAsync(request, customer, languageId,
            MessageTemplateSystemNames.MAHJAR_CONTRACT_REQUESTED_STORE_OWNER_NOTIFICATION, toStoreOwner: true);
    }

    /// <summary>
    /// Sends a "your contract is ready to sign" notification to a customer
    /// </summary>
    /// <param name="request">Contract request</param>
    /// <param name="customer">The customer the contract was sent to</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the queued email identifiers
    /// </returns>
    public virtual async Task<IList<int>> SendContractSentCustomerNotificationAsync(ContractRequest request,
        Customer customer, int languageId)
    {
        return await SendContractNotificationAsync(request, customer, languageId,
            MessageTemplateSystemNames.MAHJAR_CONTRACT_SENT_CUSTOMER_NOTIFICATION, toStoreOwner: false);
    }

    /// <summary>
    /// Sends a "contract signed" notification to the store owner
    /// </summary>
    /// <param name="request">Contract request</param>
    /// <param name="customer">The customer who signed the contract</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the queued email identifiers
    /// </returns>
    public virtual async Task<IList<int>> SendContractSignedStoreOwnerNotificationAsync(ContractRequest request,
        Customer customer, int languageId)
    {
        return await SendContractNotificationAsync(request, customer, languageId,
            MessageTemplateSystemNames.MAHJAR_CONTRACT_SIGNED_STORE_OWNER_NOTIFICATION, toStoreOwner: true);
    }

    /// <summary>
    /// Sends a "contract approved" notification to a customer
    /// </summary>
    /// <param name="request">Contract request</param>
    /// <param name="customer">The customer whose contract was approved</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the queued email identifiers
    /// </returns>
    public virtual async Task<IList<int>> SendContractApprovedCustomerNotificationAsync(ContractRequest request,
        Customer customer, int languageId)
    {
        return await SendContractNotificationAsync(request, customer, languageId,
            MessageTemplateSystemNames.MAHJAR_CONTRACT_APPROVED_CUSTOMER_NOTIFICATION, toStoreOwner: false);
    }

    /// <summary>
    /// Sends a "contract rejected" notification to a customer
    /// </summary>
    /// <param name="request">Contract request</param>
    /// <param name="customer">The customer whose contract request was rejected</param>
    /// <param name="languageId">Message language identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the queued email identifiers
    /// </returns>
    public virtual async Task<IList<int>> SendContractRejectedCustomerNotificationAsync(ContractRequest request,
        Customer customer, int languageId)
    {
        return await SendContractNotificationAsync(request, customer, languageId,
            MessageTemplateSystemNames.MAHJAR_CONTRACT_REJECTED_CUSTOMER_NOTIFICATION, toStoreOwner: false);
    }

    #endregion
}
