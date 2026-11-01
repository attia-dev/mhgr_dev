// [Mahjar partner-agreement]
using System.Data;
using FluentMigrator;
using Nop.Core.Domain.Mahjar.Agreements;
using Nop.Core.Domain.Vendors;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Installs the store owner's two master partner agreements — "اتفاقية الشريك الفرد" and
/// "اتفاقية الشريك التجاري (المؤسسات والشركات)" — on the "become a partner" application (web /vendor/apply and
/// mobile POST /api/customer/apply-vendor), replacing the short eight-clause terms body that stood in for them.
///
/// 1) <b>The acceptance record.</b> Stock nopCommerce, and this fork until now, recorded a single nullable
///    datetime. The agreements' closing table asks for far more, so the partner gains
///    <see cref="Vendor.ActivatedServicesId"/> (the "الخدمات المفعلة" row), the accepted document and version,
///    the moment, the IP, the acceptance method, a technical reference, and the accepted text itself frozen
///    with its hash. The settings body an admin edits later can then never change what a partner is held to.
///    Corporations additionally declare their authorised signatory and the capacity they sign in.
/// 2) <b>The agreements themselves.</b> Seeds <see cref="PartnerAgreementSettings"/>, whose two localized body
///    settings carry <see cref="PartnerAgreementContent.IndividualArabic"/> /
///    <see cref="PartnerAgreementContent.CorporateArabic"/> for the Arabic languages (the legally prevailing
///    text) and the English courtesy translations as the default value. Every "[●]" of the owner's documents is
///    a %Partner.* / %Store.* / %Agreement.* token, resolved from the commercial schedule in those same
///    settings by <c>IPartnerAgreementService</c>.
/// 3) <b>The wording.</b> Locale resources (en + ar) of the new application fields, the service and annex names
///    printed into the agreement, the API errors, and the admin acceptance panel.
///
/// The bodies are written through a parameterized command (they are long HTML with quotes), so nothing is
/// escaped by hand. Idempotent: the columns are guarded, the settings are inserted only when missing, the
/// localized rows are updated or inserted, and every resource insert is NOT EXISTS-guarded. SQL Server dialect,
/// matching the sibling Mahjar migrations.
///
/// NOTE ON EXISTING PARTNERS: everyone who applied before this ran accepted the old short terms, which the
/// v1.0 agreement replaces. They are deliberately left with a null <see cref="Vendor.AgreementVersion"/> — the
/// admin partner page then shows their acceptance as outstanding — rather than being back-filled as though they
/// had accepted a document they never saw. Their existing <see cref="Vendor.TermsAcceptedOnUtc"/> is untouched.
/// Activated services are back-filled to rental + sale, because that is what every partner could already do.
///
/// NOTE: settings and locale resources are cached — restart (or POST /Admin/Common/ClearCache) after it runs.
/// [Mahjar partner-agreement]
/// </summary>
[NopSchemaMigration("2026-09-20 10:00:00", "Mahjar partner agreement: the individual and corporate master agreements + their acceptance record")]
public class PartnerAgreementMigration : ForwardOnlyMigration
{
    /// <summary>The Arabic-language predicate every Mahjar resource migration uses.</summary>
    private const string IS_ARABIC = "(l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')";

    /// <summary>The T-SQL-only dialect guard every Mahjar migration uses.</summary>
    private const string DATABASE_TYPE = "sqlserver";

    public override void Up()
    {
        AddVendorColumns();
        SeedAgreementSettings();
        AddLocaleResources();
    }

    #region Utilities

    /// <summary>1) The acceptance record and the signatory of a corporate partner, on the Vendor table.</summary>
    protected virtual void AddVendorColumns()
    {
        var table = nameof(Vendor);

        void addText(string column, int length)
        {
            if (!Schema.Table(table).Column(column).Exists())
                Alter.Table(table).AddColumn(column).AsString(length).Nullable();
        }

        //the services the partner activated: 1 = rental, 2 = sale, 3 = both (PartnerServices, a flags enum)
        if (!Schema.Table(table).Column(nameof(Vendor.ActivatedServicesId)).Exists())
        {
            Alter.Table(table)
                .AddColumn(nameof(Vendor.ActivatedServicesId))
                .AsInt32()
                .NotNullable()
                .WithDefaultValue((int)PartnerServices.None);
        }

        addText(nameof(Vendor.AuthorizedSignatoryName), 400);
        addText(nameof(Vendor.AuthorizedSignatoryCapacity), 400);
        addText(nameof(Vendor.AgreementDocumentKey), 50);
        addText(nameof(Vendor.AgreementVersion), 20);
        addText(nameof(Vendor.AgreementAcceptedIp), 100);
        addText(nameof(Vendor.AgreementAcceptanceMethod), 200);
        addText(nameof(Vendor.AgreementBodyHash), 100);
        addText(nameof(Vendor.AgreementTechnicalRef), 50);

        if (!Schema.Table(table).Column(nameof(Vendor.AgreementAcceptedOnUtc)).Exists())
            Alter.Table(table).AddColumn(nameof(Vendor.AgreementAcceptedOnUtc)).AsDateTime2().Nullable();

        //the frozen copy of the accepted agreement — a whole document, so nvarchar(max)
        if (!Schema.Table(table).Column(nameof(Vendor.AgreementBody)).Exists())
            Alter.Table(table).AddColumn(nameof(Vendor.AgreementBody)).AsString(int.MaxValue).Nullable();

        //existing partners could already both rent out and sell, so that is what they are recorded as offering.
        //Their AGREEMENT fields stay null on purpose: they never saw this document (see the note on the class).
        IfDatabase(DATABASE_TYPE).Execute.Sql($@"
UPDATE [Vendor]
SET [{nameof(Vendor.ActivatedServicesId)}] = {(int)(PartnerServices.Rental | PartnerServices.Sale)}
WHERE [{nameof(Vendor.ActivatedServicesId)}] = {(int)PartnerServices.None};");
    }

    /// <summary>
    /// 2) The agreement settings, including the two bodies. As with every localized setting, the Setting row
    /// must exist before Admin can save a per-language value for it — SaveLocalizedSettingAsync silently
    /// no-ops when the setting is missing — so all of them are inserted here rather than lazily.
    /// </summary>
    protected virtual void SeedAgreementSettings()
    {
        //the scalar settings, inserted with the owner's starting values where there is a sensible one and left
        //empty where only the store owner can decide (an empty value prints as an em-dash in the agreement)
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
INSERT INTO [Setting] ([Name], [Value], [StoreId])
SELECT v.Name, v.Value, 0
FROM (VALUES
    ('partneragreementsettings.enabled',                  'True'),
    ('partneragreementsettings.version',                  '1.0'),
    ('partneragreementsettings.effectivedateutc',         ''),
    ('partneragreementsettings.operatornationaladdress',  ''),
    ('partneragreementsettings.verificationmethod',       ''),
    ('partneragreementsettings.rentalcommission',         ''),
    ('partneragreementsettings.salecommission',           ''),
    ('partneragreementsettings.listingfee',               ''),
    ('partneragreementsettings.inspectionfee',            ''),
    ('partneragreementsettings.deliveryfee',              ''),
    ('partneragreementsettings.storagefee',               ''),
    ('partneragreementsettings.minimumsettlement',        ''),
    ('partneragreementsettings.preparationtime',          ''),
    ('partneragreementsettings.settlementcycle',          ''),
    ('partneragreementsettings.settlementaccountterms',   ''),
    ('partneragreementsettings.claimresponsedays',        '5'),
    ('partneragreementsettings.terminationnoticedays',    '30'),
    ('partneragreementsettings.amicablesettlementdays',   '10'),
    ('partneragreementsettings.returninspectiondays',     '5'),
    ('partneragreementsettings.agreementterm',            ''),
    ('partneragreementsettings.renewalbasis',             ''),
    ('partneragreementsettings.commercialnotes',          '')
) AS v (Name, Value)
WHERE NOT EXISTS (SELECT 1 FROM [Setting] s WHERE s.[Name] = v.Name AND s.[StoreId] = 0);");

        //the two bodies: the English courtesy translation as the setting value, the Arabic (prevailing) text as
        //a LocalizedProperty per Arabic language, exactly as the rental agreement is installed
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

            var arabicLanguageIds = new List<int>();
            using (var cmd = command($"SELECT Id FROM Language l WHERE {IS_ARABIC}"))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    arabicLanguageIds.Add(reader.GetInt32(0));
            }

            void installBody(string settingName, string englishBody, string arabicBody)
            {
                //the setting row itself, carrying the English body
                int settingId;
                using (var cmd = command(@"
UPDATE [Setting] SET [Value] = @value WHERE [Name] = @name AND [StoreId] = 0 AND ([Value] IS NULL OR [Value] = '');

INSERT INTO [Setting] ([Name], [Value], [StoreId])
SELECT @name, @value, 0
WHERE NOT EXISTS (SELECT 1 FROM [Setting] WHERE [Name] = @name AND [StoreId] = 0);

SELECT TOP 1 Id FROM [Setting] WHERE [Name] = @name AND [StoreId] = 0;"))
                {
                    addParameter(cmd, "@name", settingName);
                    addParameter(cmd, "@value", englishBody);
                    settingId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                //the Arabic body, one LocalizedProperty row over the Setting.Value column per Arabic language
                foreach (var languageId in arabicLanguageIds)
                {
                    using var cmd = command(@"
UPDATE LocalizedProperty
SET LocaleValue = @value
WHERE EntityId = @entityId AND LanguageId = @languageId AND LocaleKeyGroup = 'Setting' AND LocaleKey = 'Value'
  AND (LocaleValue IS NULL OR LocaleValue = '');

INSERT INTO LocalizedProperty (EntityId, LanguageId, LocaleKeyGroup, LocaleKey, LocaleValue)
SELECT @entityId, @languageId, 'Setting', 'Value', @value
WHERE NOT EXISTS (
    SELECT 1 FROM LocalizedProperty
    WHERE EntityId = @entityId AND LanguageId = @languageId AND LocaleKeyGroup = 'Setting' AND LocaleKey = 'Value');");

                    addParameter(cmd, "@value", arabicBody);
                    addParameter(cmd, "@entityId", settingId);
                    addParameter(cmd, "@languageId", languageId);
                    cmd.ExecuteNonQuery();
                }
            }

            installBody("partneragreementsettings.individualbody",
                PartnerAgreementContent.IndividualEnglish, PartnerAgreementContent.IndividualArabic);
            installBody("partneragreementsettings.corporatebody",
                PartnerAgreementContent.CorporateEnglish, PartnerAgreementContent.CorporateArabic);
        });
    }

    /// <summary>3) The wording: the application fields, the agreement's own terms, the API errors, the admin panel.</summary>
    protected virtual void AddLocaleResources()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN " + IS_ARABIC + @" THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    -- the application form
    ('Vendors.ApplyAccount.AuthorizedSignatoryName',          N'اسم المفوض بالتوقيع',            'Authorised signatory'),
    ('Vendors.ApplyAccount.AuthorizedSignatoryName.Hint',     N'الاسم كما في المستند الرسمي',     'The name as it appears on the official document'),
    ('Vendors.ApplyAccount.AuthorizedSignatoryCapacity',      N'صفة المفوض بالتوقيع',            'Signing capacity'),
    ('Vendors.ApplyAccount.AuthorizedSignatoryCapacity.Hint', N'مالك، مدير، مفوض بتوكيل',        'Owner, manager, holder of a power of attorney'),
    ('Vendors.ApplyAccount.ActivatedServices',                N'الخدمات المفعلة',                 'Activated services'),
    ('Vendors.ApplyAccount.ActivatedServices.Hint',           N'تسري أحكام ملحق كل خدمة عند تفعيلها فقط. يمكن تفعيل خدمة أو إيقافها لاحقًا دون إنهاء الاتفاقية.', 'The annex of a service applies only while that service is activated. A service can be activated or stopped later without ending the agreement.'),
    ('Vendors.ApplyAccount.ActivatedServices.Required',       N'يجب تفعيل خدمة واحدة على الأقل: التأجير أو البيع.', 'Activate at least one service: renting out or selling.'),
    ('Vendors.ApplyAccount.ActivateRental',                   N'التأجير',                         'Renting out'),
    ('Vendors.ApplyAccount.ActivateSale',                     N'البيع',                           'Selling'),
    ('Vendors.ApplyAccount.Agreement.Title',                  N'اتفاقية الشريك مع منصة محجر',     'Partner agreement with the MHGR platform'),
    ('Vendors.ApplyAccount.Agreement.Version',                N'الإصدار {0}',                     'Version {0}'),
    ('Vendors.ApplyAccount.AcceptAgreement',                  N'أقر بأنني اطلعت على الاتفاقية وملاحقها المفعلة وأوافق عليها', 'I confirm that I have read the agreement and its activated annexes, and I accept them'),
    ('Vendors.ApplyAccount.AcceptanceMethod.Web',             N'قبول إلكتروني عبر نموذج الانضمام في الموقع', 'Electronic acceptance through the website application form'),
    -- what the agreement prints for the activated services and the annexes they bring into force
    ('mahjar.partneragreement.services.none',                 N'لا يوجد',                         'None'),
    ('mahjar.partneragreement.services.rental',               N'التأجير',                         'Renting out'),
    ('mahjar.partneragreement.services.sale',                 N'البيع',                           'Selling'),
    ('mahjar.partneragreement.services.both',                 N'التأجير والبيع',                  'Renting out and selling'),
    ('mahjar.partneragreement.annexes.master',                N'الاتفاقية الرئيسية والملحق (أ)',   'The master agreement and Annex A'),
    ('mahjar.partneragreement.annexes.rental',                N'الملحق (ب) — التأجير',            'Annex B — rental'),
    ('mahjar.partneragreement.annexes.sale',                  N'الملحق (ج) — البيع',              'Annex C — sale'),
    -- the mobile application
    ('mahjar.api.vendorapply.servicesrequired',               N'يجب تفعيل خدمة واحدة على الأقل: التأجير أو البيع.', 'Activate at least one service: renting out or selling.'),
    ('mahjar.api.vendorapply.acceptancemethod',               N'قبول إلكتروني عبر تطبيق محجر',    'Electronic acceptance through the MHGR application'),
    -- the admin partner page
    ('Admin.Vendors.Fields.AuthorizedSignatoryName',          N'المفوض بالتوقيع',                 'Authorised signatory'),
    ('Admin.Vendors.Fields.AuthorizedSignatoryCapacity',      N'صفة المفوض بالتوقيع',             'Signing capacity'),
    ('Admin.Vendors.Fields.ActivatedServices',                N'الخدمات المفعلة',                 'Activated services'),
    ('Admin.Vendors.Fields.AgreementDocument',                N'الاتفاقية المقبولة',              'Accepted agreement'),
    ('Admin.Vendors.Fields.AgreementDocument.Individual',     N'اتفاقية الشريك الفرد',            'Individual partner agreement'),
    ('Admin.Vendors.Fields.AgreementDocument.Corporate',      N'اتفاقية الشريك التجاري',          'Corporate partner agreement'),
    ('Admin.Vendors.Fields.AgreementVersion',                 N'الإصدار',                          'Version'),
    ('Admin.Vendors.Fields.AgreementVersion.Outdated',        N'(إصدار سابق — لم يقبل الإصدار الساري)', '(an earlier version — the version in force has not been accepted)'),
    ('Admin.Vendors.Fields.AgreementAcceptedOn',              N'تاريخ ووقت القبول',               'Accepted on'),
    ('Admin.Vendors.Fields.AgreementAcceptedIp',              N'عنوان IP للقبول',                 'Acceptance IP address'),
    ('Admin.Vendors.Fields.AgreementAcceptanceMethod',        N'وسيلة القبول الإلكتروني',         'Electronic acceptance method'),
    ('Admin.Vendors.Fields.AgreementTechnicalRef',            N'المرجع التقني للقبول',            'Technical reference of the acceptance'),
    ('Admin.Vendors.Fields.AgreementBodyHash',                N'بصمة نص الاتفاقية (SHA-256)',     'Hash of the agreement text (SHA-256)'),
    ('Admin.Vendors.Fields.Agreement.View',                   N'عرض النسخة المقبولة',             'Open the accepted copy'),
    ('Admin.Vendors.Fields.Agreement.NotAccepted',            N'لم يقبل الشريك اتفاقية الشراكة بعد', 'The partner has not accepted a partner agreement yet'),
    -- the agreement settings, edited in Admin -> Settings -> Vendor settings
    ('Admin.Configuration.Settings.Vendor.PartnerAgreement',              N'اتفاقية الشريك',        'Partner agreement'),
    ('Admin.Configuration.Settings.Vendor.PartnerAgreement.Hint',         N'نص الاتفاقية والقيم التي تطبع داخلها عبر رموزها. تُعرض على مقدم الطلب بحسب نوع الشريك، وتُحفظ نسخة مجمدة مما قبله.', 'The agreement text and the values printed into it through its tokens. It is shown to an applicant according to their partner type, and a frozen copy of what they accepted is kept.'),
    ('Admin.Configuration.Settings.Vendor.PartnerAgreementEnabled',       N'تفعيل اتفاقية الشريك',  'Enable the partner agreement'),
    ('Admin.Configuration.Settings.Vendor.PartnerAgreementEnabled.Hint',  N'عند الإيقاف يعود نموذج الانضمام إلى نص الشروط والأحكام المختصر.', 'When it is off, the application form falls back to the short terms and conditions body.'),
    ('Admin.Configuration.Settings.Vendor.PartnerAgreementVersion',       N'إصدار الاتفاقية',       'Agreement version'),
    ('Admin.Configuration.Settings.Vendor.PartnerAgreementVersion.Hint',  N'يطبع عبر الرمز %Agreement.Version% ويثبت في سجل القبول. الشريك الذي قبل إصدارًا أقدم يظهر في صفحته أنه لم يقبل الإصدار الساري.', 'Printed through %Agreement.Version% and stamped on the acceptance record. A partner who accepted an earlier version is shown on their page as not having accepted the version in force.'),
    ('Admin.Configuration.Settings.Vendor.PartnerAgreementEffectiveDate', N'تاريخ نفاذ الاتفاقية',  'Agreement effective date'),
    ('Admin.Configuration.Settings.Vendor.PartnerAgreementEffectiveDate.Hint', N'الرمز %Agreement.EffectiveDate%.', 'Token %Agreement.EffectiveDate%.'),
    ('Admin.Configuration.Settings.Vendor.IndividualBody',                N'نص اتفاقية الشريك الفرد', 'Individual partner agreement text'),
    ('Admin.Configuration.Settings.Vendor.IndividualBody.Hint',           N'النص العربي هو النص السائد. حرره لكل لغة على حدة.', 'The Arabic text is the prevailing one. Edit it per language.'),
    ('Admin.Configuration.Settings.Vendor.CorporateBody',                 N'نص اتفاقية الشريك التجاري', 'Corporate partner agreement text'),
    ('Admin.Configuration.Settings.Vendor.CorporateBody.Hint',            N'النص العربي هو النص السائد. حرره لكل لغة على حدة.', 'The Arabic text is the prevailing one. Edit it per language.'),
    ('Admin.Configuration.Settings.Vendor.OperatorNationalAddress',       N'العنوان الوطني لمحجر',  'MHGR national address'),
    ('Admin.Configuration.Settings.Vendor.OperatorNationalAddress.Hint',  N'الرمز %Store.NationalAddress%.', 'Token %Store.NationalAddress%.'),
    ('Admin.Configuration.Settings.Vendor.VerificationMethod',            N'وسيلة التحقق من الهوية', 'Identity verification method'),
    ('Admin.Configuration.Settings.Vendor.VerificationMethod.Hint',       N'الرمز %Partner.VerificationMethod%.', 'Token %Partner.VerificationMethod%.'),
    ('Admin.Configuration.Settings.Vendor.CommercialSchedule',            N'الملحق (أ): الجدول التجاري', 'Annex A: the commercial schedule'),
    ('Admin.Configuration.Settings.Vendor.CommercialSchedule.Hint',       N'القيم التي تملأ فراغات الملحق (أ) في الاتفاقيتين. اترك الحقل فارغًا لتطبع شرطة.', 'The values filling the blanks of Annex A in both agreements. Leave a field empty to print a dash.'),
    ('Admin.Configuration.Settings.Vendor.RentalCommission',              N'عمولة التأجير',         'Rental commission'),
    ('Admin.Configuration.Settings.Vendor.RentalCommission.Hint',         N'نسبة أو مبلغ، يطبع كما يكتب هنا عبر الرمز %Partner.RentalCommission%.', 'A percentage or an amount, printed as written here through %Partner.RentalCommission%.'),
    ('Admin.Configuration.Settings.Vendor.SaleCommission',                N'عمولة البيع',           'Sale commission'),
    ('Admin.Configuration.Settings.Vendor.SaleCommission.Hint',           N'الرمز %Partner.SaleCommission%.', 'Token %Partner.SaleCommission%.'),
    ('Admin.Configuration.Settings.Vendor.ListingFee',                    N'رسوم الإدراج',          'Listing fee'),
    ('Admin.Configuration.Settings.Vendor.ListingFee.Hint',               N'الرمز %Partner.ListingFee% — يظهر في اتفاقية الشريك التجاري.', 'Token %Partner.ListingFee% — it appears in the corporate agreement.'),
    ('Admin.Configuration.Settings.Vendor.InspectionFee',                 N'رسوم الفحص',            'Inspection fee'),
    ('Admin.Configuration.Settings.Vendor.InspectionFee.Hint',            N'الرمز %Partner.InspectionFee%.', 'Token %Partner.InspectionFee%.'),
    ('Admin.Configuration.Settings.Vendor.DeliveryFee',                   N'رسوم التسليم/الاستلام', 'Delivery / pick-up fee'),
    ('Admin.Configuration.Settings.Vendor.DeliveryFee.Hint',              N'الرمز %Partner.DeliveryFee%.', 'Token %Partner.DeliveryFee%.'),
    ('Admin.Configuration.Settings.Vendor.StorageFee',                    N'رسوم التخزين',          'Storage fee'),
    ('Admin.Configuration.Settings.Vendor.StorageFee.Hint',               N'الرمز %Partner.StorageFee% — يظهر في اتفاقية الشريك التجاري.', 'Token %Partner.StorageFee% — it appears in the corporate agreement.'),
    ('Admin.Configuration.Settings.Vendor.MinimumSettlement',             N'الحد الأدنى للتسوية',   'Minimum settlement'),
    ('Admin.Configuration.Settings.Vendor.MinimumSettlement.Hint',        N'الرمز %Partner.MinimumSettlement% — يظهر في اتفاقية الشريك التجاري.', 'Token %Partner.MinimumSettlement% — it appears in the corporate agreement.'),
    ('Admin.Configuration.Settings.Vendor.PreparationTime',               N'مدة تجهيز الطلب',       'Order preparation time'),
    ('Admin.Configuration.Settings.Vendor.PreparationTime.Hint',          N'الرمز %Partner.PrepTime%، ويطبع بعده ""ساعة/يوم عمل"" فاكتب الرقم فقط.', 'Token %Partner.PrepTime%; the words ""hours / working days"" are printed after it, so write only the number.'),
    ('Admin.Configuration.Settings.Vendor.SettlementCycle',               N'مدة/دورية التسوية',     'Settlement period / cycle'),
    ('Admin.Configuration.Settings.Vendor.SettlementCycle.Hint',          N'الرمز %Partner.SettlementCycle%.', 'Token %Partner.SettlementCycle%.'),
    ('Admin.Configuration.Settings.Vendor.SettlementAccountTerms',        N'حساب التسوية المعتمد',  'Approved settlement account'),
    ('Admin.Configuration.Settings.Vendor.SettlementAccountTerms.Hint',   N'ما يطبع في خانة حساب التسوية عبر الرمز %Partner.SettlementAccount%. لا يخزن رقم الحساب على سجل الشريك — يجمع ويوثق خارج المنصة.', 'What is printed in the settlement-account row through %Partner.SettlementAccount%. The account number itself is not stored on the partner record — it is collected and verified outside the platform.'),
    ('Admin.Configuration.Settings.Vendor.ClaimResponseDays',             N'مهلة الرد على مطالبة تشغيلية (أيام عمل)', 'Operational claim response (working days)'),
    ('Admin.Configuration.Settings.Vendor.ClaimResponseDays.Hint',        N'الرمز %Partner.ClaimResponseDays%.', 'Token %Partner.ClaimResponseDays%.'),
    ('Admin.Configuration.Settings.Vendor.TerminationNoticeDays',         N'مدة الإشعار بالإنهاء (أيام)', 'Termination notice (days)'),
    ('Admin.Configuration.Settings.Vendor.TerminationNoticeDays.Hint',    N'الرمز %Partner.TerminationNoticeDays%.', 'Token %Partner.TerminationNoticeDays%.'),
    ('Admin.Configuration.Settings.Vendor.AmicableSettlementDays',        N'مهلة التسوية الودية (أيام عمل)', 'Amicable settlement (working days)'),
    ('Admin.Configuration.Settings.Vendor.AmicableSettlementDays.Hint',   N'الرمز %Partner.AmicableSettlementDays%.', 'Token %Partner.AmicableSettlementDays%.'),
    ('Admin.Configuration.Settings.Vendor.ReturnInspectionDays',          N'مهلة فحص المرتجع (أيام عمل)', 'Return inspection (working days)'),
    ('Admin.Configuration.Settings.Vendor.ReturnInspectionDays.Hint',     N'الرمز %Partner.ReturnInspectionDays%.', 'Token %Partner.ReturnInspectionDays%.'),
    ('Admin.Configuration.Settings.Vendor.AgreementTerm',                 N'مدة الاتفاقية',         'Agreement term'),
    ('Admin.Configuration.Settings.Vendor.AgreementTerm.Hint',            N'الرمز %Partner.AgreementTerm% في المادة (31) من اتفاقية الشريك التجاري.', 'Token %Partner.AgreementTerm% in article 31 of the corporate agreement.'),
    ('Admin.Configuration.Settings.Vendor.RenewalBasis',                  N'أساس تجديد الاتفاقية',  'Renewal basis'),
    ('Admin.Configuration.Settings.Vendor.RenewalBasis.Hint',             N'الرمز %Partner.RenewalBasis% في المادة (31) من اتفاقية الشريك التجاري.', 'Token %Partner.RenewalBasis% in article 31 of the corporate agreement.'),
    ('Admin.Configuration.Settings.Vendor.CommercialNotes',               N'ملاحظات تجارية',        'Commercial notes'),
    ('Admin.Configuration.Settings.Vendor.CommercialNotes.Hint',          N'الرمز %Partner.CommercialNotes%، يطبع في نهاية الملحق (أ).', 'Token %Partner.CommercialNotes%, printed at the end of Annex A.')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }

    #endregion
}
