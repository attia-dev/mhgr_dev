using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Require product reviews to be approved before they appear. The install default ships this OFF, which made
/// app- and web-submitted reviews show up immediately (unmoderated). Turning CatalogSettings.ProductReviewsMustBeApproved
/// ON makes the existing API/storefront code stamp new reviews IsApproved=false and hide them from the approved-only
/// list until an admin approves them (Admin → Catalog → Product reviews); the app already shows a "pending approval"
/// message. Affects BOTH the mobile app and the web storefront.
///
/// Idempotent (flips the value to True whether the row exists or not). Settings are cached — the app restart that
/// runs this migration also clears the cache. SQL Server dialect. [Mahjar reviews-moderation]
/// </summary>
[NopSchemaMigration("2026-06-21 10:00:00", "Mahjar: require product reviews to be approved (app + web)")]
public class ReviewModerationMigration : ForwardOnlyMigration
{
    public override void Up()
    {
        Execute.Sql(@"
UPDATE [Setting] SET [Value] = 'True' WHERE [Name] = 'catalogsettings.productreviewsmustbeapproved';
IF @@ROWCOUNT = 0
    INSERT INTO [Setting] ([Name], [Value], [StoreId])
    VALUES ('catalogsettings.productreviewsmustbeapproved', 'True', 0);");
    }
}
