// [Mahjar contracts]
using Nop.Core.Domain.Customers;

namespace Nop.Services.Security;

/// <summary>
/// Mahjar permission config manager (contract-approval stage) [Mahjar contracts]
/// </summary>
public partial class MahjarPermissionConfigManager : IPermissionConfigManager
{
    public IList<PermissionConfig> AllConfigs => new List<PermissionConfig>
    {
        new ("Admin area. Mahjar contracts. View", StandardPermission.Mahjar.CONTRACTS_VIEW, nameof(StandardPermission.Mahjar), NopCustomerDefaults.AdministratorsRoleName),
        new ("Admin area. Mahjar contracts. Manage", StandardPermission.Mahjar.CONTRACTS_MANAGE, nameof(StandardPermission.Mahjar), NopCustomerDefaults.AdministratorsRoleName),
        //[Mahjar handover] new permission → on first boot the installer maps it to both roles, creating the
        //HandoverStaff role if it does not exist yet (HandoverStaffAclMigration then gives it its friendly name and
        //the two EXISTING permissions it needs, which a config manager cannot grant)
        new ("Admin area. Mahjar handovers. Manage", StandardPermission.Mahjar.HANDOVERS_MANAGE, nameof(StandardPermission.Mahjar), NopCustomerDefaults.AdministratorsRoleName, NopCustomerDefaults.HandoverStaffRoleName)
    };
}
