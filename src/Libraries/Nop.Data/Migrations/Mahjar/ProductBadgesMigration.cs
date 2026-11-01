using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Bilingual labels for the buy/rent badges shown on product cards (_ProductBox) and the product detail page
/// (ProductTemplate.Simple). Matches the mobile app's "للبيع"/"للإيجار" wording. Idempotent (resources inserted
/// only when missing); schema migration so it auto-applies on startup. SQL Server dialect. [Mahjar buy-or-rent]
/// </summary>
[NopSchemaMigration("2026-06-21 10:10:00", "Mahjar: buy/rent product-badge labels (web)")]
public class ProductBadgesMigration : ForwardOnlyMigration
{
    public override void Up()
    {
        Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN (l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%') THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    ('mahjar.product.badge.forsale', N'للبيع',   N'For Sale'),
    ('mahjar.product.badge.forrent', N'للإيجار', N'For Rent')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }
}
