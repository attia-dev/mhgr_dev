// [Mahjar handover]
using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Gives the handover staff what they need to work the handover desk and nothing more.
///
/// This is an UPDATE-pass migration on purpose: it runs from StartEngineAsync AFTER
/// <c>IPermissionService.InsertPermissionsAsync</c>, so on the very boot that ships the feature the new
/// <c>Mahjar.HandoversManage</c> permission (and, on a server where the contracts feature is new too,
/// <c>Mahjar.ContractsView</c>) already exist. A schema migration runs before that and would silently miss them.
///
/// 1) Makes sure the <c>HandoverStaff</c> role exists — the permission installer auto-creates it from
///    MahjarPermissionConfigManager with Name = SystemName — and gives it its friendly name. It is a SYSTEM role
///    (IsSystemRole = 1) at the store owner's request: it cannot be deleted, deactivated or have its system name
///    changed; only its display name can be edited. The installer creates it as a plain role, so this UPDATE is what
///    locks it on every database.
/// 2) Grants it <c>Security.AccessAdminPanel</c> and <c>Mahjar.ContractsView</c>: both already exist, and the
///    installer only maps default roles for BRAND-NEW permissions, so they must be granted here. Also (re)grants
///    <c>Mahjar.HandoversManage</c> in case the role was created after that permission.
/// 3) Arabic label of the new permission (the installer writes the English name into every language).
///
/// Idempotent; SQL Server dialect, matching the sibling Mahjar migrations. [Mahjar handover]
/// </summary>
[NopMigration("2026-09-16 10:05:00", "Mahjar handover: handover-staff role + admin-panel / contract-view access", MigrationProcessType.Update)]
public class HandoverStaffAclMigration : ForwardOnlyMigration
{
    /// <summary>The Arabic-language predicate every Mahjar resource migration uses.</summary>
    private const string IS_ARABIC = "(l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')";

    public override void Up()
    {
        IfDatabase("sqlserver").Execute.Sql(@"
IF NOT EXISTS (SELECT 1 FROM CustomerRole WHERE SystemName = 'HandoverStaff')
    INSERT INTO CustomerRole ([Name], [SystemName], [FreeShipping], [TaxExempt], [Active], [IsSystemRole],
                              [EnablePasswordLifetime], [OverrideTaxDisplayType], [DefaultTaxDisplayTypeId], [PurchasedWithProductId])
    VALUES ('Handover staff', 'HandoverStaff', 0, 0, 1, 1, 0, 0, 0, 0);

UPDATE CustomerRole
SET [Name] = CASE WHEN [Name] = 'HandoverStaff' THEN 'Handover staff' ELSE [Name] END,
    [IsSystemRole] = 1,
    [Active] = 1
WHERE SystemName = 'HandoverStaff';

INSERT INTO PermissionRecord_Role_Mapping (PermissionRecord_Id, CustomerRole_Id)
SELECT p.Id, r.Id
FROM PermissionRecord p
CROSS JOIN CustomerRole r
WHERE r.SystemName = 'HandoverStaff'
  AND p.SystemName IN ('Security.AccessAdminPanel', 'Mahjar.ContractsView', 'Mahjar.HandoversManage')
  AND NOT EXISTS (
      SELECT 1 FROM PermissionRecord_Role_Mapping m
      WHERE m.PermissionRecord_Id = p.Id AND m.CustomerRole_Id = r.Id);

UPDATE r
SET r.ResourceValue = N'لوحة الإدارة. الاستلام والتسليم. إدارة'
FROM LocaleStringResource r
JOIN Language l ON l.Id = r.LanguageId
WHERE r.ResourceName = 'Security.Permission.Mahjar.HandoversManage'
  AND " + IS_ARABIC + @"
  AND r.ResourceValue = 'Admin area. Mahjar handovers. Manage';");
    }
}
