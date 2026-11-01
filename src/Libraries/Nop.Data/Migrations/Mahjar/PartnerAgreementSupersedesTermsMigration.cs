// [Mahjar partner-agreement]
using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Wording only. Once the master partner agreements are installed, the old eight-clause
/// "شروط وأحكام الشركاء" body (<c>vendorsettings.termsofservice</c>) is no longer what an applicant reads —
/// it is only the fallback for a store that switches the agreements off. Its editor is therefore hidden on
/// Admin -> Settings -> Vendor settings while they are enabled, and this migration supplies the note shown in
/// its place, plus a hint on the "الشروط والأحكام" switch saying what it now gates: acceptance itself, for the
/// agreements as much as for the fallback text. Turning it off means nothing is required and nothing is
/// recorded. [Mahjar partner-agreement]
/// </summary>
[NopSchemaMigration("2026-09-20 11:00:00", "Mahjar partner agreement: the short partner terms are superseded by the master agreements")]
public class PartnerAgreementSupersedesTermsMigration : ForwardOnlyMigration
{
    /// <summary>The Arabic-language predicate every Mahjar resource migration uses.</summary>
    private const string IS_ARABIC = "(l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')";

    /// <summary>The T-SQL-only dialect guard every Mahjar migration uses.</summary>
    private const string DATABASE_TYPE = "sqlserver";

    public override void Up()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN " + IS_ARABIC + @" THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    ('Admin.Configuration.Settings.Vendor.TermsOfService.Superseded',
     N'حلّت اتفاقية الشريك محل هذا النص، وهو يُعرض على مقدم الطلب فقط إذا أُوقفت الاتفاقية من بطاقة ""اتفاقية الشريك"" أدناه. النص محفوظ كما هو.',
     'The partner agreement has replaced this text. It is shown to an applicant only if the agreement is switched off on the ""Partner agreement"" card below. The text itself is kept as it is.')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");

        //the switch gates ACCEPTANCE, not just the old body — say so, but only while the resource still carries
        //the text nopCommerce seeded, so a store owner's own wording is never overwritten
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
UPDATE r
SET r.ResourceValue = CASE WHEN " + IS_ARABIC + @"
    THEN N'يشترط على مقدم الطلب الموافقة قبل الإرسال، وتُسجَّل موافقته. يسري على اتفاقية الشريك وعلى نص الشروط المختصر معًا: عند الإيقاف لا يُطلب قبول ولا يُسجَّل.'
    ELSE N'The applicant must accept before submitting, and their acceptance is recorded. It covers the partner agreement and the short terms body alike: when it is off, no acceptance is asked for and none is recorded.'
    END
FROM LocaleStringResource r
JOIN Language l ON l.Id = r.LanguageId
WHERE r.ResourceName = 'Admin.Configuration.Settings.Vendor.TermsOfServiceEnabled.Hint'
  AND r.ResourceValue NOT LIKE '%" + "اتفاقية الشريك" + @"%'
  AND r.ResourceValue NOT LIKE '%partner agreement%';");
    }
}
