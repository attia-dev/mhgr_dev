// [Mahjar partner-agreement]
using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Wording for the agreement field picker on Admin -> Settings -> Vendor settings -> Partner agreement: the
/// four group headings, the line under each saying where that group's values are set, and a name for every
/// field a body may carry. The picker offers them by name and inserts the token, so the store owner never has
/// to know that "مدة تجهيز الطلب" is spelled %Partner.PrepTime%.
///
/// Resource names follow <c>Nop.Services.Mahjar.Agreements.PartnerAgreementTokens.GetResourceName</c>: the
/// token lower-cased without its percent signs. A field with no resource falls back to showing the raw token,
/// so a missing row here is untidy rather than broken. Idempotent, SQL Server dialect. NOTE: locale resources
/// are cached — restart (or POST /Admin/Common/ClearCache) after it runs. [Mahjar partner-agreement]
/// </summary>
[NopSchemaMigration("2026-09-20 12:00:00", "Mahjar partner agreement: names for the agreement field picker")]
public class PartnerAgreementFieldPickerMigration : ForwardOnlyMigration
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
    -- the picker itself
    ('Admin.Configuration.Settings.Vendor.AgreementFields',        N'حقول الاتفاقية',  'Agreement fields'),
    ('Admin.Configuration.Settings.Vendor.AgreementFields.Hint',   N'اضغط على أي حقل لإدراجه في موضع المؤشر داخل نص الاتفاقية. يُطبع الحقل بقيمته عند عرض الاتفاقية على مقدم الطلب.', 'Click a field to insert it where the cursor is in the agreement text. The field prints its value when the agreement is shown to an applicant.'),
    ('Admin.Configuration.Settings.Vendor.AgreementFields.Copied', N'انسخ هذا الحقل والصقه في النص:', 'Copy this field and paste it into the text:'),

    -- the groups
    ('admin.mahjar.partneragreement.tokens.group.platform',         N'بيانات محجر',                 'MHGR''s own details'),
    ('admin.mahjar.partneragreement.tokens.group.platform.hint',    N'تُضبط في إعدادات العقود، ما عدا العنوان الوطني فهو في هذه البطاقة.', 'Set in the contract settings, except the national address which is on this card.'),
    ('admin.mahjar.partneragreement.tokens.group.partner',          N'بيانات الشريك',               'The partner''s details'),
    ('admin.mahjar.partneragreement.tokens.group.partner.hint',     N'تُملأ من طلب الانضمام لكل شريك على حدة. تظهر فارغة (شرطة) في النسخة المعروضة قبل الإرسال.', 'Filled from each partner''s own application. They show as a dash in the copy displayed before submitting.'),
    ('admin.mahjar.partneragreement.tokens.group.schedule',         N'الجدول التجاري — الملحق (أ)',  'The commercial schedule — Annex A'),
    ('admin.mahjar.partneragreement.tokens.group.schedule.hint',    N'تُضبط في الحقول أعلاه في هذه البطاقة، وتسري على جميع الشركاء.', 'Set in the fields above on this card, and they apply to every partner.'),
    ('admin.mahjar.partneragreement.tokens.group.acceptance',       N'سجل القبول',                  'The acceptance record'),
    ('admin.mahjar.partneragreement.tokens.group.acceptance.hint',  N'تُملأ لحظة قبول الشريك، وتُحفظ معه في النسخة المجمّدة.', 'Filled at the moment the partner accepts, and kept with them in the frozen copy.'),

    -- MHGR's own details
    ('admin.mahjar.partneragreement.token.store.legalname',              N'الاسم النظامي لمحجر',     'MHGR legal name'),
    ('admin.mahjar.partneragreement.token.store.commercialregistration', N'السجل التجاري لمحجر',     'MHGR commercial registration'),
    ('admin.mahjar.partneragreement.token.store.nationaladdress',        N'العنوان الوطني لمحجر',    'MHGR national address'),
    ('admin.mahjar.partneragreement.token.store.supportchannel',         N'قناة الدعم',              'Support channel'),

    -- the partner's details
    ('admin.mahjar.partneragreement.token.partner.name',                   N'اسم الشريك',            'Partner name'),
    ('admin.mahjar.partneragreement.token.partner.type',                   N'نوع الشريك',            'Partner type'),
    ('admin.mahjar.partneragreement.token.partner.legalform',              N'الشكل النظامي للشريك',  'Partner legal form'),
    ('admin.mahjar.partneragreement.token.partner.userid',                 N'معرف المستخدم',         'User identifier'),
    ('admin.mahjar.partneragreement.token.partner.email',                  N'البريد المعتمد',        'Approved e-mail'),
    ('admin.mahjar.partneragreement.token.partner.nationalid',             N'رقم الهوية/الإقامة',    'National ID / Iqama number'),
    ('admin.mahjar.partneragreement.token.partner.commercialregistration', N'السجل التجاري للشريك',  'Partner commercial registration'),
    ('admin.mahjar.partneragreement.token.partner.vatnumber',              N'الرقم الضريبي للشريك',  'Partner VAT number'),
    ('admin.mahjar.partneragreement.token.partner.signatoryname',          N'اسم المفوض بالتوقيع',   'Authorised signatory'),
    ('admin.mahjar.partneragreement.token.partner.signatorycapacity',      N'صفة المفوض بالتوقيع',   'Signing capacity'),
    ('admin.mahjar.partneragreement.token.partner.activatedservices',      N'الخدمات المفعلة',       'Activated services'),

    -- the commercial schedule
    ('admin.mahjar.partneragreement.token.partner.verificationmethod',     N'وسيلة التحقق من الهوية', 'Identity verification method'),
    ('admin.mahjar.partneragreement.token.partner.settlementaccount',      N'حساب التسوية المعتمد',   'Approved settlement account'),
    ('admin.mahjar.partneragreement.token.partner.rentalcommission',       N'عمولة التأجير',          'Rental commission'),
    ('admin.mahjar.partneragreement.token.partner.salecommission',         N'عمولة البيع',            'Sale commission'),
    ('admin.mahjar.partneragreement.token.partner.listingfee',             N'رسوم الإدراج',           'Listing fee'),
    ('admin.mahjar.partneragreement.token.partner.inspectionfee',          N'رسوم الفحص',             'Inspection fee'),
    ('admin.mahjar.partneragreement.token.partner.deliveryfee',            N'رسوم التسليم/الاستلام',  'Delivery / pick-up fee'),
    ('admin.mahjar.partneragreement.token.partner.storagefee',             N'رسوم التخزين',           'Storage fee'),
    ('admin.mahjar.partneragreement.token.partner.minimumsettlement',      N'الحد الأدنى للتسوية',    'Minimum settlement'),
    ('admin.mahjar.partneragreement.token.partner.preptime',               N'مدة تجهيز الطلب',        'Order preparation time'),
    ('admin.mahjar.partneragreement.token.partner.settlementcycle',        N'مدة/دورية التسوية',      'Settlement period / cycle'),
    ('admin.mahjar.partneragreement.token.partner.claimresponsedays',      N'مهلة الرد على مطالبة',   'Claim response window'),
    ('admin.mahjar.partneragreement.token.partner.terminationnoticedays',  N'مدة الإشعار بالإنهاء',   'Termination notice'),
    ('admin.mahjar.partneragreement.token.partner.amicablesettlementdays', N'مهلة التسوية الودية',    'Amicable settlement window'),
    ('admin.mahjar.partneragreement.token.partner.returninspectiondays',   N'مهلة فحص المرتجع',       'Return inspection window'),
    ('admin.mahjar.partneragreement.token.partner.agreementterm',          N'مدة الاتفاقية',          'Agreement term'),
    ('admin.mahjar.partneragreement.token.partner.renewalbasis',           N'أساس تجديد الاتفاقية',   'Renewal basis'),
    ('admin.mahjar.partneragreement.token.partner.rentalitemvaluecap',     N'حد قيمة القطعة المؤجرة', 'Rental item value cap'),
    ('admin.mahjar.partneragreement.token.partner.commercialnotes',        N'ملاحظات تجارية',         'Commercial notes'),

    -- the acceptance record
    ('admin.mahjar.partneragreement.token.agreement.id',               N'رقم/معرف الاتفاقية',        'Agreement identifier'),
    ('admin.mahjar.partneragreement.token.agreement.version',          N'إصدار الاتفاقية',           'Agreement version'),
    ('admin.mahjar.partneragreement.token.agreement.effectivedate',    N'تاريخ النفاذ',              'Effective date'),
    ('admin.mahjar.partneragreement.token.agreement.acceptedannexes',  N'الملاحق المقبولة',          'Accepted annexes'),
    ('admin.mahjar.partneragreement.token.agreement.acceptedon',       N'تاريخ ووقت القبول',         'Accepted on'),
    ('admin.mahjar.partneragreement.token.agreement.acceptancemethod', N'وسيلة القبول الإلكتروني',   'Electronic acceptance method'),
    ('admin.mahjar.partneragreement.token.agreement.technicalref',     N'المرجع التقني للقبول',      'Technical reference')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }
}
