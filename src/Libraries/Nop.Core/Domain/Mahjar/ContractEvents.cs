// [Mahjar contracts]
namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// Published when a customer submits a new contract request from the cart (status Pending), so admins can
/// be notified a request awaits review. [Mahjar contracts]
/// </summary>
public partial class ContractRequestedEvent
{
    public ContractRequestedEvent(ContractRequest contractRequest)
    {
        ContractRequest = contractRequest;
    }

    /// <summary>The contract request that was just submitted.</summary>
    public ContractRequest ContractRequest { get; }
}

/// <summary>
/// Published when an admin sends the rendered contract to the customer for signing (status ContractSent).
/// [Mahjar contracts]
/// </summary>
public partial class ContractSentEvent
{
    public ContractSentEvent(ContractRequest contractRequest)
    {
        ContractRequest = contractRequest;
    }

    /// <summary>The contract request whose contract was just sent.</summary>
    public ContractRequest ContractRequest { get; }
}

/// <summary>
/// Published when the customer signs the contract (status Signed), so admins can be notified it awaits
/// approval. [Mahjar contracts]
/// </summary>
public partial class ContractSignedEvent
{
    public ContractSignedEvent(ContractRequest contractRequest)
    {
        ContractRequest = contractRequest;
    }

    /// <summary>The contract request that was just signed.</summary>
    public ContractRequest ContractRequest { get; }
}

/// <summary>
/// Published when an admin approves a signed contract (status Approved), unlocking checkout for the
/// matching cart. [Mahjar contracts]
/// </summary>
public partial class ContractApprovedEvent
{
    public ContractApprovedEvent(ContractRequest contractRequest)
    {
        ContractRequest = contractRequest;
    }

    /// <summary>The contract request that was just approved.</summary>
    public ContractRequest ContractRequest { get; }
}

/// <summary>
/// Published when an admin rejects a contract request (status Rejected, with a reason). [Mahjar contracts]
/// </summary>
public partial class ContractRejectedEvent
{
    public ContractRejectedEvent(ContractRequest contractRequest)
    {
        ContractRequest = contractRequest;
    }

    /// <summary>The contract request that was just rejected.</summary>
    public ContractRequest ContractRequest { get; }
}
