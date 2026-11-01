// [Mahjar contracts]
namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// A customer's contract-approval request: a frozen snapshot of the cart the customer wants to buy/rent,
/// the rendered contract document, the electronic signature, and the admin's review decision. Checkout is
/// gated on an Approved request whose <see cref="CartHash"/> still matches the current cart.
/// [Mahjar contracts]
/// </summary>
public partial class ContractRequest : BaseEntity
{
    /// <summary>The customer who submitted the request.</summary>
    public int CustomerId { get; set; }

    /// <summary>The store identifier.</summary>
    public int StoreId { get; set; }

    /// <summary>The status discriminator (backing value for <see cref="Status"/>).</summary>
    public int StatusId { get; set; }

    /// <summary>Opaque token used in the customer-facing sign URL.</summary>
    public Guid Token { get; set; }

    /// <summary>The customer's working language when the request was submitted (contract is rendered in it).</summary>
    public int LanguageId { get; set; }

    /// <summary>The frozen cart snapshot (serialized <see cref="ContractCartSnapshot"/>).</summary>
    public string CartSnapshotJson { get; set; }

    /// <summary>SHA256 hash of the cart lines at submission time; any cart drift invalidates the request.</summary>
    public string CartHash { get; set; }

    /// <summary>The rendered contract HTML, frozen when the admin sends the contract (nullable until then).</summary>
    public string ContractBody { get; set; }

    /// <summary>The customer's drawn signature as a PNG data URL (nullable until signed).</summary>
    public string SignatureDataUrl { get; set; }

    /// <summary>The customer's typed full name at signing (nullable until signed).</summary>
    public string SignedName { get; set; }

    /// <summary>When the customer signed (UTC), if signed.</summary>
    public DateTime? SignedOnUtc { get; set; }

    /// <summary>The IP address the signature was submitted from (nullable until signed).</summary>
    public string SignedIp { get; set; }

    /// <summary>Free-form internal admin note (nullable).</summary>
    public string AdminComment { get; set; }

    /// <summary>The reason given by the admin on rejection (nullable).</summary>
    public string RejectionReason { get; set; }

    /// <summary>The order placed against the approved contract (0 = none yet).</summary>
    public int OrderId { get; set; }

    /// <summary>Creation date (UTC).</summary>
    public DateTime CreatedOnUtc { get; set; }

    /// <summary>When the admin sent the contract for signing (UTC), if sent.</summary>
    public DateTime? SentOnUtc { get; set; }

    /// <summary>When the admin approved/rejected (UTC), if reviewed.</summary>
    public DateTime? ReviewedOnUtc { get; set; }

    /// <summary>Last state-transition date (UTC).</summary>
    public DateTime UpdatedOnUtc { get; set; }

    #region Per-transaction contract values ([Mahjar contracts])

    //Each of these overrides the matching default in ContractSettings for THIS contract only, and is printed by the
    //template through its %Contract.*% token. Null/empty means "use the store-wide setting"; when that is empty too,
    //the token prints a dash. They are editable until the contract is sent, because sending freezes the rendered body.

    /// <summary>When and where the item is handed over (overrides ContractSettings.DeliveryDetails).</summary>
    public string DeliveryDetails { get; set; }

    /// <summary>When and where the item is taken back (overrides ContractSettings.ReturnDetails).</summary>
    public string ReturnDetails { get; set; }

    /// <summary>The identity-verification method or reference recorded for this customer.</summary>
    public string VerificationReference { get; set; }

    /// <summary>The guarantee or security instrument required (overrides ContractSettings.GuaranteeType).</summary>
    public string GuaranteeType { get; set; }

    /// <summary>The guarantee amount; null falls back to the frozen cart's deposit total.</summary>
    public decimal? GuaranteeAmount { get; set; }

    /// <summary>Any additional fees for this transaction (overrides ContractSettings.ExtraFeesTerms).</summary>
    public string ExtraFees { get; set; }

    /// <summary>The tax amount for this transaction; null falls back to ContractSettings.TaxTerms.</summary>
    public decimal? TaxAmount { get; set; }

    /// <summary>The late-return charge for this transaction (overrides ContractSettings.LateReturnFeeTerms).</summary>
    public string LateReturnFee { get; set; }

    #endregion

    /// <summary>Typed wrapper over <see cref="StatusId"/>.</summary>
    public ContractRequestStatus Status
    {
        get => (ContractRequestStatus)StatusId;
        set => StatusId = (int)value;
    }
}
