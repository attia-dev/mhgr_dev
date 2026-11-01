using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Points each home-page category card at one of the featured products' real watch renders (a distinct
/// image per category) instead of the generic sample placeholder. Implemented by sharing the product's
/// primary picture (category image alt/title text is derived from the category name, so there is no SEO
/// downside, and no binary copy is needed — works whether pictures live in the DB or on disk).
///
/// Matched by stable English names (not ids) so it is deploy-safe across environments, and idempotent
/// (re-running sets the same value / no-ops). Schema migration so it auto-applies on startup.
/// NOTE: the home-page category model is cached — ClearCache/restart after this runs. SQL Server dialect.
/// [Mahjar category-images]
/// </summary>
[NopSchemaMigration("2026-06-14 14:00:00", "Mahjar category-images: use featured-product renders as home-page category images")]
public class CategoryImagesMigration : ForwardOnlyMigration
{
    public override void Up()
    {
        //match the source image by the product picture's stable SeoFilename (set at image import,
        //identical across environments) rather than by display name — more deploy-robust & deterministic.
        Execute.Sql(@"
DECLARE @map TABLE (CatName nvarchar(400), PicSeo nvarchar(400));
INSERT INTO @map (CatName, PicSeo) VALUES
 (N'Women''s Watches', N'rose-gold-elegance'),
 (N'Luxury Watches',   N'royal-moonphase'),
 (N'Men''s Watches',   N'heritage-automatic-41'),
 (N'Sport Watches',    N'chronograph-sport'),
 (N'Smart Watches',    N'smart-watch-pro');

UPDATE c
SET c.PictureId = src.Id
FROM Category c
JOIN @map m ON m.CatName = c.Name
CROSS APPLY (
    SELECT TOP 1 pic.Id
    FROM Picture pic
    WHERE pic.SeoFilename = m.PicSeo
    ORDER BY pic.Id
) src
WHERE c.Deleted = 0 AND c.ShowOnHomepage = 1 AND c.PictureId <> src.Id;");
    }
}
