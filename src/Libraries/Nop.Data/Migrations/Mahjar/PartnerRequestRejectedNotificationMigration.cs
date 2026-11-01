// [Mahjar partner-request]
using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// The wording of the notification an applicant gets when their partner application is refused.
///
/// Approval already had one — it rides on <c>VendorApprovedEvent</c>, which fires when the partner is activated.
/// Rejection had none: a pending application was never active, so the deactivation event never fired for it, and
/// nothing told the applicant. The body carries the partner name and the reason, because the point of telling
/// them is so they can correct it and apply again.
///
/// Idempotent, SQL Server dialect. NOTE: locale resources are cached — restart (or POST /Admin/Common/ClearCache)
/// after it runs. [Mahjar partner-request]
/// </summary>
[NopSchemaMigration("2026-09-22 10:00:00", "Mahjar partner request: the applicant is told when their request is refused")]
public class PartnerRequestRejectedNotificationMigration : ForwardOnlyMigration
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
    ('mahjar.notifications.type.sellerrejected.title',    N'لم يُقبل طلب الانضمام',
     'Your application was not accepted'),
    ('mahjar.notifications.type.sellerrejected.body',     N'لم يُقبل طلب انضمامك كشريك باسم «{0}». السبب: {1}. يمكنك تصحيح البيانات والتقدم بطلب جديد.',
     'Your application to become a partner as ""{0}"" was not accepted. Reason: {1}. You can correct the details and apply again.'),
    ('mahjar.notifications.type.sellerrejected.noreason', N'لم يُذكر سبب',
     'no reason was given')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }
}
