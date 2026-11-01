// [Mahjar notifications]
using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Adds the bilingual (en + ar) locale resources for the two become-a-seller notification types:
/// NewVendorApplication (admin) and SellerApproved (buyer). Separate from NotificationsMigration because that
/// one has already been applied; idempotent + SQL Server dialect. [Mahjar notifications]
/// </summary>
[NopSchemaMigration("2026-06-29 10:00:00", "Mahjar notifications: become-a-seller resources (application + approval)")]
public class NotificationSellerResourcesMigration : ForwardOnlyMigration
{
    public override void Up()
    {
        IfDatabase("sqlserver").Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN (l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%') THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    ('mahjar.notifications.type.newvendorapplication.title', N'طلب بائع جديد',                        N'New seller application'),
    ('mahjar.notifications.type.newvendorapplication.body',  N'تقدّم «{0}» بطلب ليصبح بائعًا.',          N'""{0}"" applied to become a seller.'),
    ('mahjar.notifications.type.sellerapproved.title',       N'تمت الموافقة على طلبك',                  N'Application approved'),
    ('mahjar.notifications.type.sellerapproved.body',        N'تهانينا! تمت الموافقة على متجرك «{0}» كبائع في محجر.', N'Congratulations! Your seller store ""{0}"" has been approved on MHGR.')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }
}
