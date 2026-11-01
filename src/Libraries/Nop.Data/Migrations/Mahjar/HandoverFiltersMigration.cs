// [Mahjar handover]
using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// The search box and the extra filters of the handover-desk queues: a keyword (item, customer, order number,
/// contract number), the employee the work belongs to, how far along the check is, a date range, and overdue
/// rentals only.
///
/// NOTE: the date string below IS the migration version — it must differ from every other Mahjar migration's, or the
/// runner silently treats this one as already applied. Locale resources are cached: restart (or POST
/// /Admin/Common/ClearCache) after it runs. SQL Server dialect, matching the sibling Mahjar migrations.
/// [Mahjar handover]
/// </summary>
[NopSchemaMigration("2026-09-16 12:00:00", "Mahjar handover: keyword search and filters on the desk queues")]
public class HandoverFiltersMigration : ForwardOnlyMigration
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
    ('admin.mahjar.handover.list.keyword',        N'كلمة البحث',      N'Keyword'),
    ('admin.mahjar.handover.list.keyword.hint',   N'ابحث باسم القطعة أو رقمها التعريفي، أو باسم العميل أو بريده، أو برقم الطلب أو رقم العقد.', N'Search by item name or SKU, customer name or e-mail, order number, or contract number.'),
    ('admin.mahjar.handover.list.startdate',      N'من تاريخ',        N'From date'),
    ('admin.mahjar.handover.list.startdate.hint', N'أقدم تاريخ تريد عرضه.', N'The earliest date to show.'),
    ('admin.mahjar.handover.list.enddate',        N'إلى تاريخ',       N'To date'),
    ('admin.mahjar.handover.list.enddate.hint',   N'أحدث تاريخ تريد عرضه، ويشمل اليوم نفسه.', N'The latest date to show, that day included.'),
    ('admin.mahjar.handover.list.overdueonly',    N'المتأخرة فقط',    N'Overdue only'),
    ('admin.mahjar.handover.list.overdueonly.hint', N'اعرض فقط التأجيرات التي تجاوزت موعد إرجاعها.', N'Show only rentals whose return date has already passed.'),
    ('admin.mahjar.handover.list.assignee.all',   N'كل الموظفين',     N'Anyone'),
    ('admin.mahjar.handover.list.status.all',     N'كل الحالات',      N'Any status')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }
}
