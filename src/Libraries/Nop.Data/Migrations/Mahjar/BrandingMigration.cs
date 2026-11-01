using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Rebrands the back office: replaces leftover "nopCommerce" wording (and the Arabic "سبارك"
/// that had crept into the admin page title) with the store brand — "MHGR" in Latin contexts and
/// "محجر" in Arabic ones — and hides the "Powered by nopCommerce" link in both the admin and the
/// storefront footers. Genuine external references (nopcommerce.com links, the marketplace, premium
/// support, the literal default distributed-cache instance name) are intentionally left untouched.
///
/// Implemented as a schema migration so it re-applies on every app start until recorded, which means
/// it runs automatically against an already-installed database on deploy (data migrations would not).
/// All statements are idempotent / re-runnable. SQL Server dialect (this fork runs on MS SQL).
/// [Mahjar branding]
/// </summary>
[NopSchemaMigration("2026-06-14 12:00:00", "Mahjar branding: replace nopCommerce/سبارك wording with MHGR/محجر")]
public class BrandingMigration : ForwardOnlyMigration
{
    public override void Up()
    {
        //predicate (over a joined Language alias "l") that is true for Arabic languages
        const string isArabic = "(l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')";

        //1) admin page / browser-tab title — was "إدارة سبارك" (ar) / "nopCommerce administration" (en)
        Execute.Sql($@"
UPDATE r SET r.ResourceValue =
    CASE WHEN {isArabic} THEN N'إدارة محجر' ELSE N'MHGR Administration' END
FROM LocaleStringResource r
JOIN Language l ON l.Id = r.LanguageId
WHERE r.ResourceName = 'admin.pagetitle';");

        //2) deep scrub — rebrand the generic "nopCommerce" mentions in admin labels / help texts
        Execute.Sql($@"
UPDATE r SET r.ResourceValue =
    REPLACE(r.ResourceValue, 'nopCommerce', CASE WHEN {isArabic} THEN N'محجر' ELSE N'MHGR' END)
FROM LocaleStringResource r
JOIN Language l ON l.Id = r.LanguageId
WHERE r.ResourceName IN (
    'admin.system.systeminfo.nopversion',
    'admin.system.systeminfo.nopversion.hint',
    'admin.configuration.settings.generalcommon.captcha.instructions',
    'admin.configuration.appsettings.weboptimizer.enablecssbundling.hint',
    'admin.configurationsteps.product.details.text',
    'admin.system.maintenance.backupdatabase.description',
    'admin.configuration.plugins.description.step1'
);");

        //3) hide the "Powered by nopCommerce" link in the admin & storefront footers
        Execute.Sql(@"
UPDATE [Setting] SET [Value] = 'True'
WHERE [Name] = 'StoreInformationSettings.HidePoweredByNopCommerce';");
    }
}
