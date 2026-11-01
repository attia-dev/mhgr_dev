// [Mahjar contracts]
using FluentMigrator;
using Nop.Core.Domain.Mahjar;
using Nop.Data.Extensions;
using Nop.Data.Mapping;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Creates the contract-approval stage before payment: the <see cref="ContractRequest"/> +
/// <see cref="ContractTemplate"/> tables (with a composite index on the hot active-request query), the
/// bilingual default contract template, the five contract message templates (en + ar), the
/// <c>ContractSettings</c> setting rows, and every bilingual (en + ar) <c>mahjar.contracts.*</c> /
/// <c>admin.mahjar.contracts.*</c> / <c>mahjar.api.contracts.*</c> / notification-type locale resource.
///
/// Idempotent (tables via FluentMigrator's version table; every seed row inserted only when missing), so it
/// is deploy-safe, and a schema migration so it auto-applies on startup. SQL Server dialect.
/// [Mahjar contracts]
/// </summary>
[NopSchemaMigration("2026-09-03 10:00:00", "Mahjar contracts: contract-approval stage before payment")]
public class ContractsMigration : ForwardOnlyMigration
{
    public override void Up()
    {
        //1) schema — create the two tables via the LinqToDB builders. FluentMigrator's version table guards
        //   against re-runs, so no Schema.Table(...).Exists() guard is needed (and that guard misbehaves on the
        //   SQLite test provider, silently skipping creation — matching the base SchemaMigration which also
        //   creates tables unconditionally).
        Create.TableFor<ContractRequest>();
        Create.TableFor<ContractTemplate>();

        //2) composite index for the active-request / checkout-gate hot path (FK conventions alone don't cover it).
        //   SQL Server only — the SQLite test DB doesn't need it.
        const string databaseType = "sqlserver";
        var contractRequestTable = NameCompatibilityManager.GetTableName(typeof(ContractRequest));
        if (!Schema.Table(contractRequestTable).Index("IX_ContractRequest_Customer_Status").Exists())
            IfDatabase(databaseType).Create.Index("IX_ContractRequest_Customer_Status").OnTable(contractRequestTable)
                .OnColumn(nameof(ContractRequest.CustomerId)).Ascending()
                .OnColumn(nameof(ContractRequest.StatusId)).Ascending();

        //3) the default contract template (en Body; the Body is localized via LocalizedProperty).
        //   T-SQL dialect (N'…' literals) → SQL Server only; harmless to skip on the SQLite test DB.
        //   [Mahjar contracts] TemplateTypeId is listed explicitly because on a FRESH install the builder above
        //   already creates the (NOT NULL, no default) column that ContractTemplateTypesMigration adds to
        //   already-installed DBs — this row is the combined sales/rental one, i.e. ContractTemplateType.Mixed (30),
        //   and that later migration renames it to 'Mixed contract'.
        IfDatabase(databaseType).Execute.Sql(@"
INSERT INTO ContractTemplate ([Name], [TemplateTypeId], [Body], [UpdatedOnUtc])
SELECT 'Default contract', 30, N'<div style=""font-family:Segoe UI,Tahoma,Arial,sans-serif;max-width:800px;margin:0 auto;color:#222;line-height:1.7"">
<h1 style=""text-align:center;margin-bottom:0"">%Store.Name%</h1>
<h2 style=""text-align:center;margin-top:4px"">Sales / Rental Contract %Contract.Number%</h2>
<p style=""text-align:center;color:#666"">Date: %Contract.Date%</p>
<h3>Parties</h3>
<p>This contract is entered into between <strong>%Store.Name%</strong> (the First Party &mdash; the platform) and <strong>%Customer.FullName%</strong> (%Customer.Email%) (the Second Party &mdash; the customer).</p>
<h3>Subject of the contract</h3>
<p>The Second Party requests to purchase and/or rent the items listed below under the terms of this contract:</p>
%Contract.ItemsTable%
<h3>Terms and conditions</h3>
<ol>
<li><strong>Payment.</strong> The full contract amount is due upon approval of this contract; the order is processed only after payment is completed.</li>
<li><strong>Rental period and return.</strong> The Second Party shall observe the rental period stated for each rented item and return every rented item at the end of its period in the same condition in which it was received.</li>
<li><strong>Security deposit.</strong> A refundable security deposit of %Contract.DepositTotal% is collected for the rented items and is returned in full upon their intact, damage-free return.</li>
<li><strong>Item condition.</strong> The Second Party is responsible for any loss of, or damage to, the rented items during the rental period; the value of any damage may be deducted from the security deposit.</li>
<li><strong>Governing law.</strong> This contract is governed by the laws and regulations in force in the Kingdom of Saudi Arabia.</li>
</ol>
<h3>Totals</h3>
<p>Subtotal: %Contract.SubTotal%<br />Security deposits: %Contract.DepositTotal%<br /><strong>Grand total: %Contract.GrandTotal%</strong><br /><span style=""color:#666"">Shipping fees and taxes, if any, are finalized at checkout.</span></p>
<p style=""margin-top:24px"">Signed electronically via the MHGR platform.</p>
</div>', GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ContractTemplate WHERE [Name] = 'Default contract');");

        //4) Arabic translation of the template body — one LocalizedProperty row per ar language.
        IfDatabase(databaseType).Execute.Sql(@"
INSERT INTO LocalizedProperty (EntityId, LanguageId, LocaleKeyGroup, LocaleKey, LocaleValue)
SELECT t.Id, l.Id, 'ContractTemplate', 'Body', N'<div dir=""rtl"" style=""font-family:Segoe UI,Tahoma,Arial,sans-serif;max-width:800px;margin:0 auto;color:#222;line-height:1.9;text-align:right"">
<h1 style=""text-align:center;margin-bottom:0"">%Store.Name%</h1>
<h2 style=""text-align:center;margin-top:4px"">عقد بيع / إيجار رقم %Contract.Number%</h2>
<p style=""text-align:center;color:#666"">التاريخ: %Contract.Date%</p>
<h3>أطراف العقد</h3>
<p>أُبرم هذا العقد بين <strong>%Store.Name%</strong> (الطرف الأول &mdash; المنصة) و<strong>%Customer.FullName%</strong> (%Customer.Email%) (الطرف الثاني &mdash; العميل).</p>
<h3>موضوع العقد</h3>
<p>يطلب الطرف الثاني شراء و/أو استئجار الأصناف المبينة أدناه وفقًا لأحكام هذا العقد:</p>
%Contract.ItemsTable%
<h3>الشروط والأحكام</h3>
<ol>
<li><strong>الدفع.</strong> يستحق سداد كامل قيمة العقد فور اعتماده، ولا يُنفذ الطلب إلا بعد إتمام الدفع.</li>
<li><strong>مدة الإيجار والإرجاع.</strong> يلتزم الطرف الثاني بالتقيد بمدة الإيجار المبينة لكل صنف مؤجر، وبإعادة كل صنف مؤجر في نهاية مدته بالحالة التي استلمه بها.</li>
<li><strong>مبلغ التأمين.</strong> يُحصَّل مبلغ تأمين مسترد قدره %Contract.DepositTotal% عن الأصناف المؤجرة، ويُعاد كاملًا عند إرجاعها سليمة وخالية من الأضرار.</li>
<li><strong>المسؤولية عن حالة الأصناف.</strong> يتحمل الطرف الثاني المسؤولية عن أي فقدان أو تلف يلحق بالأصناف المؤجرة خلال مدة الإيجار، ويجوز خصم قيمة الأضرار من مبلغ التأمين.</li>
<li><strong>القانون الواجب التطبيق.</strong> يخضع هذا العقد للأنظمة واللوائح المعمول بها في المملكة العربية السعودية.</li>
</ol>
<h3>الإجماليات</h3>
<p>المجموع الفرعي: %Contract.SubTotal%<br />إجمالي مبالغ التأمين: %Contract.DepositTotal%<br /><strong>الإجمالي الكلي: %Contract.GrandTotal%</strong><br /><span style=""color:#666"">تُحدد رسوم الشحن والضرائب، إن وُجدت، عند إتمام الشراء.</span></p>
<p style=""margin-top:24px"">وُقّع هذا العقد إلكترونيًا عبر منصة محجر MHGR.</p>
</div>'
FROM ContractTemplate t
CROSS JOIN Language l
WHERE t.[Name] = 'Default contract'
  AND (l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')
  AND NOT EXISTS (
      SELECT 1 FROM LocalizedProperty lp
      WHERE lp.EntityId = t.Id AND lp.LanguageId = l.Id
        AND lp.LocaleKeyGroup = 'ContractTemplate' AND lp.LocaleKey = 'Body');");

        //5) the five contract message templates (en Subject/Body; ar via LocalizedProperty below).
        IfDatabase(databaseType).Execute.Sql(@"
INSERT INTO MessageTemplate ([Name], [Subject], [Body], [IsActive], [EmailAccountId], [DelayPeriodId], [AttachedDownloadId], [AllowDirectReply], [LimitedToStores])
SELECT v.[Name], v.[Subject], v.[Body], 1, ISNULL((SELECT TOP 1 Id FROM EmailAccount ORDER BY Id), 0), 0, 0, 0, 0
FROM (VALUES
    ('Mahjar.ContractRequested.StoreOwnerNotification',
     '%Store.Name% - New contract request %Contract.Number%',
     N'<p><a href=""%Store.URL%"">%Store.Name%</a></p><p>%Customer.FullName% (%Customer.Email%) submitted a new contract request %Contract.Number% on %Contract.Date%.</p><p>Please review it in the admin area and send the contract for signing.</p>'),
    ('Mahjar.ContractSent.CustomerNotification',
     '%Store.Name% - Your contract %Contract.Number% is ready to sign',
     N'<p><a href=""%Store.URL%"">%Store.Name%</a></p><p>Hello %Customer.FullName%,</p><p>Your contract %Contract.Number% is ready for your signature.</p><p><a href=""%Contract.SignURL%"">Review and sign your contract</a></p><p>Thank you, %Store.Name%</p>'),
    ('Mahjar.ContractSigned.StoreOwnerNotification',
     '%Store.Name% - Contract %Contract.Number% signed',
     N'<p><a href=""%Store.URL%"">%Store.Name%</a></p><p>Contract %Contract.Number% was signed by %Customer.FullName% and now awaits review in the admin area.</p>'),
    ('Mahjar.ContractApproved.CustomerNotification',
     '%Store.Name% - Contract %Contract.Number% approved',
     N'<p><a href=""%Store.URL%"">%Store.Name%</a></p><p>Hello %Customer.FullName%,</p><p>Your contract %Contract.Number% has been approved. You can now complete your payment.</p><p><a href=""%Contract.SignURL%"">View your contracts</a></p>'),
    ('Mahjar.ContractRejected.CustomerNotification',
     '%Store.Name% - Contract %Contract.Number% update',
     N'<p><a href=""%Store.URL%"">%Store.Name%</a></p><p>Hello %Customer.FullName%,</p><p>We are sorry &mdash; your contract %Contract.Number% was not approved.</p><p>Reason: %Contract.RejectReason%</p>')
) AS v ([Name], [Subject], [Body])
WHERE NOT EXISTS (SELECT 1 FROM MessageTemplate mt WHERE mt.[Name] = v.[Name]);");

        //6) Arabic Subject + Body for the five message templates — one LocalizedProperty row per ar language.
        IfDatabase(databaseType).Execute.Sql(@"
INSERT INTO LocalizedProperty (EntityId, LanguageId, LocaleKeyGroup, LocaleKey, LocaleValue)
SELECT mt.Id, l.Id, 'MessageTemplate', v.LocaleKey, v.LocaleValue
FROM (VALUES
    ('Mahjar.ContractRequested.StoreOwnerNotification', 'Subject', N'%Store.Name% - طلب تعاقد جديد %Contract.Number%'),
    ('Mahjar.ContractRequested.StoreOwnerNotification', 'Body',    N'<p dir=""rtl""><a href=""%Store.URL%"">%Store.Name%</a></p><p dir=""rtl"">قدّم العميل %Customer.FullName% (%Customer.Email%) طلب تعاقد جديدًا برقم %Contract.Number% بتاريخ %Contract.Date%.</p><p dir=""rtl"">يرجى مراجعته في لوحة الإدارة وإرسال العقد للتوقيع.</p>'),
    ('Mahjar.ContractSent.CustomerNotification', 'Subject', N'%Store.Name% - عقدك %Contract.Number% جاهز للتوقيع'),
    ('Mahjar.ContractSent.CustomerNotification', 'Body',    N'<p dir=""rtl""><a href=""%Store.URL%"">%Store.Name%</a></p><p dir=""rtl"">مرحبًا %Customer.FullName%،</p><p dir=""rtl"">عقدك رقم %Contract.Number% جاهز لتوقيعك.</p><p dir=""rtl""><a href=""%Contract.SignURL%"">مراجعة العقد وتوقيعه</a></p><p dir=""rtl"">شكرًا لك، %Store.Name%</p>'),
    ('Mahjar.ContractSigned.StoreOwnerNotification', 'Subject', N'%Store.Name% - تم توقيع العقد %Contract.Number%'),
    ('Mahjar.ContractSigned.StoreOwnerNotification', 'Body',    N'<p dir=""rtl""><a href=""%Store.URL%"">%Store.Name%</a></p><p dir=""rtl"">تم توقيع العقد رقم %Contract.Number% بواسطة %Customer.FullName% وهو الآن بانتظار المراجعة في لوحة الإدارة.</p>'),
    ('Mahjar.ContractApproved.CustomerNotification', 'Subject', N'%Store.Name% - تم اعتماد العقد %Contract.Number%'),
    ('Mahjar.ContractApproved.CustomerNotification', 'Body',    N'<p dir=""rtl""><a href=""%Store.URL%"">%Store.Name%</a></p><p dir=""rtl"">مرحبًا %Customer.FullName%،</p><p dir=""rtl"">تم اعتماد عقدك رقم %Contract.Number%. يمكنك الآن إتمام عملية الدفع.</p><p dir=""rtl""><a href=""%Contract.SignURL%"">عرض عقودي</a></p>'),
    ('Mahjar.ContractRejected.CustomerNotification', 'Subject', N'%Store.Name% - تحديث بشأن العقد %Contract.Number%'),
    ('Mahjar.ContractRejected.CustomerNotification', 'Body',    N'<p dir=""rtl""><a href=""%Store.URL%"">%Store.Name%</a></p><p dir=""rtl"">مرحبًا %Customer.FullName%،</p><p dir=""rtl"">نأسف &mdash; لم يتم اعتماد عقدك رقم %Contract.Number%.</p><p dir=""rtl"">السبب: %Contract.RejectReason%</p>')
) AS v (TemplateName, LocaleKey, LocaleValue)
JOIN MessageTemplate mt ON mt.[Name] = v.TemplateName
CROSS JOIN Language l
WHERE (l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')
  AND NOT EXISTS (
      SELECT 1 FROM LocalizedProperty lp
      WHERE lp.EntityId = mt.Id AND lp.LanguageId = l.Id
        AND lp.LocaleKeyGroup = 'MessageTemplate' AND lp.LocaleKey = v.LocaleKey);");

        //7) ContractSettings rows (typed ISettings POCO defaults, persisted so admin edits stick).
        IfDatabase(databaseType).Execute.Sql(@"
INSERT INTO [Setting] ([Name], [Value], [StoreId])
SELECT v.[Name], v.[Value], 0
FROM (VALUES
    ('contractsettings.enabled', 'True'),
    ('contractsettings.requireonlyforrentals', 'False')
) AS v ([Name], [Value])
WHERE NOT EXISTS (SELECT 1 FROM [Setting] s WHERE s.[Name] = v.[Name]);");

        //8) storefront locale resources — en + ar.
        IfDatabase(databaseType).Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN (l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%') THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    ('mahjar.contracts.accounttab',            N'عقودي',                                     'My contracts'),
    ('mahjar.contracts.title',                 N'عقودي',                                     'My contracts'),
    ('mahjar.contracts.status.pending',        N'بانتظار المراجعة',                          'Awaiting review'),
    ('mahjar.contracts.status.contractsent',   N'العقد جاهز للتوقيع',                        'Contract ready to sign'),
    ('mahjar.contracts.status.signed',         N'تم التوقيع — بانتظار الاعتماد',             N'Signed — awaiting approval'),
    ('mahjar.contracts.status.approved',       N'معتمد — يمكنك إتمام الشراء',                N'Approved — you can checkout'),
    ('mahjar.contracts.status.rejected',       N'مرفوض',                                     'Rejected'),
    ('mahjar.contracts.status.cancelled',      N'ملغي',                                      'Cancelled'),
    ('mahjar.contracts.status.completed',      N'مكتمل',                                     'Completed'),
    ('mahjar.contracts.cart.required',         N'يتطلب هذا الطلب توقيع عقد قبل الدفع.',       'This order requires a signed contract before payment.'),
    ('mahjar.contracts.cart.button',           N'إرسال طلب التعاقد',                         'Send contract request'),
    ('mahjar.contracts.cart.approved',         N'تم اعتماد عقدك — يمكنك إتمام الشراء.',       N'Your contract is approved — you can proceed to checkout.'),
    ('mahjar.contracts.cart.pendingnote',      N'طلب التعاقد الخاص بك قيد المعالجة. تابعه في صفحة عقودي.', 'Your contract request is being processed. Track it in My contracts.'),
    ('mahjar.contracts.submitted',             N'تم إرسال طلب التعاقد. سنقوم بإشعارك عندما يكون العقد جاهزًا للتوقيع.', 'Your contract request has been sent. We will notify you once the contract is ready to sign.'),
    ('mahjar.contracts.cartchanged',           N'تم تغيير سلة التسوق، لذا أُلغي طلب التعاقد السابق وتم إرسال طلب جديد.', 'Your cart changed, so the previous contract request was cancelled and a new one was sent.'),
    ('mahjar.contracts.empty',                 N'لا توجد لديك طلبات تعاقد بعد.',              'You have no contract requests yet.'),
    ('mahjar.contracts.view',                  N'عرض العقد',                                 'View contract'),
    ('mahjar.contracts.signlink',              N'توقيع العقد',                               'Sign contract'),
    ('mahjar.contracts.rejectionreason',       N'سبب الرفض',                                 'Rejection reason'),
    ('mahjar.contracts.proceedtocheckout',     N'إتمام الشراء',                              'Proceed to checkout'),
    ('mahjar.contracts.cancel',                N'إلغاء الطلب',                               'Cancel request'),
    ('mahjar.contracts.cancelled',             N'تم إلغاء طلب التعاقد.',                      'The contract request was cancelled.'),
    ('mahjar.contracts.checkoutblocked',       N'يلزم وجود عقد موقّع ومعتمد قبل الدفع. يرجى مراجعة صفحة عقودي.', 'A signed and approved contract is required before payment. Please check My contracts.'),
    ('mahjar.contracts.toomanyrequests',       N'عدد كبير من طلبات التعاقد. يرجى الانتظار قليلًا والمحاولة مرة أخرى.', 'Too many contract requests. Please wait a moment and try again.'),
    ('mahjar.contracts.sign.title',            N'توقيع العقد',                               'Sign contract'),
    ('mahjar.contracts.sign.agreement',        N'لقد قرأت العقد وأوافق على شروطه.',           'I have read the contract and agree to its terms.'),
    ('mahjar.contracts.sign.signhere',         N'ارسم توقيعك بالأسفل',                       'Draw your signature below'),
    ('mahjar.contracts.sign.clear',            N'مسح',                                       'Clear'),
    ('mahjar.contracts.sign.typedname',        N'الاسم الكامل',                              'Full name'),
    ('mahjar.contracts.sign.submit',           N'إرسال التوقيع',                             'Submit signature'),
    ('mahjar.contracts.sign.success',          N'شكرًا لك. تم إرسال توقيعك وهو بانتظار الاعتماد.', 'Thank you. Your signature was submitted and is awaiting approval.'),
    ('mahjar.contracts.sign.invalidsignature', N'يرجى إدخال توقيع صحيح واسمك الكامل.',        'Please provide a valid signature and your full name.'),
    ('mahjar.contracts.sign.mustagree',        N'يجب الموافقة على شروط العقد.',               'You must agree to the contract terms.'),
    ('mahjar.contracts.sign.notsignable',      N'هذا العقد غير متاح للتوقيع.',                'This contract is not available for signing.'),
    ('mahjar.contracts.signedon',              N'تاريخ التوقيع',                             'Signed on')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");

        //9) snapshot/table locale resources (contract body + admin + storefront) — en + ar.
        IfDatabase(databaseType).Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN (l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%') THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    ('mahjar.contracts.snapshot.product',      N'الصنف',                  'Item'),
    ('mahjar.contracts.snapshot.mode',         N'النوع',                  'Type'),
    ('mahjar.contracts.snapshot.mode.buy',     N'شراء',                   'Purchase'),
    ('mahjar.contracts.snapshot.mode.rent',    N'إيجار',                  'Rental'),
    ('mahjar.contracts.snapshot.qty',          N'الكمية',                 'Qty'),
    ('mahjar.contracts.snapshot.period',       N'مدة الإيجار',            'Rental period'),
    ('mahjar.contracts.snapshot.days',         N'{0} يوم',                '{0} day(s)'),
    ('mahjar.contracts.snapshot.unitprice',    N'سعر الوحدة',             'Unit price'),
    ('mahjar.contracts.snapshot.deposit',      N'مبلغ التأمين',           'Security deposit'),
    ('mahjar.contracts.snapshot.linetotal',    N'الإجمالي',               'Total'),
    ('mahjar.contracts.snapshot.subtotal',     N'المجموع الفرعي',         'Subtotal'),
    ('mahjar.contracts.snapshot.deposittotal', N'إجمالي مبالغ التأمين',   'Deposits total'),
    ('mahjar.contracts.snapshot.grandtotal',   N'الإجمالي الكلي',         'Grand total')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");

        //10) admin-area locale resources — en + ar.
        IfDatabase(databaseType).Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN (l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%') THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    ('admin.mahjar.contracts.menu',                     N'العقود',                  'Contracts'),
    ('admin.mahjar.contracts.menu.requests',            N'طلبات التعاقد',           'Contract requests'),
    ('admin.mahjar.contracts.menu.template',            N'نموذج العقد',             'Contract template'),
    ('admin.mahjar.contracts.menu.settings',            N'إعدادات العقود',          'Contract settings'),
    ('admin.mahjar.contracts.list.title',               N'طلبات التعاقد',           'Contract requests'),
    ('admin.mahjar.contracts.edit.title',               N'تفاصيل طلب التعاقد',      'Contract request details'),
    ('admin.mahjar.contracts.list.searchstatus',        N'الحالة',                  'Status'),
    ('admin.mahjar.contracts.list.searchcreatedfrom',   N'من تاريخ',                'Created from'),
    ('admin.mahjar.contracts.list.searchcreatedto',     N'إلى تاريخ',               'Created to'),
    ('admin.mahjar.contracts.list.all',                 N'الكل',                    'All'),
    ('admin.mahjar.contracts.fields.customer',          N'العميل',                  'Customer'),
    ('admin.mahjar.contracts.fields.createdon',         N'تاريخ الإنشاء',           'Created on'),
    ('admin.mahjar.contracts.fields.status',            N'الحالة',                  'Status'),
    ('admin.mahjar.contracts.fields.total',             N'الإجمالي',                'Total'),
    ('admin.mahjar.contracts.fields.signedon',          N'تاريخ التوقيع',           'Signed on'),
    ('admin.mahjar.contracts.fields.signedname',        N'اسم الموقّع',             'Signed name'),
    ('admin.mahjar.contracts.fields.signedip',          N'عنوان IP للتوقيع',        'Signed from IP'),
    ('admin.mahjar.contracts.fields.signature',         N'التوقيع',                 'Signature'),
    ('admin.mahjar.contracts.fields.admincomment',      N'ملاحظات الإدارة',         'Admin comment'),
    ('admin.mahjar.contracts.fields.rejectionreason',   N'سبب الرفض',               'Rejection reason'),
    ('admin.mahjar.contracts.fields.contractbody',      N'مستند العقد',             'Contract document'),
    ('admin.mahjar.contracts.fields.snapshot',          N'الأصناف المطلوبة',        'Requested items'),
    ('admin.mahjar.contracts.fields.order',             N'الطلب',                   'Order'),
    ('admin.mahjar.contracts.fields.language',          N'لغة العقد',               'Contract language'),
    ('admin.mahjar.contracts.buttons.sendcontract',     N'إرسال العقد',             'Send contract'),
    ('admin.mahjar.contracts.buttons.approve',          N'اعتماد',                  'Approve'),
    ('admin.mahjar.contracts.buttons.reject',           N'رفض',                     'Reject'),
    ('admin.mahjar.contracts.sent',                     N'تم إرسال العقد إلى العميل للتوقيع.', 'The contract has been sent to the customer for signing.'),
    ('admin.mahjar.contracts.approved',                 N'تم اعتماد العقد. يمكن للعميل الدفع الآن.', 'The contract has been approved. The customer can now pay.'),
    ('admin.mahjar.contracts.rejected',                 N'تم رفض طلب التعاقد.',      'The contract request has been rejected.'),
    ('admin.mahjar.contracts.saved',                    N'تم تحديث طلب التعاقد.',    'The contract request has been updated.'),
    ('admin.mahjar.contracts.rejectionreasonrequired',  N'يرجى إدخال سبب الرفض.',    'Please enter a rejection reason.'),
    ('admin.mahjar.contracts.invalidstatus',            N'هذا الإجراء غير مسموح للحالة الحالية.', 'This action is not allowed for the current status.'),
    ('admin.mahjar.contracts.template.title',           N'نموذج العقد',             'Contract template'),
    ('admin.mahjar.contracts.template.body',            N'نص النموذج',              'Template body'),
    ('admin.mahjar.contracts.template.tokens.hint',     N'الرموز المتاحة: %Contract.Number%، %Contract.Date%، %Customer.FullName%، %Customer.Email%، %Store.Name%، %Contract.ItemsTable%، %Contract.SubTotal%، %Contract.DepositTotal%، %Contract.GrandTotal%', 'Allowed tokens: %Contract.Number%, %Contract.Date%, %Customer.FullName%, %Customer.Email%, %Store.Name%, %Contract.ItemsTable%, %Contract.SubTotal%, %Contract.DepositTotal%, %Contract.GrandTotal%'),
    ('admin.mahjar.contracts.template.updated',         N'تم تحديث نموذج العقد.',    'The contract template has been updated.'),
    ('admin.mahjar.contracts.settings.title',           N'إعدادات العقود',          'Contract settings'),
    ('admin.mahjar.contracts.settings.enabled',         N'اشتراط عقد قبل الدفع',    'Require contract before payment'),
    ('admin.mahjar.contracts.settings.enabled.hint',    N'عند التفعيل، يجب على العملاء طلب عقد وتوقيعه واعتماده قبل التمكن من الدفع.', 'When enabled, customers must request, sign and get an approved contract before they can pay.'),
    ('admin.mahjar.contracts.settings.requireonlyforrentals',      N'فقط للسلال التي تحتوي على إيجارات', 'Only for carts containing rentals'),
    ('admin.mahjar.contracts.settings.requireonlyforrentals.hint', N'عند التفعيل، تتخطى سلال الشراء فقط مرحلة العقد.', 'When enabled, buy-only carts skip the contract stage.'),
    ('admin.mahjar.contracts.settings.updated',         N'تم تحديث إعدادات العقود.', 'The contract settings have been updated.')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");

        //11) mobile-API messages + notification-type title/body templates ({0}=contract number,
        //    {1}=reason / customer name per key) — en + ar.
        IfDatabase(databaseType).Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN (l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%') THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    ('mahjar.api.contracts.notfound',          N'لا يوجد طلب تعاقد.',              'No contract request found.'),
    ('mahjar.api.contracts.cartempty',         N'سلة التسوق فارغة.',               'Your shopping cart is empty.'),
    ('mahjar.api.contracts.notrequired',       N'لا يلزم عقد لهذه السلة.',          'No contract is required for this cart.'),
    ('mahjar.api.contracts.submitted',         N'تم إرسال طلب التعاقد.',            'Contract request submitted.'),
    ('mahjar.api.contracts.notsignable',       N'هذا العقد غير متاح للتوقيع.',      'This contract is not available for signing.'),
    ('mahjar.api.contracts.signed',            N'تم إرسال التوقيع. بانتظار الاعتماد.', 'Signature submitted. Awaiting approval.'),
    ('mahjar.api.contracts.invalidsignature',  N'بيانات توقيع غير صالحة.',          'Invalid signature data.'),
    ('mahjar.api.contracts.cancelled',         N'تم إلغاء طلب التعاقد.',            'Contract request cancelled.'),
    ('mahjar.api.contracts.cannotcancel',      N'لا يمكن إلغاء طلب التعاقد هذا.',   'This contract request cannot be cancelled.'),
    ('mahjar.notifications.type.contractsentbuyer.title',      N'عقد جاهز للتوقيع',            'Contract ready to sign'),
    ('mahjar.notifications.type.contractsentbuyer.body',       N'العقد {0} جاهز لتوقيعك.',      'Contract {0} is ready for your signature.'),
    ('mahjar.notifications.type.contractapprovedbuyer.title',  N'تم اعتماد العقد',             'Contract approved'),
    ('mahjar.notifications.type.contractapprovedbuyer.body',   N'تم اعتماد العقد {0}. يمكنك الآن إتمام الشراء.', 'Contract {0} was approved. You can now complete your purchase.'),
    ('mahjar.notifications.type.contractrejectedbuyer.title',  N'تم رفض العقد',                'Contract rejected'),
    ('mahjar.notifications.type.contractrejectedbuyer.body',   N'تم رفض العقد {0}. {1}',        'Contract {0} was rejected. {1}'),
    ('mahjar.notifications.type.contractrequestedadmin.title', N'طلب تعاقد جديد',              'New contract request'),
    ('mahjar.notifications.type.contractrequestedadmin.body',  N'قدّم العميل {1} طلب التعاقد {0}.', 'Customer {1} submitted contract request {0}.'),
    ('mahjar.notifications.type.contractsignedadmin.title',    N'تم توقيع عقد',                'Contract signed'),
    ('mahjar.notifications.type.contractsignedadmin.body',     N'تم توقيع العقد {0} بواسطة {1} وهو بانتظار المراجعة.', 'Contract {0} was signed by {1} and awaits review.')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }
}
