// [Mahjar i18n]
using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Corrects Arabic UI strings that came wrong in the stock language pack and are visible to customers — the storefront
/// "Print" button read «طشركاء» (not a word), "Return Item(s)" named returned products instead of asking for a return,
/// and a few strings were missing their hamza/tanween. Each update is guarded on the exact wrong value, so a string
/// the store has already re-worded is never touched.
///
/// SQL Server dialect, matching the sibling Mahjar migrations. NOTE: locale resources are cached — restart (or POST
/// /Admin/Common/ClearCache) after it runs. [Mahjar i18n]
/// </summary>
//NOTE: the date string IS the migration version. "2026-09-16 10:00:00" is already taken by HandoverMigration, and a
//duplicate version is silently treated as already applied — the migration then never runs. Keep every date unique.
[NopSchemaMigration("2026-09-16 11:00:00", "Mahjar i18n: fix mistranslated Arabic UI strings (Print, Return items, checkout)")]
public class ArabicWordingFixesMigration : ForwardOnlyMigration
{
    /// <summary>The Arabic-language predicate every Mahjar resource migration uses.</summary>
    private const string IS_ARABIC = "(l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')";

    /// <summary>The T-SQL-only dialect guard every Mahjar migration uses.</summary>
    private const string DATABASE_TYPE = "sqlserver";

    public override void Up()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
UPDATE r
SET r.ResourceValue = v.NewValue
FROM LocaleStringResource r
JOIN Language l ON l.Id = r.LanguageId
JOIN (VALUES
    -- the order details button read as gibberish
    ('order.print',                     N'طشركاء',                  N'طباعة'),
    -- this button STARTS a return request, it does not list returned products
    ('order.returnitems',               N'المنتجات المرتجعة',        N'إرجاع المنتجات'),
    -- «الي» is the preposition «إلى», and the natural order is «العنوان نفسه»
    ('checkout.shiptosameaddress',      N'اشحن الي نفس العنوان',     N'الشحن إلى العنوان نفسه'),
    -- «او» needs its hamza; the doubled wording adds nothing
    ('checkout.paymentmethod',          N'طريقة الدفع او السداد',    N'طريقة الدفع'),
    ('checkout.thankyou',               N'شكرا لك',                 N'شكرًا لك'),
    -- the admin button sets the order to paid
    ('admin.orders.fields.markaspaid',  N'علامة كمدفع',             N'تعيين كمدفوع')
) AS v (ResourceName, OldValue, NewValue)
    ON v.ResourceName = r.ResourceName
WHERE " + IS_ARABIC + @"
  AND r.ResourceValue = v.OldValue;");
    }
}
