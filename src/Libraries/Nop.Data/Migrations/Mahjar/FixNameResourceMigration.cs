using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Repairs a bad bulk localization edit: a large number of Arabic "الاسم" (Name) field/column
/// labels had been overwritten with "إسم البائع" (Seller name). As a result the generic "Name"
/// column showed "إسم البائع" across the whole back office (categories, manufacturers, countries,
/// currencies, languages, all attribute editors, activity types, campaigns, discounts, vendors, …)
/// and the storefront vendor-info name field. This restores every affected Arabic resource to "الاسم".
///
/// Implemented as a schema migration so it re-applies on every app start until recorded, which means
/// it runs automatically against an already-installed database on deploy (data migrations would not).
/// The statement is idempotent / re-runnable and scoped to Arabic languages only. SQL Server dialect.
/// [Mahjar branding]
/// </summary>
[NopSchemaMigration("2026-06-23 11:00:00", "Mahjar branding: restore Arabic 'الاسم' label overwritten with 'إسم البائع'")]
public class FixNameResourceMigration : ForwardOnlyMigration
{
    public override void Up()
    {
        //predicate (over a joined Language alias "l") that is true for Arabic languages
        const string isArabic = "(l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')";

        Execute.Sql($@"
UPDATE r SET r.ResourceValue = N'الاسم'
FROM LocaleStringResource r
JOIN Language l ON l.Id = r.LanguageId
WHERE r.ResourceValue = N'إسم البائع'
  AND {isArabic};");
    }
}
