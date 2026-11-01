// [Mahjar terminology]
using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Global terminology rebrand: replaces the Arabic "seller" word-forms (بائع / باعة …) with the "partner"
/// forms (شريك / شركاء …) across EVERY Arabic locale-resource value — storefront, admin, notifications and
/// the Mahjar seller resources alike. The marketplace now refers to vendors as "شركاء".
///
/// Pure value-REPLACE (no key list), so it also catches nopCommerce core vendor strings. Replacement order
/// matters: the plurals and the possessive "باعتنا" (our sellers) are handled BEFORE the generic بائع→شريك
/// so "البائعين" becomes "الشركاء" (not the dual "الشريكين"). Verified on the dev DB before shipping: 142
/// Arabic rows transformed, 0 residual seller-forms. Scoped to Arabic only; English stays "Vendor".
///
/// Schema migration so it auto-applies on startup against an already-installed DB (data migrations would
/// not). Re-runnable — a second pass finds no seller-forms left. SQL Server dialect, matching the sibling
/// Mahjar resource migrations. NOTE: locale resources are cached — restart (or POST /Admin/Common/ClearCache)
/// after it runs. [Mahjar terminology] [Mahjar branding]
/// </summary>
[NopSchemaMigration("2026-06-29 12:00:00", "Mahjar terminology: rename Arabic بائع/باعة → شريك/شركاء across all resources")]
public class SellerToPartnerTerminologyMigration : ForwardOnlyMigration
{
    public override void Up()
    {
        IfDatabase("sqlserver").Execute.Sql(@"
UPDATE r
SET r.ResourceValue =
    REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
        r.ResourceValue,
        N'باعتنا',   N'شركائنا'),
        N'الباعة',   N'الشركاء'),
        N'باعة',     N'شركاء'),
        N'البائعين', N'الشركاء'),
        N'بائعين',   N'شركاء'),
        N'البائعون', N'الشركاء'),
        N'بائعون',   N'شركاء'),
        N'بائع',     N'شريك')
FROM LocaleStringResource r
JOIN Language l ON l.Id = r.LanguageId
WHERE (l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')
  AND (r.ResourceValue LIKE N'%بائع%' OR r.ResourceValue LIKE N'%باعة%' OR r.ResourceValue LIKE N'%باعتنا%');");
    }
}
