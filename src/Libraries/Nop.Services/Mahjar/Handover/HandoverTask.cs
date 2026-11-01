// [Mahjar handover]
using Nop.Core.Domain.Mahjar;

namespace Nop.Services.Mahjar.Handover;

/// <summary>
/// One row of a handover-desk work queue: an item that needs (or got) a condition check at a checkpoint, with the
/// check itself when one was started. [Mahjar handover]
/// </summary>
public partial class HandoverTask
{
    /// <summary>The queue the row belongs to.</summary>
    public HandoverStage Stage { get; set; }

    /// <summary>The checkpoint to document.</summary>
    public ConditionCheckType CheckType { get; set; }

    /// <summary>The product (the physical item).</summary>
    public int ProductId { get; set; }

    /// <summary>The contract request (0 = none).</summary>
    public int ContractRequestId { get; set; }

    /// <summary>The order (0 = none).</summary>
    public int OrderId { get; set; }

    /// <summary>The customer-facing order number (null when there is no order).</summary>
    public string OrderNumber { get; set; }

    /// <summary>The order item (0 = none).</summary>
    public int OrderItemId { get; set; }

    /// <summary>The quantity of the order item / contract line.</summary>
    public int Quantity { get; set; }

    /// <summary>The customer (0 = none, e.g. an intake).</summary>
    public int CustomerId { get; set; }

    /// <summary>Rental start (UTC), rentals only.</summary>
    public DateTime? RentalStartUtc { get; set; }

    /// <summary>Rental end (UTC) — the return due date — rentals only.</summary>
    public DateTime? RentalEndUtc { get; set; }

    /// <summary>The date the row is sorted by: the contract/order date, or the check date for confirmed checks.</summary>
    public DateTime DateUtc { get; set; }

    /// <summary>[Mahjar handover] The employee the order's handover is assigned to (0 = nobody yet).</summary>
    public int AssignedCustomerId { get; set; }

    /// <summary>The condition check already started for the row (null = not started).</summary>
    public ConditionCheck Check { get; set; }
}

/// <summary>
/// What the desk narrowed a work queue down to. Every part is optional; an empty filter lists the whole queue.
///
/// The keyword is resolved to product and customer ids BEFORE it reaches the service: the admin factory owns the
/// catalog and customer searches (localized product names, and customer names, which live in generic attributes
/// rather than columns), which keeps the queue itself a plain join. A row matches the keyword when ANY of its parts
/// does — the item, the customer, the order number or the contract number. [Mahjar handover]
/// </summary>
public partial class HandoverTaskFilter
{
    /// <summary>
    /// <see cref="AssignedCustomerId"/>: only orders that nobody owns yet.
    ///
    /// Deliberately a large POSITIVE number rather than -1. The dashboard runs in Arabic (ar-EG), whose ICU culture
    /// writes a negative sign that is NOT the ASCII hyphen, and MVC parses posted form values with the current
    /// culture — so a plain "-1" fails to bind, silently leaves 0, and the option sits in the dropdown doing
    /// nothing. Proven on this very page before it shipped. No customer id can ever reach this value.
    /// </summary>
    public const int UNASSIGNED = int.MaxValue;

    /// <summary>
    /// <see cref="StatusId"/>: only rows whose check has not been started. Positive for the same reason as
    /// <see cref="UNASSIGNED"/>; no <see cref="Nop.Core.Domain.Mahjar.ConditionCheckStatus"/> can reach it.
    /// </summary>
    public const int NOT_STARTED = int.MaxValue;

    /// <summary>What was typed in the search box (null or empty = no keyword).</summary>
    public string Keyword { get; set; }

    /// <summary>The products whose name or SKU matched the keyword.</summary>
    public IList<int> KeywordProductIds { get; set; } = new List<int>();

    /// <summary>The customers whose name, e-mail or user name matched the keyword.</summary>
    public IList<int> KeywordCustomerIds { get; set; } = new List<int>();

    /// <summary>The contract the keyword names when it reads like a contract number (0 = it does not).</summary>
    public int KeywordContractRequestId { get; set; }

    /// <summary>0 = anyone, <see cref="UNASSIGNED"/> = waiting to be assigned, otherwise that employee.</summary>
    public int AssignedCustomerId { get; set; }

    /// <summary>0 = any, <see cref="NOT_STARTED"/> = not started, otherwise a <see cref="ConditionCheckStatus"/> value.</summary>
    public int StatusId { get; set; }

    /// <summary>Only rows dated on or after this instant.</summary>
    public DateTime? FromUtc { get; set; }

    /// <summary>Only rows dated before this instant — the caller passes the day AFTER the one the user picked.</summary>
    public DateTime? ToUtc { get; set; }

    /// <summary>Only rentals whose return date has already passed.</summary>
    public bool OverdueOnly { get; set; }

    /// <summary>Whether a keyword was typed at all.</summary>
    public bool HasKeyword => !string.IsNullOrWhiteSpace(Keyword);
}

/// <summary>
/// What confirming a condition check did besides locking it, as localized lines for the employee: the side effects
/// that happened (order delivered, product page updated, deposit to refund) and the ones that failed without
/// undoing the confirmation. [Mahjar handover]
/// </summary>
public partial class ConditionCheckConfirmResult
{
    /// <summary>Side effects that happened.</summary>
    public IList<string> Messages { get; } = new List<string>();

    /// <summary>Side effects that failed; the check itself stays confirmed.</summary>
    public IList<string> Warnings { get; } = new List<string>();
}
