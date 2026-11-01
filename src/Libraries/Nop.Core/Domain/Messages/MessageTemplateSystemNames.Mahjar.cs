// [Mahjar contracts]
namespace Nop.Core.Domain.Messages;

/// <summary>
/// System names of the Mahjar contract-approval message templates (rows seeded by the contracts migration).
/// [Mahjar contracts]
/// </summary>
public static partial class MessageTemplateSystemNames
{
    #region Mahjar contracts

    /// <summary>
    /// Represents system name of notification to the store owner about a new contract request
    /// </summary>
    public const string MAHJAR_CONTRACT_REQUESTED_STORE_OWNER_NOTIFICATION = "Mahjar.ContractRequested.StoreOwnerNotification";

    /// <summary>
    /// Represents system name of notification to the customer that the contract is ready to sign
    /// </summary>
    public const string MAHJAR_CONTRACT_SENT_CUSTOMER_NOTIFICATION = "Mahjar.ContractSent.CustomerNotification";

    /// <summary>
    /// Represents system name of notification to the store owner that the contract was signed
    /// </summary>
    public const string MAHJAR_CONTRACT_SIGNED_STORE_OWNER_NOTIFICATION = "Mahjar.ContractSigned.StoreOwnerNotification";

    /// <summary>
    /// Represents system name of notification to the customer that the contract was approved
    /// </summary>
    public const string MAHJAR_CONTRACT_APPROVED_CUSTOMER_NOTIFICATION = "Mahjar.ContractApproved.CustomerNotification";

    /// <summary>
    /// Represents system name of notification to the customer that the contract request was rejected
    /// </summary>
    public const string MAHJAR_CONTRACT_REJECTED_CUSTOMER_NOTIFICATION = "Mahjar.ContractRejected.CustomerNotification";

    #endregion
}
