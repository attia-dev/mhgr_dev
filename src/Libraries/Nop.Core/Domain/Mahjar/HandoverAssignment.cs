// [Mahjar handover]
namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// The handover employee an order is assigned to: the one person who hands its items over to the customer and, for a
/// rental, takes them back. One row per order, created when an admin assigns it on the order page and updated when
/// they reassign it.
///
/// It is its own row rather than a field of <see cref="ConditionCheck"/> because a check exists per order ITEM per
/// checkpoint and is only created once someone starts the work — while the assignment must exist BEFORE that, and
/// decides who is allowed to start it at all. [Mahjar handover]
/// </summary>
public partial class HandoverAssignment : BaseEntity
{
    /// <summary>The order whose handover (and rental return) is assigned.</summary>
    public int OrderId { get; set; }

    /// <summary>The handover employee who owns the work.</summary>
    public int AssignedToCustomerId { get; set; }

    /// <summary>The admin who made (or last changed) the assignment.</summary>
    public int AssignedByCustomerId { get; set; }

    /// <summary>When the order was first assigned (UTC).</summary>
    public DateTime CreatedOnUtc { get; set; }

    /// <summary>When the assignment was last changed (UTC); equals <see cref="CreatedOnUtc"/> until a reassignment.</summary>
    public DateTime UpdatedOnUtc { get; set; }
}
