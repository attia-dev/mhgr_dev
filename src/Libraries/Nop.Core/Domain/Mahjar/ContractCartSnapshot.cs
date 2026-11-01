// [Mahjar contracts]
namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// The frozen cart snapshot serialized into <see cref="ContractRequest.CartSnapshotJson"/> when a contract
/// request is submitted. Amounts are captured in the primary store currency. [Mahjar contracts]
/// </summary>
public partial class ContractCartSnapshot
{
    /// <summary>The frozen cart lines.</summary>
    public List<ContractCartSnapshotLine> Lines { get; set; } = new();

    /// <summary>Cart subtotal (with discounts), excluding deposits.</summary>
    public decimal SubTotal { get; set; }

    /// <summary>Sum of the refundable security deposits (deposit * qty over rental lines).</summary>
    public decimal DepositTotal { get; set; }

    /// <summary>SubTotal + DepositTotal.</summary>
    public decimal GrandTotal { get; set; }

    /// <summary>The primary store currency code the amounts are captured in.</summary>
    public string CurrencyCode { get; set; }
}

/// <summary>
/// A single frozen cart line inside a <see cref="ContractCartSnapshot"/>. [Mahjar contracts]
/// </summary>
public partial class ContractCartSnapshotLine
{
    /// <summary>The product identifier.</summary>
    public int ProductId { get; set; }

    /// <summary>The product name (English).</summary>
    public string ProductName { get; set; }

    /// <summary>The product name (Arabic; falls back to the English name).</summary>
    public string ProductNameAr { get; set; }

    /// <summary>The product SKU.</summary>
    public string Sku { get; set; }

    /// <summary>The formatted attribute description of the line.</summary>
    public string AttributeDescription { get; set; }

    /// <summary>Whether the line is a rental (carries rental dates).</summary>
    public bool IsRental { get; set; }

    /// <summary>The quantity.</summary>
    public int Quantity { get; set; }

    /// <summary>Rental start date (UTC), rentals only.</summary>
    public DateTime? RentalStartUtc { get; set; }

    /// <summary>Rental end date (UTC), rentals only.</summary>
    public DateTime? RentalEndUtc { get; set; }

    /// <summary>The rental period length in days (min 1), rentals only.</summary>
    public int RentalDays { get; set; }

    /// <summary>The unit price (after discounts).</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>The refundable security deposit per unit, rentals only.</summary>
    public decimal Deposit { get; set; }

    /// <summary>UnitPrice * Quantity (+ Deposit * Quantity when a rental).</summary>
    public decimal LineTotal { get; set; }
}
