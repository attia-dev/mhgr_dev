// [Mahjar handover]
namespace Nop.Core.Domain.Customers;

public static partial class NopCustomerDefaults
{
    /// <summary>
    /// Gets a system name of the handover-staff customer role: the employees who receive items from partners,
    /// hand them over to customers and take rentals back, documenting the condition at every step. The role has
    /// admin-panel access limited to the handover desk plus read-only contracts. [Mahjar handover]
    /// </summary>
    public static string HandoverStaffRoleName => "HandoverStaff";
}
