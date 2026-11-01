// [Mahjar home-slides]
using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Points the "Rent with confidence" seed slide (<see cref="HomeSlidesMigration"/>, button "Rent now" / «استأجر الآن»)
/// at rent mode: a product slide whose TargetValue is "rent" opens the product page with Rent already chosen, on the
/// website (?mode=rent) and in the app (product?id=…&amp;mode=rent). Only the untouched seed row is changed — the one
/// still titled 'Wear Luxury for the Occasion', linking a product, with no link value — so an owner's own edits in
/// Admin → Content management → Home slides survive. Idempotent (a re-run finds nothing to change; a database without
/// the seed row, e.g. a fresh install without sample data, changes nothing) and SQL Server only like the sibling
/// Mahjar migrations. The admin form's "Open in rent mode" checkbox was added with it (resources in
/// <see cref="HomeSlidesAdminMigration"/>). NOTE: the slide table is cached — restart (or POST /Admin/Common/ClearCache)
/// after a hand-applied change. [Mahjar home-slides]
/// </summary>
[NopSchemaMigration("2026-09-29 10:30:00", "Mahjar home-slides: rent-mode link for the rental seed slide")]
public class HomeSlidesRentModeMigration : ForwardOnlyMigration
{
    /// <summary>The only dialect the T-SQL targets; the SQLite test DB simply skips it.</summary>
    private const string DATABASE_TYPE = "sqlserver";

    public override void Up()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
IF OBJECT_ID(N'dbo.HomeSlide', N'U') IS NOT NULL
BEGIN
    UPDATE HomeSlide
    SET TargetValue = N'rent',
        UpdatedOnUtc = SYSUTCDATETIME()
    WHERE Title = N'Wear Luxury for the Occasion'
      AND TargetTypeId = 10
      AND TargetValue IS NULL;
END");
    }
}
