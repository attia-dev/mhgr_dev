// [Mahjar contracts]
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Mahjar;

namespace Nop.Services.Messages;

/// <summary>
/// Mahjar contract-approval email notifications (partial additions to the workflow message service).
/// [Mahjar contracts]
/// </summary>
public partial interface IWorkflowMessageService
{
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
    Task<IList<int>> SendContractRequestedStoreOwnerNotificationAsync(ContractRequest request, Customer customer, int languageId);

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
    Task<IList<int>> SendContractSentCustomerNotificationAsync(ContractRequest request, Customer customer, int languageId);

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
    Task<IList<int>> SendContractSignedStoreOwnerNotificationAsync(ContractRequest request, Customer customer, int languageId);

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
    Task<IList<int>> SendContractApprovedCustomerNotificationAsync(ContractRequest request, Customer customer, int languageId);

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
    Task<IList<int>> SendContractRejectedCustomerNotificationAsync(ContractRequest request, Customer customer, int languageId);
}
