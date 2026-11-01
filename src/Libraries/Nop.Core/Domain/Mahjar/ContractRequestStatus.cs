// [Mahjar contracts]
namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// The lifecycle of a contract request: the customer submits from the cart (Pending), the admin renders and
/// sends the contract (ContractSent), the customer signs it (Signed), the admin approves or rejects it, and
/// after a successful checkout the request is Completed. Cancelled covers customer withdrawal and cart-drift
/// resubmission. [Mahjar contracts]
/// </summary>
public enum ContractRequestStatus
{
    /// <summary>Submitted by the customer, awaiting admin review.</summary>
    Pending = 10,

    /// <summary>The admin sent the rendered contract; awaiting the customer's signature.</summary>
    ContractSent = 20,

    /// <summary>Signed by the customer; awaiting admin approval.</summary>
    Signed = 30,

    /// <summary>Approved by the admin; the customer may proceed to payment.</summary>
    Approved = 40,

    /// <summary>Rejected by the admin (with a reason).</summary>
    Rejected = 50,

    /// <summary>Cancelled by the customer, or superseded after the cart changed.</summary>
    Cancelled = 60,

    /// <summary>An order was placed against the approved contract.</summary>
    Completed = 70
}
