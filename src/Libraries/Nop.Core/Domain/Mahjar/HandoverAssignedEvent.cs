// [Mahjar handover]
using Nop.Core.Domain.Orders;

namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// Published when an admin assigns an order's handover to a member of the handover desk (or moves it to another
/// one), so the employee who now owns the work is told about it. [Mahjar handover]
/// </summary>
public partial class HandoverAssignedEvent
{
    public HandoverAssignedEvent(Order order, int assignedToCustomerId)
    {
        Order = order;
        AssignedToCustomerId = assignedToCustomerId;
    }

    /// <summary>The order whose handover was assigned.</summary>
    public Order Order { get; }

    /// <summary>The handover employee who now owns it.</summary>
    public int AssignedToCustomerId { get; }
}
