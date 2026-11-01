// [Mahjar notifications]
using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Adds the bilingual (en + ar) locale resources for the SellerDeactivated notification — sent to a seller when
/// an admin suspends/deactivates their account (Active true → false), the mirror of SellerApproved. Idempotent +
/// SQL Server dialect, matching the sibling Mahjar notification migrations. Uses the "partner/شريك" wording per
/// the terminology rebrand. [Mahjar notifications]
/// </summary>
[NopSchemaMigration("2026-06-29 13:00:00", "Mahjar notifications: seller-deactivated resources")]
public class SellerDeactivatedResourcesMigration : ForwardOnlyMigration
{
    public override void Up()
    {
        IfDatabase("sqlserver").Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN (l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%') THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    ('mahjar.notifications.type.sellerdeactivated.title', N'تم إيقاف حسابك كشريك',                                            N'Your partner account was deactivated'),
    ('mahjar.notifications.type.sellerdeactivated.body',  N'تم إيقاف تفعيل متجرك «{0}» في محجر. تواصل مع الإدارة لمزيد من المعلومات.', N'Your store ""{0}"" on MHGR has been deactivated. Please contact us for details.')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }
}
