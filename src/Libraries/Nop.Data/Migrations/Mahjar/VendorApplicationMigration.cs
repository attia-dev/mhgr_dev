// [Mahjar vendor-application]
using FluentMigrator;
using Nop.Core.Domain.Vendors;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Upgrades the "become a partner" application (web /vendor/apply + mobile POST /api/customer/apply-vendor) with
/// the two things the marketplace needs before it can onboard a partner:
///
/// 1) <b>Terms and conditions.</b> Turns on <see cref="VendorSettings.TermsOfServiceEnabled"/> and seeds the new
///    <see cref="VendorSettings.TermsOfService"/> setting (HTML, English value + Arabic <c>LocalizedProperty</c>)
///    with starter terms. The store owner edits them in Admin -> Settings -> Vendor settings, per language — no
///    deploy needed. Acceptance is now enforced SERVER-side on both the web form and the API (stock nopCommerce
///    only had a JavaScript alert), and the moment of acceptance is stamped on the vendor record.
/// 2) <b>Identity of the applicant.</b> Adds <see cref="Vendor.VendorTypeId"/> (individual vs corporation) plus the
///    KSA identity set — <see cref="Vendor.NationalId"/> (National ID / Iqama, individuals),
///    <see cref="Vendor.CommercialRegistrationNumber"/> (CR, corporations) and the optional
///    <see cref="Vendor.VatNumber"/> — and <see cref="Vendor.TermsAcceptedOnUtc"/>. All of it shows on the request
///    in Admin -> Partners and in the store-owner notification email (new %Vendor.*% tokens).
///
/// Idempotent throughout (column guards, NOT EXISTS on every insert, token-guarded template rewrite). Schema
/// migration so it auto-applies on startup against an already-installed DB. SQL Server dialect, matching the
/// sibling Mahjar migrations. NOTE: settings and locale resources are cached — restart (or
/// POST /Admin/Common/ClearCache) after it runs. [Mahjar vendor-application]
/// </summary>
[NopSchemaMigration("2026-09-09 10:00:00", "Mahjar vendor application: terms of service + partner type & identity")]
public class VendorApplicationMigration : ForwardOnlyMigration
{
    /// <summary>The Arabic-language predicate every Mahjar resource migration uses.</summary>
    private const string IS_ARABIC = "(l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')";

    public override void Up()
    {
        AddVendorColumns();
        EnableAndSeedTermsOfService();
        AddLocaleResources();
        ExtendStoreOwnerNotification();
    }

    #region Utilities

    /// <summary>1) Partner type + KSA identity fields + the terms-acceptance stamp on the Vendor table.</summary>
    protected virtual void AddVendorColumns()
    {
        var vendorTable = nameof(Vendor);

        //individual (1) / corporation (2); 0 = not specified, which is what pre-existing vendors keep
        if (!Schema.Table(vendorTable).Column(nameof(Vendor.VendorTypeId)).Exists())
        {
            Alter.Table(vendorTable)
                .AddColumn(nameof(Vendor.VendorTypeId))
                .AsInt32()
                .NotNullable()
                .WithDefaultValue((int)VendorType.NotSpecified);
        }

        //National ID / Iqama — required from individuals
        if (!Schema.Table(vendorTable).Column(nameof(Vendor.NationalId)).Exists())
        {
            Alter.Table(vendorTable)
                .AddColumn(nameof(Vendor.NationalId))
                .AsString(50)
                .Nullable();
        }

        //commercial registration — required from corporations
        if (!Schema.Table(vendorTable).Column(nameof(Vendor.CommercialRegistrationNumber)).Exists())
        {
            Alter.Table(vendorTable)
                .AddColumn(nameof(Vendor.CommercialRegistrationNumber))
                .AsString(50)
                .Nullable();
        }

        //VAT registration — optional for both types
        if (!Schema.Table(vendorTable).Column(nameof(Vendor.VatNumber)).Exists())
        {
            Alter.Table(vendorTable)
                .AddColumn(nameof(Vendor.VatNumber))
                .AsString(50)
                .Nullable();
        }

        //when the applicant ticked the terms box (null for vendors created before the gate existed)
        if (!Schema.Table(vendorTable).Column(nameof(Vendor.TermsAcceptedOnUtc)).Exists())
        {
            Alter.Table(vendorTable)
                .AddColumn(nameof(Vendor.TermsAcceptedOnUtc))
                .AsDateTime2()
                .Nullable();
        }
    }

    /// <summary>
    /// 2) Require terms on the application and seed the editable terms body (en value + ar LocalizedProperty).
    /// The Setting row must exist before Admin can save a localized value for it — SaveLocalizedSettingAsync
    /// silently no-ops when the setting is missing — which is why the row is inserted here rather than lazily.
    /// </summary>
    protected virtual void EnableAndSeedTermsOfService()
    {
        IfDatabase("sqlserver").Execute.Sql(@"
UPDATE [Setting] SET [Value] = 'True' WHERE [Name] = 'vendorsettings.termsofserviceenabled';
IF @@ROWCOUNT = 0
    INSERT INTO [Setting] ([Name], [Value], [StoreId])
    VALUES ('vendorsettings.termsofserviceenabled', 'True', 0);");

        //English (default) body — the store owner replaces this from Admin -> Settings -> Vendor settings
        IfDatabase("sqlserver").Execute.Sql(@"
INSERT INTO [Setting] ([Name], [Value], [StoreId])
SELECT 'vendorsettings.termsofservice', N'<h3>MHGR Partner Terms and Conditions</h3>
<ol>
<li>The applicant confirms that every detail and document submitted is accurate and complete, and that they are legally entitled to trade in the Kingdom of Saudi Arabia.</li>
<li>The partner may list only pieces they own or are authorised to sell or rent, and is responsible for the accuracy of their description, images and price.</li>
<li>The partner is fully responsible for the authenticity of every listed piece. MHGR may suspend any listing or account where authenticity is in doubt.</li>
<li>The platform commission is charged on each sale or rental at the rate stated in the partner agreement in force.</li>
<li>For rentals, the partner shall hand the piece over on the agreed date and in the agreed condition. Security deposits are refunded under the platform policy.</li>
<li>The partner shall comply with the applicable laws of the Kingdom of Saudi Arabia, including e-commerce, consumer-protection and tax regulations.</li>
<li>MHGR may suspend or close a partner account that breaches these terms, after settling any outstanding balance.</li>
<li>Submitting this application constitutes acceptance of these terms and of the processing of the partner data for verification and operational purposes.</li>
</ol>', 0
WHERE NOT EXISTS (SELECT 1 FROM [Setting] WHERE [Name] = 'vendorsettings.termsofservice' AND [StoreId] = 0);");

        //Arabic body — localized settings are stored as a LocalizedProperty over the Setting.Value column
        IfDatabase("sqlserver").Execute.Sql($@"
INSERT INTO [LocalizedProperty] ([EntityId], [LanguageId], [LocaleKeyGroup], [LocaleKey], [LocaleValue])
SELECT s.Id, l.Id, 'Setting', 'Value', N'<h3>شروط وأحكام الانضمام كشريك في محجر</h3>
<ol>
<li>يقرّ مقدّم الطلب بأن جميع البيانات والمستندات المقدّمة صحيحة وكاملة، وأنه مخوّل نظاماً بمزاولة النشاط داخل المملكة العربية السعودية.</li>
<li>يلتزم الشريك بعرض القطع التي يملكها أو المخوّل ببيعها أو تأجيرها فقط، وبصحة وصفها وصورها وسعرها.</li>
<li>يتحمّل الشريك كامل المسؤولية عن أصالة القطع المعروضة، ويحق لمحجر إيقاف أي إعلان أو حساب عند الاشتباه في المخالفة.</li>
<li>تُحتسب عمولة المنصّة على كل عملية بيع أو إيجار وفق ما هو معلن في اتفاقية الشراكة السارية.</li>
<li>في عمليات الإيجار، يلتزم الشريك بتسليم القطعة في الموعد وبالحالة المتفق عليها، وتُردّ مبالغ التأمين وفق سياسة المنصّة.</li>
<li>يلتزم الشريك بأنظمة المملكة العربية السعودية ذات العلاقة، بما في ذلك أنظمة التجارة الإلكترونية وحماية المستهلك والضريبة.</li>
<li>يحق لمحجر تعليق أو إنهاء حساب الشريك عند مخالفة هذه الشروط، مع تسوية المستحقات القائمة.</li>
<li>يُعدّ التقدّم بهذا الطلب موافقةً على هذه الشروط وعلى معالجة بيانات الشريك لأغراض التحقق والتشغيل.</li>
</ol>'
FROM [Setting] s
CROSS JOIN [Language] l
WHERE s.[Name] = 'vendorsettings.termsofservice' AND s.[StoreId] = 0
  AND {IS_ARABIC}
  AND NOT EXISTS (
      SELECT 1 FROM [LocalizedProperty] lp
      WHERE lp.EntityId = s.Id AND lp.LanguageId = l.Id
        AND lp.LocaleKeyGroup = 'Setting' AND lp.LocaleKey = 'Value');");
    }

    /// <summary>
    /// 3) Bilingual labels: storefront form, VendorType enum values, admin partner record and settings page, and
    /// the mobile-API error messages. Inserted only when missing, so a later edit in Admin is never clobbered.
    /// </summary>
    protected virtual void AddLocaleResources()
    {
        IfDatabase("sqlserver").Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN (l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%') THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    --storefront application form
    ('vendors.applyaccount.vendortype',                          N'نوع الشريك',                     N'Partner type'),
    ('vendors.applyaccount.vendortype.required',                 N'اختر نوع الشريك: فرد أو شركة.',   N'Please choose whether you are an individual or a corporation.'),
    ('vendors.applyaccount.nationalid',                          N'رقم الهوية / الإقامة',            N'National ID / Iqama number'),
    ('vendors.applyaccount.nationalid.hint',                     N'أدخل رقم الهوية الوطنية أو الإقامة', N'Enter your National ID or Iqama number.'),
    ('vendors.applyaccount.nationalid.required',                 N'رقم الهوية / الإقامة مطلوب.',      N'National ID / Iqama number is required.'),
    ('vendors.applyaccount.commercialregistrationnumber',        N'رقم السجل التجاري',               N'Commercial registration (CR) number'),
    ('vendors.applyaccount.commercialregistrationnumber.hint',   N'أدخل رقم السجل التجاري للمنشأة',   N'Enter the commercial registration number of the company.'),
    ('vendors.applyaccount.commercialregistrationnumber.required', N'رقم السجل التجاري مطلوب.',      N'Commercial registration number is required.'),
    ('vendors.applyaccount.vatnumber',                           N'الرقم الضريبي (اختياري)',         N'VAT number (optional)'),
    ('vendors.applyaccount.vatnumber.hint',                      N'أدخل الرقم الضريبي إن وُجد',       N'Enter your VAT registration number, if you have one.'),
    ('vendors.applyaccount.termsofservice.title',                N'الشروط والأحكام',                 N'Terms and conditions'),
    --VendorType enum, used by both the storefront and the admin dropdown
    ('enums.nop.core.domain.vendors.vendortype.notspecified',    N'غير محدد',                        N'Not specified'),
    ('enums.nop.core.domain.vendors.vendortype.individual',      N'فرد',                             N'Individual'),
    ('enums.nop.core.domain.vendors.vendortype.corporation',     N'شركة',                            N'Corporation'),
    --admin: the partner record
    ('admin.vendors.fields.vendortype',                          N'نوع الشريك',                      N'Partner type'),
    ('admin.vendors.fields.vendortype.hint',                     N'فرد أو شركة، كما حدّده مقدّم الطلب.', N'Individual or corporation, as stated on the application.'),
    ('admin.vendors.fields.nationalid',                          N'رقم الهوية / الإقامة',             N'National ID / Iqama number'),
    ('admin.vendors.fields.nationalid.hint',                     N'رقم هوية الشريك الفرد.',           N'National ID / Iqama number of an individual partner.'),
    ('admin.vendors.fields.commercialregistrationnumber',        N'رقم السجل التجاري',                N'Commercial registration (CR) number'),
    ('admin.vendors.fields.commercialregistrationnumber.hint',   N'رقم السجل التجاري للشريك الشركة.',  N'Commercial registration number of a corporate partner.'),
    ('admin.vendors.fields.vatnumber',                           N'الرقم الضريبي',                    N'VAT number'),
    ('admin.vendors.fields.vatnumber.hint',                      N'الرقم الضريبي للشريك (اختياري).',   N'VAT registration number of the partner (optional).'),
    ('admin.vendors.fields.termsacceptedon',                     N'الموافقة على الشروط',              N'Terms accepted'),
    ('admin.vendors.fields.termsacceptedon.hint',                N'تاريخ موافقة مقدّم الطلب على الشروط والأحكام.', N'When the applicant accepted the terms and conditions.'),
    ('admin.vendors.fields.termsacceptedon.no',                  N'لم تتم الموافقة',                  N'Not accepted'),
    --admin: vendor settings page
    ('admin.configuration.settings.vendor.termsofservice',       N'شروط وأحكام الشركاء',              N'Partner terms and conditions'),
    ('admin.configuration.settings.vendor.termsofservice.hint',  N'النص الذي يجب أن يوافق عليه مقدّم الطلب قبل إرسال طلب الشراكة (يقبل HTML، ولكل لغة نصّها).', N'The text an applicant must accept before submitting a partner application (HTML allowed, one per language).'),
    --mobile API
    ('mahjar.api.vendorapply.typerequired',                      N'اختر نوع الشريك: فرد أو شركة.',    N'Please choose whether you are an individual or a corporation.'),
    ('mahjar.api.vendorapply.nationalidrequired',                N'رقم الهوية / الإقامة مطلوب.',      N'National ID / Iqama number is required.'),
    ('mahjar.api.vendorapply.crrequired',                        N'رقم السجل التجاري مطلوب.',         N'Commercial registration number is required.'),
    ('mahjar.api.vendorapply.termsrequired',                     N'يجب الموافقة على الشروط والأحكام.', N'You must accept the terms and conditions.')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");

        //the stock strings are generic ('I accept terms of service'); make the partner wording explicit in both
        //languages, and give Arabic a deterministic value instead of whatever the imported pack happened to carry
        IfDatabase("sqlserver").Execute.Sql($@"
UPDATE r SET r.ResourceValue =
    CASE WHEN {IS_ARABIC} THEN N'أوافق على الشروط والأحكام' ELSE N'I accept the terms and conditions' END
FROM LocaleStringResource r
JOIN Language l ON l.Id = r.LanguageId
WHERE r.ResourceName = 'vendors.applyaccount.accepttermsofservice';

UPDATE r SET r.ResourceValue =
    CASE WHEN {IS_ARABIC} THEN N'يجب الموافقة على الشروط والأحكام.' ELSE N'Please accept the terms and conditions.' END
FROM LocaleStringResource r
JOIN Language l ON l.Id = r.LanguageId
WHERE r.ResourceName = 'vendors.applyaccount.accepttermsofservice.required';

UPDATE r SET r.ResourceValue =
    CASE WHEN {IS_ARABIC} THEN N'(اقرأ الشروط)' ELSE N'(read)' END
FROM LocaleStringResource r
JOIN Language l ON l.Id = r.LanguageId
WHERE r.ResourceName = 'vendors.applyaccount.accepttermsofservice.read';");
    }

    /// <summary>
    /// 4) Put the new details into the store-owner email. A token-level REPLACE keeps it safe against a body the
    /// owner has already edited, and the NOT LIKE guard makes a re-run a no-op. Any Arabic override of the same
    /// template (LocalizedProperty) gets the Arabic labels.
    /// </summary>
    protected virtual void ExtendStoreOwnerNotification()
    {
        IfDatabase("sqlserver").Execute.Sql(@"
UPDATE MessageTemplate
SET [Body] = REPLACE([Body], '%Vendor.Email%',
        '%Vendor.Email%<br />'
      + 'Partner type: %Vendor.VendorType%<br />'
      + 'National ID / Iqama: %Vendor.NationalId%<br />'
      + 'Commercial registration: %Vendor.CommercialRegistrationNumber%<br />'
      + 'VAT number: %Vendor.VatNumber%<br />'
      + 'Terms accepted: %Vendor.TermsAccepted%')
WHERE [Name] = 'VendorAccountApply.StoreOwnerNotification'
  AND [Body] LIKE '%[%]Vendor.Email[%]%'
  AND [Body] NOT LIKE '%[%]Vendor.VendorType[%]%';

UPDATE lp
SET lp.LocaleValue = REPLACE(lp.LocaleValue, '%Vendor.Email%',
        '%Vendor.Email%<br />'
      + N'نوع الشريك: %Vendor.VendorType%<br />'
      + N'رقم الهوية / الإقامة: %Vendor.NationalId%<br />'
      + N'رقم السجل التجاري: %Vendor.CommercialRegistrationNumber%<br />'
      + N'الرقم الضريبي: %Vendor.VatNumber%<br />'
      + N'الموافقة على الشروط: %Vendor.TermsAccepted%')
FROM LocalizedProperty lp
JOIN MessageTemplate mt ON mt.Id = lp.EntityId
WHERE lp.LocaleKeyGroup = 'MessageTemplate' AND lp.LocaleKey = 'Body'
  AND mt.[Name] = 'VendorAccountApply.StoreOwnerNotification'
  AND lp.LocaleValue LIKE '%[%]Vendor.Email[%]%'
  AND lp.LocaleValue NOT LIKE '%[%]Vendor.VendorType[%]%';");
    }

    #endregion
}
