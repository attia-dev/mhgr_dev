using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Enables the "Become a seller" feature by turning on VendorSettings.AllowCustomersToApplyForVendorAccount, the
/// gate that lets a logged-in customer apply for a vendor account. The web flow (/vendor/apply) and the new mobile
/// API (POST /api/customer/apply-vendor) both honour this setting server-side. Applications create a DISABLED vendor
/// and email the store owner; an admin activates the vendor and grants the Vendors role.
///
/// Idempotent (flips the value to True whether the row exists or not). SQL Server dialect. [Mahjar become-seller]
/// </summary>
[NopSchemaMigration("2026-06-21 10:05:00", "Mahjar: enable customers to apply for a vendor account (become a seller)")]
public class BecomeSellerMigration : ForwardOnlyMigration
{
    public override void Up()
    {
        Execute.Sql(@"
UPDATE [Setting] SET [Value] = 'True' WHERE [Name] = 'vendorsettings.allowcustomerstoapplyforvendoraccount';
IF @@ROWCOUNT = 0
    INSERT INTO [Setting] ([Name], [Value], [StoreId])
    VALUES ('vendorsettings.allowcustomerstoapplyforvendoraccount', 'True', 0);");
    }
}
