using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Adds the bilingual (en/ar) localized strings for the prominent "become a seller" call-to-action
/// shown on the storefront home page (see Nop.Web/Views/Home/Index.cshtml). Idempotent: each string
/// is inserted per language only when missing. Schema migration so it auto-applies on deploy against
/// an already-installed database. SQL Server dialect. [Mahjar seller-cta]
/// </summary>
[NopSchemaMigration("2026-06-14 13:00:00", "Mahjar seller-cta: home page 'become a seller' resources")]
public class SellerCtaMigration : ForwardOnlyMigration
{
    public override void Up()
    {
        Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN (l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%') THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    ('mahjar.home.sellercta.title',  N'بِع معنا على محجر',                                            N'Sell with us on MHGR'),
    ('mahjar.home.sellercta.text',   N'انضمّ إلى باعتنا واعرض منتجاتك للبيع أو للإيجار على محجر.',     N'Join our sellers and list your products for sale or for rent on MHGR.'),
    ('mahjar.home.sellercta.button', N'سجّل كبائع الآن',                                              N'Become a seller')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }
}
