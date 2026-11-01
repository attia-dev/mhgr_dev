// [Mahjar roles]
using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Makes the stock "Forum Moderators" customer role an ORDINARY role, so the store owner can delete it from
/// Admin, Customers, Customer roles. The store runs with forums switched off, and nopCommerce only ever asks "is this
/// customer in ForumModerators?" — which is simply false once the role is gone — so nothing breaks without it.
/// Applied only while forums are disabled.
///
/// The other stock system roles are deliberately left locked: nopCommerce looks them up by system name for the admin
/// panel (Administrators), sign-in and registration (Registered), every anonymous visitor and cart (Guests) and the
/// partner panel (Vendors); deleting any of them breaks the store. Idempotent; SQL Server dialect. [Mahjar roles]
/// </summary>
[NopSchemaMigration("2026-09-16 10:15:00", "Mahjar roles: Forum Moderators becomes an ordinary (deletable) role while forums are off")]
public class ForumModeratorsOrdinaryRoleMigration : ForwardOnlyMigration
{
    public override void Up()
    {
        IfDatabase("sqlserver").Execute.Sql(@"
UPDATE CustomerRole
SET IsSystemRole = 0
WHERE SystemName = 'ForumModerators'
  AND IsSystemRole = 1
  AND NOT EXISTS (SELECT 1 FROM [Setting] WHERE [Name] = 'forumsettings.forumsenabled' AND [Value] = 'True');");
    }
}
