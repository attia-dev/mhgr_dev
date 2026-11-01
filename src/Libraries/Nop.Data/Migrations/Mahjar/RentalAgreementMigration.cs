// [Mahjar contracts]
using System.Data;
using FluentMigrator;
using Nop.Core.Domain.Mahjar;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Installs the store owner's rental agreement ("اتفاقية المستأجر عبر منصة محجر") as the Rental contract template,
/// and everything it needs to print without a blank:
///
/// 1) The per-transaction columns on <see cref="ContractRequest"/> (handover and return details, verification
///    reference, guarantee, extra fees, tax, late-return charge) that override the contract settings per contract.
/// 2) The agreement body itself: <see cref="RentalAgreementContent.Arabic"/> for the Arabic languages (the legally
///    prevailing text) and <see cref="RentalAgreementContent.English"/> as the default/English body. The rental
///    template is REPLACED, because the owner asked for this text to be the default rental contract.
/// 3) The locale resources of the new Contract settings fields and the per-contract values, plus the refreshed list
///    of tokens shown on the template editor.
///
/// The bodies are written through a parameterized command (they are long HTML with quotes), so nothing is escaped by
/// hand. Idempotent: the columns are guarded, the localized rows are updated or inserted, and the resources are
/// inserted only when missing. SQL Server dialect, matching the sibling Mahjar migrations. NOTE: locale resources and
/// settings are cached — restart (or POST /Admin/Common/ClearCache) after it runs. [Mahjar contracts]
/// </summary>
[NopSchemaMigration("2026-09-16 09:00:00", "Mahjar contracts: the rental agreement as the default rental contract + its settings")]
public class RentalAgreementMigration : ForwardOnlyMigration
{
    /// <summary>The Arabic-language predicate every Mahjar resource migration uses.</summary>
    private const string IS_ARABIC = "(l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')";

    /// <summary>The T-SQL-only dialect guard every Mahjar migration uses.</summary>
    private const string DATABASE_TYPE = "sqlserver";

    public override void Up()
    {
        AddContractRequestColumns();
        InstallRentalAgreement();
        AddLocaleResources();
    }

    #region Utilities

    /// <summary>1) The per-transaction contract values, each nullable (null = use the contract setting).</summary>
    protected virtual void AddContractRequestColumns()
    {
        var table = nameof(ContractRequest);

        void addText(string column, int length)
        {
            if (!Schema.Table(table).Column(column).Exists())
                Alter.Table(table).AddColumn(column).AsString(length).Nullable();
        }

        void addAmount(string column)
        {
            if (!Schema.Table(table).Column(column).Exists())
                Alter.Table(table).AddColumn(column).AsDecimal(18, 4).Nullable();
        }

        addText(nameof(ContractRequest.DeliveryDetails), 1000);
        addText(nameof(ContractRequest.ReturnDetails), 1000);
        addText(nameof(ContractRequest.VerificationReference), 400);
        addText(nameof(ContractRequest.GuaranteeType), 1000);
        addText(nameof(ContractRequest.ExtraFees), 1000);
        addText(nameof(ContractRequest.LateReturnFee), 1000);
        addAmount(nameof(ContractRequest.GuaranteeAmount));
        addAmount(nameof(ContractRequest.TaxAmount));
    }

    /// <summary>2) The agreement itself: the English body on the rental template, the Arabic body per Arabic language.</summary>
    protected virtual void InstallRentalAgreement()
    {
        IfDatabase(DATABASE_TYPE).Execute.WithConnection((connection, transaction) =>
        {
            IDbCommand command(string sql)
            {
                var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = sql;

                return cmd;
            }

            void addParameter(IDbCommand cmd, string name, object value)
            {
                var parameter = cmd.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value ?? DBNull.Value;
                cmd.Parameters.Add(parameter);
            }

            //the rental template row (kind 20); nothing to do when it is missing
            int templateId;
            using (var cmd = command($"SELECT TOP 1 Id FROM ContractTemplate WHERE TemplateTypeId = {(int)ContractTemplateType.Rental} ORDER BY Id"))
            {
                var found = cmd.ExecuteScalar();
                if (found == null || found == DBNull.Value)
                    return;

                templateId = Convert.ToInt32(found);
            }

            //the default body (used for English and any non-Arabic language)
            using (var cmd = command("UPDATE ContractTemplate SET Body = @body, UpdatedOnUtc = @updatedOn WHERE Id = @id"))
            {
                addParameter(cmd, "@body", RentalAgreementContent.English);
                addParameter(cmd, "@updatedOn", DateTime.UtcNow);
                addParameter(cmd, "@id", templateId);
                cmd.ExecuteNonQuery();
            }

            //the Arabic body, one LocalizedProperty row per Arabic language
            var arabicLanguageIds = new List<int>();
            using (var cmd = command($"SELECT Id FROM Language l WHERE {IS_ARABIC}"))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    arabicLanguageIds.Add(reader.GetInt32(0));
            }

            foreach (var languageId in arabicLanguageIds)
            {
                using var cmd = command(@"
UPDATE LocalizedProperty
SET LocaleValue = @value
WHERE EntityId = @entityId AND LanguageId = @languageId AND LocaleKeyGroup = 'ContractTemplate' AND LocaleKey = 'Body';

INSERT INTO LocalizedProperty (EntityId, LanguageId, LocaleKeyGroup, LocaleKey, LocaleValue)
SELECT @entityId, @languageId, 'ContractTemplate', 'Body', @value
WHERE NOT EXISTS (
    SELECT 1 FROM LocalizedProperty
    WHERE EntityId = @entityId AND LanguageId = @languageId AND LocaleKeyGroup = 'ContractTemplate' AND LocaleKey = 'Body');");

                addParameter(cmd, "@value", RentalAgreementContent.Arabic);
                addParameter(cmd, "@entityId", templateId);
                addParameter(cmd, "@languageId", languageId);
                cmd.ExecuteNonQuery();
            }

            //A localized body SHADOWS the Body column for that language, so a leftover one — e.g. an English body an
            //admin saved before this agreement existed — would keep being served instead of the new text. Drop the
            //localized rows of every non-Arabic language for this template: the Body column is their text now.
            using (var cmd = command($@"
DELETE lp
FROM LocalizedProperty lp
JOIN Language l ON l.Id = lp.LanguageId
WHERE lp.EntityId = @entityId
  AND lp.LocaleKeyGroup = 'ContractTemplate'
  AND lp.LocaleKey = 'Body'
  AND NOT {IS_ARABIC}"))
            {
                addParameter(cmd, "@entityId", templateId);
                cmd.ExecuteNonQuery();
            }

            //name the template after the agreement, but only while it still carries the seeded name
            using (var cmd = command("UPDATE ContractTemplate SET Name = @name WHERE Id = @id AND Name = 'Rental contract'"))
            {
                addParameter(cmd, "@name", "Renter agreement (rental)");
                addParameter(cmd, "@id", templateId);
                cmd.ExecuteNonQuery();
            }
        });
    }

    /// <summary>3) Locale resources for the new settings / per-contract values, and the refreshed token list.</summary>
    protected virtual void AddLocaleResources()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN " + IS_ARABIC + @" THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    ('admin.mahjar.contracts.settings.blocktitle.stage',          N'مرحلة التعاقد',                  'The contract stage'),
    ('admin.mahjar.contracts.settings.blocktitle.party',          N'المنصة كطرف في العقد',           'The platform as a contracting party'),
    ('admin.mahjar.contracts.settings.blocktitle.agreement',      N'نص الاتفاقية',                   'The agreement text'),
    ('admin.mahjar.contracts.settings.blocktitle.policy',         N'قيم سياسة التأجير',              'Rental policy values'),
    ('admin.mahjar.contracts.settings.blocktitle.defaults',       N'القيم الافتراضية لكل عملية',      'Default wording per transaction'),
    ('admin.mahjar.contracts.settings.hint',                      N'تطبع هذه القيم داخل العقد عبر رموزها، ويمكن تجاوز قيم العملية الواحدة من صفحة طلب التعاقد قبل إرسال العقد.', N'These values are printed inside the contract through its tokens. The values of a single transaction can be overridden on its contract request before the contract is sent.'),
    ('admin.mahjar.contracts.settings.operatorlegalname',         N'الاسم النظامي لمشغل المنصة',      'Platform operator''s legal name'),
    ('admin.mahjar.contracts.settings.operatorlegalname.hint',    N'يطبع في العقد عبر الرمز %Store.LegalName%. إذا تُرك فارغًا استُخدم اسم الشركة ثم اسم المتجر.', N'Printed in the contract through %Store.LegalName%. If left empty, the company name and then the store name are used.'),
    ('admin.mahjar.contracts.settings.commercialregistration',    N'رقم السجل التجاري',              'Commercial registration number'),
    ('admin.mahjar.contracts.settings.commercialregistration.hint', N'يطبع بجانب الاسم النظامي عبر الرمز %Store.CommercialRegistration%.', N'Printed next to the legal name through %Store.CommercialRegistration%.'),
    ('admin.mahjar.contracts.settings.supportchannel',            N'قناة الدعم',                     'Support channel'),
    ('admin.mahjar.contracts.settings.supportchannel.hint',       N'بريد أو رقم يظهر للعميل في العقد عبر الرمز %Store.SupportChannel%.', N'An email or a number shown to the customer through %Store.SupportChannel%.'),
    ('admin.mahjar.contracts.settings.agreementversion',          N'رقم إصدار الاتفاقية',            'Agreement version'),
    ('admin.mahjar.contracts.settings.agreementversion.hint',     N'يطبع في ترويسة الاتفاقية وفي سجل القبول عبر الرمز %Contract.Version%.', N'Printed in the agreement header and in the acceptance record through %Contract.Version%.'),
    ('admin.mahjar.contracts.settings.effectivedate',             N'تاريخ نفاذ الاتفاقية',           'Agreement effective date'),
    ('admin.mahjar.contracts.settings.effectivedate.hint',        N'الرمز %Contract.EffectiveDate%. إذا تُرك فارغًا طُبعت شرطة.', N'Token %Contract.EffectiveDate%. A dash is printed when it is empty.'),
    ('admin.mahjar.contracts.settings.updateddate',               N'تاريخ آخر تحديث للاتفاقية',      'Agreement last updated'),
    ('admin.mahjar.contracts.settings.updateddate.hint',          N'الرمز %Contract.UpdatedDate%.',   N'Token %Contract.UpdatedDate%.'),
    ('admin.mahjar.contracts.settings.itemvaluecap',              N'حد قيمة القطعة المؤجرة',         'Rental item value cap'),
    ('admin.mahjar.contracts.settings.itemvaluecap.hint',         N'أعلى قيمة قطعة يقبلها المتجر للتأجير، تطبع عبر الرمز %Contract.ItemValueCap%.', N'The highest item value the store rents out, printed through %Contract.ItemValueCap%.'),
    ('admin.mahjar.contracts.settings.lategracehours',            N'مهلة المعالجة الأولية للتأخير (بالساعات)', 'Late-return grace period (hours)'),
    ('admin.mahjar.contracts.settings.lategracehours.hint',       N'الرمز %Contract.LateGraceHours%.', N'Token %Contract.LateGraceHours%.'),
    ('admin.mahjar.contracts.settings.latefeeterms',              N'رسوم أو مقابل التأخير',          'Late-return charge'),
    ('admin.mahjar.contracts.settings.latefeeterms.hint',         N'المبلغ أو طريقة احتسابه، يطبع عبر الرمز %Contract.LateFee%.', N'The amount or how it is calculated, printed through %Contract.LateFee%.'),
    ('admin.mahjar.contracts.settings.cancellationterms',         N'رسوم الإلغاء قبل التسليم',       'Cancellation charge before handover'),
    ('admin.mahjar.contracts.settings.cancellationterms.hint',    N'الرمز %Contract.CancellationFee%. إذا تُرك فارغًا طُبعت شرطة.', N'Token %Contract.CancellationFee%. A dash is printed when it is empty.'),
    ('admin.mahjar.contracts.settings.refunddays',                N'مدة معالجة الاسترداد (أيام عمل)', 'Refund processing (working days)'),
    ('admin.mahjar.contracts.settings.refunddays.hint',           N'الرمز %Contract.RefundDays%.',    N'Token %Contract.RefundDays%.'),
    ('admin.mahjar.contracts.settings.objectiondays',             N'مهلة الاعتراض على مطالبة الضرر (أيام عمل)', 'Damage objection window (working days)'),
    ('admin.mahjar.contracts.settings.objectiondays.hint',        N'الرمز %Contract.ObjectionDays%.', N'Token %Contract.ObjectionDays%.'),
    ('admin.mahjar.contracts.settings.complaintdays',             N'مدة معالجة الشكوى (أيام عمل)',    'Complaint handling (working days)'),
    ('admin.mahjar.contracts.settings.complaintdays.hint',        N'الرمز %Contract.ComplaintDays%.', N'Token %Contract.ComplaintDays%.'),
    ('admin.mahjar.contracts.settings.settlementdays',            N'مهلة التسوية الودية (أيام عمل)',  'Amicable settlement (working days)'),
    ('admin.mahjar.contracts.settings.settlementdays.hint',       N'الرمز %Contract.SettlementDays%.', N'Token %Contract.SettlementDays%.'),
    ('admin.mahjar.contracts.settings.guaranteetype',             N'الضمان أو السند المطلوب',        'Guarantee required'),
    ('admin.mahjar.contracts.settings.guaranteetype.hint',        N'الرمز %Contract.GuaranteeType%، ويمكن تجاوزه لكل عملية.', N'Token %Contract.GuaranteeType%; it can be overridden per transaction.'),
    ('admin.mahjar.contracts.settings.deliverydetails',           N'موعد ومكان التسليم',             'Handover time and place'),
    ('admin.mahjar.contracts.settings.deliverydetails.hint',      N'الرمز %Contract.DeliveryDetails%، ويمكن تجاوزه لكل عملية.', N'Token %Contract.DeliveryDetails%; it can be overridden per transaction.'),
    ('admin.mahjar.contracts.settings.returndetails',             N'موعد ومكان الإرجاع',             'Return time and place'),
    ('admin.mahjar.contracts.settings.returndetails.hint',        N'الرمز %Contract.ReturnDetails%، ويمكن تجاوزه لكل عملية.', N'Token %Contract.ReturnDetails%; it can be overridden per transaction.'),
    ('admin.mahjar.contracts.settings.extrafees',                 N'الرسوم الإضافية',                'Additional fees'),
    ('admin.mahjar.contracts.settings.extrafees.hint',            N'الرمز %Contract.ExtraFees%. إذا تُرك فارغًا طُبعت شرطة.', N'Token %Contract.ExtraFees%. A dash is printed when it is empty.'),
    ('admin.mahjar.contracts.settings.taxterms',                  N'الضريبة',                        'Tax'),
    ('admin.mahjar.contracts.settings.taxterms.hint',             N'كيف تُعامل الضريبة في مبالغ العقد، الرمز %Contract.Tax%. ويمكن إدخال مبلغ ضريبة لعملية بعينها.', N'How tax is treated in the contract amounts, token %Contract.Tax%. A tax amount can be entered for an individual transaction.'),
    ('admin.mahjar.contracts.fields.transactionterms',            N'بيانات هذه العملية في العقد',     'The contract values of this transaction'),
    ('admin.mahjar.contracts.fields.transactionterms.hint',       N'اترك الحقل فارغًا لاستخدام القيمة الافتراضية من إعدادات العقود. لا يمكن التعديل بعد إرسال العقد لأن نصه يُجمَّد عند الإرسال.', N'Leave a field empty to use the default from the contract settings. They cannot be changed after the contract is sent, because its text is frozen at that moment.'),
    ('admin.mahjar.contracts.fields.deliverydetails',             N'موعد ومكان التسليم',             'Handover time and place'),
    ('admin.mahjar.contracts.fields.returndetails',               N'موعد ومكان الإرجاع',             'Return time and place'),
    ('admin.mahjar.contracts.fields.verificationreference',       N'وسيلة/معرف التحقق من الهوية',    'Identity verification method / reference'),
    ('admin.mahjar.contracts.fields.guaranteetype',               N'الضمان أو السند المطلوب',        'Guarantee required'),
    ('admin.mahjar.contracts.fields.guaranteeamount',             N'قيمة الضمان',                    'Guarantee amount'),
    ('admin.mahjar.contracts.fields.guaranteeamount.hint',        N'إذا تُرك فارغًا استُخدم إجمالي مبالغ التأمين في السلة.', N'If left empty, the cart''s deposit total is used.'),
    ('admin.mahjar.contracts.fields.extrafees',                   N'الرسوم الإضافية',                'Additional fees'),
    ('admin.mahjar.contracts.fields.taxamount',                   N'مبلغ الضريبة',                   'Tax amount'),
    ('admin.mahjar.contracts.fields.latefee',                     N'رسوم أو مقابل التأخير',          'Late-return charge')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");

        //the template editor lists the tokens an admin may use — refresh it with the agreement's tokens, but only
        //while it still holds the previously seeded text, so an admin's own wording is never overwritten.
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
UPDATE r
SET r.ResourceValue = CASE WHEN " + IS_ARABIC + @"
    THEN N'الرموز المتاحة: %Contract.Number%، %Contract.OrderNumber%، %Contract.Date%، %Contract.Version%، %Contract.EffectiveDate%، %Contract.UpdatedDate%، %Customer.FullName%، %Customer.Email%، %Customer.Id%، %Store.Name%، %Store.LegalName%، %Store.CommercialRegistration%، %Store.SupportChannel%، %Contract.ItemsTable%، %Contract.RentalPeriod%، %Contract.SubTotal%، %Contract.DepositTotal%، %Contract.GrandTotal%، %Contract.GuaranteeType%، %Contract.GuaranteeAmount%، %Contract.DeliveryDetails%، %Contract.ReturnDetails%، %Contract.VerificationRef%، %Contract.ExtraFees%، %Contract.Tax%، %Contract.LateFee%، %Contract.CancellationFee%، %Contract.ItemValueCap%، %Contract.LateGraceHours%، %Contract.RefundDays%، %Contract.ObjectionDays%، %Contract.ComplaintDays%، %Contract.SettlementDays%'
    ELSE N'Allowed tokens: %Contract.Number%, %Contract.OrderNumber%, %Contract.Date%, %Contract.Version%, %Contract.EffectiveDate%, %Contract.UpdatedDate%, %Customer.FullName%, %Customer.Email%, %Customer.Id%, %Store.Name%, %Store.LegalName%, %Store.CommercialRegistration%, %Store.SupportChannel%, %Contract.ItemsTable%, %Contract.RentalPeriod%, %Contract.SubTotal%, %Contract.DepositTotal%, %Contract.GrandTotal%, %Contract.GuaranteeType%, %Contract.GuaranteeAmount%, %Contract.DeliveryDetails%, %Contract.ReturnDetails%, %Contract.VerificationRef%, %Contract.ExtraFees%, %Contract.Tax%, %Contract.LateFee%, %Contract.CancellationFee%, %Contract.ItemValueCap%, %Contract.LateGraceHours%, %Contract.RefundDays%, %Contract.ObjectionDays%, %Contract.ComplaintDays%, %Contract.SettlementDays%'
    END
FROM LocaleStringResource r
JOIN Language l ON l.Id = r.LanguageId
WHERE r.ResourceName = 'admin.mahjar.contracts.template.tokens.hint'
  AND r.ResourceValue LIKE '%Contract.ItemsTable%%'
  AND r.ResourceValue NOT LIKE '%Contract.RentalPeriod%%';");
    }

    #endregion
}
