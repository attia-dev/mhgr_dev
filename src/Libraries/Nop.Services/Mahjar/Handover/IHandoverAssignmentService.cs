// [Mahjar handover]
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Mahjar;

namespace Nop.Services.Mahjar.Handover;

/// <summary>
/// Who handles an order's handover: the single handover employee an admin assigned to it, who alone may hand its
/// items over and, for a rental, take them back. [Mahjar handover]
/// </summary>
public partial interface IHandoverAssignmentService
{
    /// <summary>Gets the assignment of an order (null when nobody is assigned yet).</summary>
    Task<HandoverAssignment> GetByOrderIdAsync(int orderId);

    /// <summary>The employee an order is assigned to, or 0 when nobody is.</summary>
    Task<int> GetAssignedCustomerIdAsync(int orderId);

    /// <summary>The employees the given orders are assigned to, keyed by order id (orders without one are absent).</summary>
    Task<IDictionary<int, int>> GetAssignedCustomerIdsAsync(IList<int> orderIds);

    /// <summary>
    /// Assigns an order to a handover employee, or moves it to another one. The employee must be an active member of
    /// the HandoverStaff role, and the order must not be handed over already.
    /// </summary>
    Task AssignAsync(int orderId, int assignToCustomerId, Customer assignedBy);

    /// <summary>Clears an order's assignment (it goes back to "waiting to be assigned").</summary>
    Task ClearAsync(int orderId);

    /// <summary>The active handover employees who may be assigned work.</summary>
    Task<IList<Customer>> GetAssignableStaffAsync();

    /// <summary>
    /// Whether the employee may work the handover of this order: true when they are the assignee. An unassigned order
    /// is open to the whole desk, so that a paid order is never stuck if nobody got round to assigning it.
    /// </summary>
    Task<bool> MayWorkOrderAsync(int orderId, Customer employee);
}
