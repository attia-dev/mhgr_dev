// [Mahjar contracts]
using FluentMigrator;
using Nop.Core.Domain.Mahjar;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Splits the single tokenized contract template into one template per <see cref="ContractTemplateType"/>, so a
/// buy-only cart is no longer sent a contract full of rental clauses (and vice versa):
///
/// 1) Adds <see cref="ContractTemplate.TemplateTypeId"/> and re-labels the existing combined template as the
///    <see cref="ContractTemplateType.Mixed"/> one — its wording already covers both cases, and its Arabic
///    <c>LocalizedProperty</c> body rides along untouched because that row is keyed by EntityId, not by Name.
/// 2) Seeds a <see cref="ContractTemplateType.Purchase"/> template (delivery, transfer of ownership, warranty,
///    returns — no rental period, no deposit) and a <see cref="ContractTemplateType.Rental"/> template (rental
///    period, return, security deposit, liability, late return, no sub-letting), each with an English Body and an
///    Arabic <c>LocalizedProperty</c> body.
/// 3) Seeds the locale resources for the new Admin -> Contracts -> Contract templates list/edit screens and the
///    contract-kind labels, and re-points the menu entry's label at the now-plural screen.
///
/// The contract body is rendered from the template whose kind matches the request's frozen cart snapshot (falling
/// back to Mixed), so contracts already frozen into <see cref="ContractRequest.ContractBody"/> are untouched here.
///
/// Idempotent throughout (column guard, NOT EXISTS on every insert, value-guarded updates). Schema migration so it
/// auto-applies on startup against an already-installed DB. SQL Server dialect, matching the sibling Mahjar
/// migrations. NOTE: locale resources are cached — restart (or POST /Admin/Common/ClearCache) after it runs.
/// [Mahjar contracts]
/// </summary>
[NopSchemaMigration("2026-09-09 11:00:00", "Mahjar contracts: separate purchase / rental / mixed contract templates")]
public class ContractTemplateTypesMigration : ForwardOnlyMigration
{
    /// <summary>The Arabic-language predicate every Mahjar resource migration uses.</summary>
    private const string IS_ARABIC = "(l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')";

    /// <summary>The T-SQL-only dialect guard every Mahjar migration uses.</summary>
    private const string DATABASE_TYPE = "sqlserver";

    public override void Up()
    {
        AddTemplateTypeColumn();
        ReclassifyExistingTemplateAsMixed();
        SeedPurchaseAndRentalTemplates();
        SeedArabicTemplateBodies();
        AddLocaleResources();
    }

    #region Utilities

    /// <summary>1) The kind discriminator. Pre-existing rows keep the combined sales/rental wording → Mixed.</summary>
    protected virtual void AddTemplateTypeColumn()
    {
        var templateTable = nameof(ContractTemplate);

        if (!Schema.Table(templateTable).Column(nameof(ContractTemplate.TemplateTypeId)).Exists())
        {
            Alter.Table(templateTable)
                .AddColumn(nameof(ContractTemplate.TemplateTypeId))
                .AsInt32()
                .NotNullable()
                .WithDefaultValue((int)ContractTemplateType.Mixed);
        }
    }

    /// <summary>
    /// 2) Re-label the seeded 'Default contract' as the mixed one. Guarded on the seeded name so an admin who has
    /// already renamed it keeps their own name; any other pre-existing row keeps the combined wording too.
    /// </summary>
    protected virtual void ReclassifyExistingTemplateAsMixed()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
UPDATE ContractTemplate
SET [Name] = 'Mixed contract', [TemplateTypeId] = 30
WHERE [Name] = 'Default contract';

UPDATE ContractTemplate SET [TemplateTypeId] = 30 WHERE [TemplateTypeId] = 0;");
    }

    /// <summary>3) The purchase-only and rental-only templates (English Body).</summary>
    protected virtual void SeedPurchaseAndRentalTemplates()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
INSERT INTO ContractTemplate ([Name], [TemplateTypeId], [Body], [UpdatedOnUtc])
SELECT v.[Name], v.[TemplateTypeId], v.[Body], GETUTCDATE()
FROM (VALUES
(
    'Purchase contract', 10,
    N'<div style=""font-family:Segoe UI,Tahoma,Arial,sans-serif;max-width:800px;margin:0 auto;color:#222;line-height:1.7"">
<h1 style=""text-align:center;margin-bottom:0"">%Store.Name%</h1>
<h2 style=""text-align:center;margin-top:4px"">Sales Contract %Contract.Number%</h2>
<p style=""text-align:center;color:#666"">Date: %Contract.Date%</p>
<h3>Parties</h3>
<p>This contract is entered into between <strong>%Store.Name%</strong> (the First Party &mdash; the platform) and <strong>%Customer.FullName%</strong> (%Customer.Email%) (the Second Party &mdash; the customer).</p>
<h3>Subject of the contract</h3>
<p>The Second Party requests to purchase the items listed below under the terms of this contract:</p>
%Contract.ItemsTable%
<h3>Terms and conditions</h3>
<ol>
<li><strong>Payment.</strong> The full contract amount is due upon approval of this contract; the order is processed only after payment is completed.</li>
<li><strong>Delivery.</strong> The purchased items are delivered to the address specified by the Second Party in accordance with the shipping policy in force on the platform.</li>
<li><strong>Transfer of ownership.</strong> Ownership of the purchased items passes to the Second Party upon completion of payment and receipt of the items.</li>
<li><strong>Item condition and warranty.</strong> The items are delivered in the condition described on their product pages, together with the selling partner''s warranty where one is offered.</li>
<li><strong>Exchange and return.</strong> Exchanging or returning purchased items is subject to the return policy in force on the platform.</li>
<li><strong>Governing law.</strong> This contract is governed by the laws and regulations in force in the Kingdom of Saudi Arabia.</li>
</ol>
<h3>Totals</h3>
<p>Subtotal: %Contract.SubTotal%<br /><strong>Grand total: %Contract.GrandTotal%</strong><br /><span style=""color:#666"">Shipping fees and taxes, if any, are finalized at checkout.</span></p>
<p style=""margin-top:24px"">Signed electronically via the MHGR platform.</p>
</div>'
),
(
    'Rental contract', 20,
    N'<div style=""font-family:Segoe UI,Tahoma,Arial,sans-serif;max-width:800px;margin:0 auto;color:#222;line-height:1.7"">
<h1 style=""text-align:center;margin-bottom:0"">%Store.Name%</h1>
<h2 style=""text-align:center;margin-top:4px"">Rental Contract %Contract.Number%</h2>
<p style=""text-align:center;color:#666"">Date: %Contract.Date%</p>
<h3>Parties</h3>
<p>This contract is entered into between <strong>%Store.Name%</strong> (the First Party &mdash; the platform) and <strong>%Customer.FullName%</strong> (%Customer.Email%) (the Second Party &mdash; the customer).</p>
<h3>Subject of the contract</h3>
<p>The Second Party requests to rent the items listed below, for the rental period stated against each of them, under the terms of this contract:</p>
%Contract.ItemsTable%
<h3>Terms and conditions</h3>
<ol>
<li><strong>Payment.</strong> The full contract amount, including the security deposit, is due upon approval of this contract; the order is processed only after payment is completed.</li>
<li><strong>Rental period and return.</strong> The Second Party shall observe the rental period stated for each rented item and return every rented item at the end of its period in the same condition in which it was received.</li>
<li><strong>Security deposit.</strong> A refundable security deposit of %Contract.DepositTotal% is collected for the rented items and is returned in full upon their intact, damage-free return.</li>
<li><strong>Item condition and liability.</strong> The Second Party is responsible for any loss of, or damage to, the rented items during the rental period; the value of any damage may be deducted from the security deposit.</li>
<li><strong>Late return.</strong> Returning a rented item after the end of its rental period incurs a late fee for each day of delay, which may be deducted from the security deposit.</li>
<li><strong>No sub-letting.</strong> The Second Party may not sub-let the rented items or assign them to any third party.</li>
<li><strong>Ownership.</strong> The rented items remain the property of the First Party or of the renting partner at all times; renting them conveys no ownership of any kind.</li>
<li><strong>Governing law.</strong> This contract is governed by the laws and regulations in force in the Kingdom of Saudi Arabia.</li>
</ol>
<h3>Totals</h3>
<p>Rental subtotal: %Contract.SubTotal%<br />Security deposits: %Contract.DepositTotal%<br /><strong>Grand total: %Contract.GrandTotal%</strong><br /><span style=""color:#666"">Shipping fees and taxes, if any, are finalized at checkout.</span></p>
<p style=""margin-top:24px"">Signed electronically via the MHGR platform.</p>
</div>'
)
) AS v ([Name], [TemplateTypeId], [Body])
WHERE NOT EXISTS (SELECT 1 FROM ContractTemplate t WHERE t.[TemplateTypeId] = v.[TemplateTypeId]);");
    }

    /// <summary>4) Arabic bodies for the two new templates — one LocalizedProperty row per ar language.</summary>
    protected virtual void SeedArabicTemplateBodies()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
INSERT INTO LocalizedProperty (EntityId, LanguageId, LocaleKeyGroup, LocaleKey, LocaleValue)
SELECT t.Id, l.Id, 'ContractTemplate', 'Body', v.LocaleValue
FROM (VALUES
(
    10,
    N'<div dir=""rtl"" style=""font-family:Segoe UI,Tahoma,Arial,sans-serif;max-width:800px;margin:0 auto;color:#222;line-height:1.9;text-align:right"">
<h1 style=""text-align:center;margin-bottom:0"">%Store.Name%</h1>
<h2 style=""text-align:center;margin-top:4px"">عقد بيع رقم %Contract.Number%</h2>
<p style=""text-align:center;color:#666"">التاريخ: %Contract.Date%</p>
<h3>أطراف العقد</h3>
<p>أُبرم هذا العقد بين <strong>%Store.Name%</strong> (الطرف الأول &mdash; المنصة) و<strong>%Customer.FullName%</strong> (%Customer.Email%) (الطرف الثاني &mdash; العميل).</p>
<h3>موضوع العقد</h3>
<p>يطلب الطرف الثاني شراء الأصناف المبينة أدناه وفقًا لأحكام هذا العقد:</p>
%Contract.ItemsTable%
<h3>الشروط والأحكام</h3>
<ol>
<li><strong>الدفع.</strong> يستحق سداد كامل قيمة العقد فور اعتماده، ولا يُنفذ الطلب إلا بعد إتمام الدفع.</li>
<li><strong>التسليم.</strong> تُسلَّم الأصناف المشتراة إلى العنوان الذي يحدده الطرف الثاني وفق سياسة الشحن المعمول بها في المنصة.</li>
<li><strong>انتقال الملكية.</strong> تنتقل ملكية الأصناف المشتراة إلى الطرف الثاني عند إتمام الدفع واستلام الأصناف.</li>
<li><strong>حالة الأصناف والضمان.</strong> تُسلَّم الأصناف بالحالة الموصوفة في صفحاتها، ويسري عليها ضمان الشريك البائع إن وُجد.</li>
<li><strong>الاستبدال والاسترجاع.</strong> يخضع استبدال الأصناف المشتراة أو استرجاعها لسياسة الاسترجاع المعمول بها في المنصة.</li>
<li><strong>القانون الواجب التطبيق.</strong> يخضع هذا العقد للأنظمة واللوائح المعمول بها في المملكة العربية السعودية.</li>
</ol>
<h3>الإجماليات</h3>
<p>المجموع الفرعي: %Contract.SubTotal%<br /><strong>الإجمالي الكلي: %Contract.GrandTotal%</strong><br /><span style=""color:#666"">تُحدد رسوم الشحن والضرائب، إن وُجدت، عند إتمام الشراء.</span></p>
<p style=""margin-top:24px"">وُقّع هذا العقد إلكترونيًا عبر منصة محجر MHGR.</p>
</div>'
),
(
    20,
    N'<div dir=""rtl"" style=""font-family:Segoe UI,Tahoma,Arial,sans-serif;max-width:800px;margin:0 auto;color:#222;line-height:1.9;text-align:right"">
<h1 style=""text-align:center;margin-bottom:0"">%Store.Name%</h1>
<h2 style=""text-align:center;margin-top:4px"">عقد إيجار رقم %Contract.Number%</h2>
<p style=""text-align:center;color:#666"">التاريخ: %Contract.Date%</p>
<h3>أطراف العقد</h3>
<p>أُبرم هذا العقد بين <strong>%Store.Name%</strong> (الطرف الأول &mdash; المنصة) و<strong>%Customer.FullName%</strong> (%Customer.Email%) (الطرف الثاني &mdash; العميل).</p>
<h3>موضوع العقد</h3>
<p>يطلب الطرف الثاني استئجار الأصناف المبينة أدناه، لمدة الإيجار المحددة أمام كل صنف منها، وفقًا لأحكام هذا العقد:</p>
%Contract.ItemsTable%
<h3>الشروط والأحكام</h3>
<ol>
<li><strong>الدفع.</strong> يستحق سداد كامل قيمة العقد، شاملًا مبلغ التأمين، فور اعتماده، ولا يُنفذ الطلب إلا بعد إتمام الدفع.</li>
<li><strong>مدة الإيجار والإرجاع.</strong> يلتزم الطرف الثاني بالتقيد بمدة الإيجار المبينة لكل صنف مؤجر، وبإعادة كل صنف مؤجر في نهاية مدته بالحالة التي استلمه بها.</li>
<li><strong>مبلغ التأمين.</strong> يُحصَّل مبلغ تأمين مسترد قدره %Contract.DepositTotal% عن الأصناف المؤجرة، ويُعاد كاملًا عند إرجاعها سليمة وخالية من الأضرار.</li>
<li><strong>المسؤولية عن حالة الأصناف.</strong> يتحمل الطرف الثاني المسؤولية عن أي فقدان أو تلف يلحق بالأصناف المؤجرة خلال مدة الإيجار، ويجوز خصم قيمة الأضرار من مبلغ التأمين.</li>
<li><strong>التأخر في الإرجاع.</strong> يترتب على إرجاع أي صنف مؤجر بعد نهاية مدة إيجاره رسوم تأخير عن كل يوم تأخير، ويجوز خصمها من مبلغ التأمين.</li>
<li><strong>عدم جواز التأجير من الباطن.</strong> لا يجوز للطرف الثاني تأجير الأصناف المؤجرة من الباطن أو التنازل عنها للغير.</li>
<li><strong>الملكية.</strong> تظل الأصناف المؤجرة مملوكة للطرف الأول أو للشريك المؤجِّر في جميع الأوقات، ولا يترتب على استئجارها أي حق ملكية.</li>
<li><strong>القانون الواجب التطبيق.</strong> يخضع هذا العقد للأنظمة واللوائح المعمول بها في المملكة العربية السعودية.</li>
</ol>
<h3>الإجماليات</h3>
<p>إجمالي أجرة الإيجار: %Contract.SubTotal%<br />إجمالي مبالغ التأمين: %Contract.DepositTotal%<br /><strong>الإجمالي الكلي: %Contract.GrandTotal%</strong><br /><span style=""color:#666"">تُحدد رسوم الشحن والضرائب، إن وُجدت، عند إتمام الشراء.</span></p>
<p style=""margin-top:24px"">وُقّع هذا العقد إلكترونيًا عبر منصة محجر MHGR.</p>
</div>'
)
) AS v (TemplateTypeId, LocaleValue)
JOIN ContractTemplate t ON t.[TemplateTypeId] = v.TemplateTypeId
CROSS JOIN Language l
WHERE " + IS_ARABIC + @"
  AND NOT EXISTS (
      SELECT 1 FROM LocalizedProperty lp
      WHERE lp.EntityId = t.Id AND lp.LanguageId = l.Id
        AND lp.LocaleKeyGroup = 'ContractTemplate' AND lp.LocaleKey = 'Body');");
    }

    /// <summary>5) Locale resources for the templates list/edit screens + the contract-kind labels.</summary>
    protected virtual void AddLocaleResources()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN " + IS_ARABIC + @" THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    ('admin.mahjar.contracts.template.list.title',       N'نماذج العقود',             'Contract templates'),
    ('admin.mahjar.contracts.template.list.hint',        N'يُرسَل إلى العميل النموذج المطابق لمحتوى سلته: نموذج البيع للسلال التي تحتوي على أصناف مشتراة فقط، ونموذج الإيجار للسلال التي تحتوي على أصناف مؤجرة فقط، ونموذج البيع والإيجار للسلال التي تجمع بينهما.', N'The customer is sent the template that matches their cart: the purchase template for carts holding only bought items, the rental template for carts holding only rented items, and the purchase + rental template for carts that mix both.'),
    ('admin.mahjar.contracts.template.edit.title',       N'تعديل نموذج العقد',        'Edit contract template'),
    ('admin.mahjar.contracts.template.backtolist',       N'العودة إلى قائمة النماذج', 'back to template list'),
    ('admin.mahjar.contracts.template.fields.name',      N'الاسم',                    'Name'),
    ('admin.mahjar.contracts.template.fields.type',      N'نوع العقد',                'Contract kind'),
    ('admin.mahjar.contracts.template.fields.type.hint', N'يحدد متى يُستخدم هذا النموذج، بحسب محتوى سلة العميل.', N'Determines when this template is used, based on the contents of the customer''s cart.'),
    ('admin.mahjar.contracts.template.fields.updatedon', N'آخر تحديث',                'Updated on'),
    ('admin.mahjar.contracts.template.type.purchase',    N'بيع',                      'Purchase'),
    ('admin.mahjar.contracts.template.type.rental',      N'إيجار',                    'Rental'),
    ('admin.mahjar.contracts.template.type.mixed',       N'بيع وإيجار',               'Purchase + rental'),
    ('admin.mahjar.contracts.template.notfound',         N'لم يُعثر على نموذج العقد.', 'The contract template was not found.'),
    ('admin.mahjar.contracts.fields.kind',               N'نوع العقد',                'Contract kind'),
    ('admin.mahjar.contracts.fields.kind.hint',          N'النموذج الذي سيُستخدم عند إرسال هذا العقد، بحسب الأصناف المطلوبة.', 'The template that will be used when this contract is sent, based on the requested items.')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");

        //the menu entry now opens a list of templates, so re-label it — but only while it still holds the
        //seeded singular text, so an admin's own wording is never overwritten.
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
UPDATE r
SET r.ResourceValue = CASE WHEN " + IS_ARABIC + @" THEN N'نماذج العقود' ELSE 'Contract templates' END
FROM LocaleStringResource r
JOIN Language l ON l.Id = r.LanguageId
WHERE r.ResourceName = 'admin.mahjar.contracts.menu.template'
  AND r.ResourceValue IN (N'نموذج العقد', 'Contract template');");
    }

    #endregion
}
