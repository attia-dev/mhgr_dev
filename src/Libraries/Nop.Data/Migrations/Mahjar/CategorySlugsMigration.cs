// [Mahjar category-slugs]
using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Gives the five watch categories watch urls instead of the stock nopCommerce sample slugs they were renamed from
/// (Luxury Watches was still /electronics, Men's /computers, Women's /apparel, Sport /digital-downloads, Smart /books),
/// which the home hero CTAs and shared links put in front of customers. For each category it inserts the new active
/// slug and deactivates the stock one, so nopCommerce 301-redirects every old url (bookmarks, search engines, shared
/// links) to the new one. Guarded per category: matched by its unique English name, and only while its active
/// standard-language slug is still the stock value and the new slug is not taken by anything — an admin's own SEO
/// name is never touched. Idempotent (a re-run finds nothing to do) and SQL Server only like the sibling Mahjar
/// migrations. NOTE: url records are cached — restart (or POST /Admin/Common/ClearCache) after a hand-applied run.
/// [Mahjar category-slugs]
/// </summary>
[NopSchemaMigration("2026-09-29 10:40:00", "Mahjar category slugs: watch categories get watch urls (stock slugs 301-redirect)")]
public class CategorySlugsMigration : ForwardOnlyMigration
{
    /// <summary>The only dialect the T-SQL targets; the SQLite test DB simply skips it.</summary>
    private const string DATABASE_TYPE = "sqlserver";

    public override void Up()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
DECLARE @map TABLE (CatName nvarchar(400), OldSlug nvarchar(400), NewSlug nvarchar(400));
INSERT INTO @map (CatName, OldSlug, NewSlug) VALUES
 (N'Luxury Watches',   N'electronics',       N'luxury-watches'),
 (N'Men''s Watches',   N'computers',         N'mens-watches'),
 (N'Women''s Watches', N'apparel',           N'womens-watches'),
 (N'Sport Watches',    N'digital-downloads', N'sport-watches'),
 (N'Smart Watches',    N'books',             N'smart-watches');

DECLARE @todo TABLE (CategoryId int, OldUrlRecordId int, NewSlug nvarchar(400));
INSERT INTO @todo (CategoryId, OldUrlRecordId, NewSlug)
SELECT c.Id, u.Id, m.NewSlug
FROM @map m
JOIN Category c ON c.[Name] = m.CatName AND c.Deleted = 0
JOIN UrlRecord u ON u.EntityName = N'Category' AND u.EntityId = c.Id AND u.LanguageId = 0
                AND u.IsActive = 1 AND u.Slug = m.OldSlug
WHERE (SELECT COUNT(*) FROM Category c2 WHERE c2.[Name] = m.CatName AND c2.Deleted = 0) = 1
  AND (SELECT COUNT(*) FROM UrlRecord a WHERE a.EntityName = N'Category' AND a.EntityId = c.Id
                                          AND a.LanguageId = 0 AND a.IsActive = 1) = 1
  AND NOT EXISTS (SELECT 1 FROM UrlRecord x WHERE x.Slug = m.NewSlug);

INSERT INTO UrlRecord (EntityId, EntityName, Slug, IsActive, LanguageId)
SELECT t.CategoryId, N'Category', t.NewSlug, 1, 0
FROM @todo t;

UPDATE u
SET u.IsActive = 0
FROM UrlRecord u
JOIN @todo t ON t.OldUrlRecordId = u.Id;");
    }
}
