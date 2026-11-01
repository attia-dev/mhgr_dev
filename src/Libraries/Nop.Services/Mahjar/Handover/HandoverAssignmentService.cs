// [Mahjar handover]
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Mahjar;
using Nop.Data;
using Nop.Services.Customers;
using Nop.Services.Localization;

namespace Nop.Services.Mahjar.Handover;

/// <summary>
/// Who handles an order's handover. One row per order (a filtered unique index enforces it), written when an admin
/// assigns the order on the order page. [Mahjar handover]
/// </summary>
public partial class HandoverAssignmentService : IHandoverAssignmentService
{
    #region Fields

    protected readonly ICustomerService _customerService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IRepository<HandoverAssignment> _assignmentRepository;

    #endregion

    #region Ctor

    public HandoverAssignmentService(ICustomerService customerService,
        ILocalizationService localizationService,
        IRepository<HandoverAssignment> assignmentRepository)
    {
        _customerService = customerService;
        _localizationService = localizationService;
        _assignmentRepository = assignmentRepository;
    }

    #endregion

    #region Methods

    /// <summary>Gets the assignment of an order (null when nobody is assigned yet).</summary>
    public virtual async Task<HandoverAssignment> GetByOrderIdAsync(int orderId)
    {
        if (orderId <= 0)
            return null;

        return await _assignmentRepository.Table
            .Where(assignment => assignment.OrderId == orderId)
            .OrderByDescending(assignment => assignment.Id)
            .FirstOrDefaultAsync();
    }

    /// <summary>The employee an order is assigned to, or 0 when nobody is.</summary>
    public virtual async Task<int> GetAssignedCustomerIdAsync(int orderId)
    {
        return (await GetByOrderIdAsync(orderId))?.AssignedToCustomerId ?? 0;
    }

    /// <summary>The employees the given orders are assigned to, keyed by order id (orders without one are absent).</summary>
    public virtual async Task<IDictionary<int, int>> GetAssignedCustomerIdsAsync(IList<int> orderIds)
    {
        var map = new Dictionary<int, int>();
        if (orderIds == null || !orderIds.Any())
            return map;

        var ids = orderIds.Where(id => id > 0).Distinct().ToList();
        if (!ids.Any())
            return map;

        var rows = await _assignmentRepository.Table
            .Where(assignment => ids.Contains(assignment.OrderId))
            .ToListAsync();

        foreach (var row in rows.OrderBy(assignment => assignment.Id))
            map[row.OrderId] = row.AssignedToCustomerId;

        return map;
    }

    /// <summary>Assigns an order to a handover employee, or moves it to another one.</summary>
    public virtual async Task AssignAsync(int orderId, int assignToCustomerId, Customer assignedBy)
    {
        ArgumentNullException.ThrowIfNull(assignedBy);

        if (orderId <= 0)
            throw new NopException(await ResourceAsync("admin.mahjar.handover.notfound"));

        //only an active member of the handover desk may own the work
        var employee = await _customerService.GetCustomerByIdAsync(assignToCustomerId);
        if (employee == null || !employee.Active || employee.Deleted ||
            !await _customerService.IsInCustomerRoleAsync(employee, NopCustomerDefaults.HandoverStaffRoleName))
        {
            throw new NopException(await ResourceAsync("admin.mahjar.handover.assign.notstaff"));
        }

        var now = DateTime.UtcNow;
        var assignment = await GetByOrderIdAsync(orderId);
        if (assignment == null)
        {
            await _assignmentRepository.InsertAsync(new HandoverAssignment
            {
                OrderId = orderId,
                AssignedToCustomerId = employee.Id,
                AssignedByCustomerId = assignedBy.Id,
                CreatedOnUtc = now,
                UpdatedOnUtc = now
            });

            return;
        }

        assignment.AssignedToCustomerId = employee.Id;
        assignment.AssignedByCustomerId = assignedBy.Id;
        assignment.UpdatedOnUtc = now;
        await _assignmentRepository.UpdateAsync(assignment);
    }

    /// <summary>Clears an order's assignment (it goes back to "waiting to be assigned").</summary>
    public virtual async Task ClearAsync(int orderId)
    {
        var assignment = await GetByOrderIdAsync(orderId);
        if (assignment != null)
            await _assignmentRepository.DeleteAsync(assignment);
    }

    /// <summary>The active handover employees who may be assigned work.</summary>
    public virtual async Task<IList<Customer>> GetAssignableStaffAsync()
    {
        var role = await _customerService.GetCustomerRoleBySystemNameAsync(NopCustomerDefaults.HandoverStaffRoleName);
        if (role is not { Active: true })
            return new List<Customer>();

        return (await _customerService.GetAllCustomersAsync(customerRoleIds: new[] { role.Id }))
            .Where(customer => customer.Active && !customer.Deleted)
            .ToList();
    }

    /// <summary>Whether the employee may work the handover of this order.</summary>
    public virtual async Task<bool> MayWorkOrderAsync(int orderId, Customer employee)
    {
        if (employee == null)
            return false;

        var assignedTo = await GetAssignedCustomerIdAsync(orderId);

        //nobody assigned: the desk works it as before, so a paid order is never stuck
        if (assignedTo == 0)
            return true;

        if (assignedTo == employee.Id)
            return true;

        //an administrator is not a desk employee, but must never be locked out of the work
        return await _customerService.IsAdminAsync(employee);
    }

    #endregion

    #region Utilities

    /// <summary>A localized resource in the working language.</summary>
    protected virtual async Task<string> ResourceAsync(string key)
    {
        return await _localizationService.GetResourceAsync(key);
    }

    #endregion
}
