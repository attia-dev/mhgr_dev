// [Mahjar payments]
using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Turns the simulated payment method into a completed payment: an order placed with "Check / Money Order" is marked
/// as paid the moment it is placed, so the customer is notified and the handover desk gets its task without an admin
/// pressing "Mark as paid" (the behaviour itself lives in the plugin's OrderPlacedEventConsumer).
///
/// The plugin is already installed here, so its own InstallAsync no longer runs: this seeds the setting row and the
/// labels of its toggle (English + Arabic). Idempotent (NOT EXISTS on every insert). SQL Server dialect, matching the
/// sibling Mahjar migrations. NOTE: settings and locale resources are cached — restart (or POST
/// /Admin/Common/ClearCache) after it runs. [Mahjar payments]
/// </summary>
[NopSchemaMigration("2026-09-16 10:30:00", "Mahjar payments: the simulated method marks orders as paid on checkout")]
public class CheckMoneyOrderAutoPaidMigration : ForwardOnlyMigration
{
    /// <summary>The Arabic-language predicate every Mahjar resource migration uses.</summary>
    private const string IS_ARABIC = "(l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')";

    /// <summary>The T-SQL-only dialect guard every Mahjar migration uses.</summary>
    private const string DATABASE_TYPE = "sqlserver";

    public override void Up()
    {
        //the setting, for all stores (StoreId 0); the plugin's C# default is the same, this makes it visible/editable
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
INSERT INTO Setting ([Name], [Value], [StoreId])
SELECT 'checkmoneyorderpaymentsettings.markordersaspaidautomatically', 'True', 0
WHERE NOT EXISTS (
    SELECT 1 FROM Setting
    WHERE [Name] = 'checkmoneyorderpaymentsettings.markordersaspaidautomatically' AND [StoreId] = 0
);");

        //the labels of the toggle on the plugin's configuration page
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN " + IS_ARABIC + @" THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    ('Plugins.Payment.CheckMoneyOrder.MarkOrdersAsPaidAutomatically',      N'تعيين الطلبات كمدفوعة تلقائيًا', N'Mark orders as paid automatically'),
    ('Plugins.Payment.CheckMoneyOrder.MarkOrdersAsPaidAutomatically.Hint', N'اعتبر إتمام العميل للطلب دفعًا مكتملًا: يُعلَّم الطلب كمدفوع فور إنشائه، فيصل العميل إشعار استلام الدفعة ويصل مكتب التسليم طلبه دون أن يضغط أحد «تعيين كمدفوع». أوقف هذا الخيار عند ربط بوابة دفع حقيقية.', N'Treat placing the order as a completed payment: the order is marked as paid at once, so the customer is notified and the handover desk gets its task without anyone pressing ""Mark as paid"". Turn this off once a real payment gateway is in place.')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }
}
