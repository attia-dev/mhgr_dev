// [Mahjar partner-request]
using FluentMigrator;
using Nop.Core.Domain.Mahjar.Agreements;
using Nop.Core.Domain.Vendors;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Gives a partner application a life of its own, instead of it being an inactive partner row an admin had to
/// know to look for: it is submitted, it waits in a queue, and the store owner approves or rejects it.
///
/// 1) <b>The review state.</b> <see cref="Vendor.ApplicationStatusId"/> (pending / approved / rejected),
///    when it was submitted and reviewed, and the rejection reason the applicant is shown.
/// 2) <b>Who applied.</b> <see cref="Vendor.ApplicationCustomerId"/>. A rejection clears
///    <c>Customer.VendorId</c> so the customer can apply again, and without this column the record would no
///    longer say whose application it was.
/// 3) <b>The wording</b> of the queue in Admin -> Partner requests.
///
/// Back-fill: a partner who accepted terms went through the application form, so they become approved if they
/// are active and pending if they are not. Partners created straight in admin keep status 0 and never appear in
/// the queue. The submitted date is taken from the acceptance stamp, which is the closest thing to it that
/// exists — nopCommerce does not record when a vendor row was created.
///
/// Idempotent (column guards, NOT EXISTS on every insert, the back-fill only touches status 0). SQL Server
/// dialect. NOTE: locale resources are cached — restart (or POST /Admin/Common/ClearCache) after it runs.
/// [Mahjar partner-request]
/// </summary>
[NopSchemaMigration("2026-09-21 10:00:00", "Mahjar partner request: the application review queue")]
public class PartnerRequestMigration : ForwardOnlyMigration
{
    /// <summary>The Arabic-language predicate every Mahjar resource migration uses.</summary>
    private const string IS_ARABIC = "(l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')";

    /// <summary>The T-SQL-only dialect guard every Mahjar migration uses.</summary>
    private const string DATABASE_TYPE = "sqlserver";

    public override void Up()
    {
        AddVendorColumns();
        BackfillExistingApplications();
        AddLocaleResources();
    }

    #region Utilities

    /// <summary>1) The review state on the Vendor table.</summary>
    protected virtual void AddVendorColumns()
    {
        var table = nameof(Vendor);

        void addInt(string column)
        {
            if (!Schema.Table(table).Column(column).Exists())
                Alter.Table(table).AddColumn(column).AsInt32().NotNullable().WithDefaultValue(0);
        }

        void addDate(string column)
        {
            if (!Schema.Table(table).Column(column).Exists())
                Alter.Table(table).AddColumn(column).AsDateTime2().Nullable();
        }

        addInt(nameof(Vendor.ApplicationStatusId));
        addInt(nameof(Vendor.ApplicationCustomerId));
        addDate(nameof(Vendor.ApplicationSubmittedOnUtc));
        addDate(nameof(Vendor.ApplicationReviewedOnUtc));

        if (!Schema.Table(table).Column(nameof(Vendor.ApplicationRejectionReason)).Exists())
            Alter.Table(table).AddColumn(nameof(Vendor.ApplicationRejectionReason)).AsString(1000).Nullable();
    }

    /// <summary>2) Existing applications join the queue in the state they are already in.</summary>
    protected virtual void BackfillExistingApplications()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql($@"
UPDATE v
SET [{nameof(Vendor.ApplicationStatusId)}] = CASE WHEN v.[Active] = 1
        THEN {(int)PartnerApplicationStatus.Approved}
        ELSE {(int)PartnerApplicationStatus.Pending} END,
    [{nameof(Vendor.ApplicationSubmittedOnUtc)}] = COALESCE(v.[{nameof(Vendor.AgreementAcceptedOnUtc)}], v.[{nameof(Vendor.TermsAcceptedOnUtc)}]),
    [{nameof(Vendor.ApplicationReviewedOnUtc)}] = CASE WHEN v.[Active] = 1
        THEN COALESCE(v.[{nameof(Vendor.AgreementAcceptedOnUtc)}], v.[{nameof(Vendor.TermsAcceptedOnUtc)}])
        ELSE NULL END
FROM [Vendor] v
WHERE v.[{nameof(Vendor.ApplicationStatusId)}] = {(int)PartnerApplicationStatus.NotAnApplication}
  AND v.[{nameof(Vendor.TermsAcceptedOnUtc)}] IS NOT NULL;");

        //who applied, from the customer still pointing at the partner
        IfDatabase(DATABASE_TYPE).Execute.Sql($@"
UPDATE v
SET [{nameof(Vendor.ApplicationCustomerId)}] = c.Id
FROM [Vendor] v
CROSS APPLY (SELECT TOP 1 Id FROM [Customer] WHERE VendorId = v.Id AND Deleted = 0 ORDER BY Id) c
WHERE v.[{nameof(Vendor.ApplicationCustomerId)}] = 0
  AND v.[{nameof(Vendor.ApplicationStatusId)}] <> {(int)PartnerApplicationStatus.NotAnApplication};");
    }

    /// <summary>3) The wording of the queue and of the review screen.</summary>
    protected virtual void AddLocaleResources()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN " + IS_ARABIC + @" THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    -- the queue
    ('Admin.Mahjar.PartnerRequests',                       N'طلبات الشركاء',          'Partner requests'),
    ('Admin.Mahjar.PartnerRequests.Hint',                  N'طلبات الانضمام كشريك، بانتظار المراجعة. يمكنك الاطلاع على البيانات المقدمة ونسخة الاتفاقية التي قبلها مقدم الطلب قبل الموافقة أو الرفض.', 'Applications to become a partner, waiting for review. You can go through the submitted details and the copy of the agreement the applicant accepted before approving or rejecting.'),
    ('Admin.Mahjar.PartnerRequests.Pending',               N'قيد المراجعة',           'Pending'),
    ('Admin.Mahjar.PartnerRequests.List.SearchStatus',     N'الحالة',                 'Status'),
    ('Admin.Mahjar.PartnerRequests.List.SearchName',       N'اسم الشريك أو بريده',    'Partner name or e-mail'),
    ('Admin.Mahjar.PartnerRequests.List.NoRequests',       N'لا توجد طلبات',          'There are no requests'),
    ('Admin.Mahjar.PartnerRequests.Fields.Name',           N'اسم الشريك',             'Partner name'),
    ('Admin.Mahjar.PartnerRequests.Fields.Email',          N'البريد الإلكتروني',      'E-mail'),
    ('Admin.Mahjar.PartnerRequests.Fields.VendorType',     N'نوع الشريك',             'Partner type'),
    ('Admin.Mahjar.PartnerRequests.Fields.Services',       N'الخدمات المفعلة',        'Activated services'),
    ('Admin.Mahjar.PartnerRequests.Fields.SubmittedOn',    N'تاريخ الطلب',            'Submitted on'),
    ('Admin.Mahjar.PartnerRequests.Fields.ReviewedOn',     N'تاريخ المراجعة',         'Reviewed on'),
    ('Admin.Mahjar.PartnerRequests.Fields.Status',         N'الحالة',                 'Status'),
    ('Admin.Mahjar.PartnerRequests.Fields.Customer',       N'حساب مقدم الطلب',        'Applicant account'),
    ('Admin.Mahjar.PartnerRequests.Fields.Description',    N'وصف النشاط',             'Business description'),
    ('Admin.Mahjar.PartnerRequests.Fields.RejectionReason', N'سبب الرفض',             'Rejection reason'),
    ('Admin.Mahjar.PartnerRequests.Fields.RejectionReason.Hint', N'يظهر لمقدم الطلب حتى يعرف ما يصححه قبل التقدم مرة أخرى.', 'Shown to the applicant so they know what to correct before applying again.'),

    -- the statuses
    ('Admin.Mahjar.PartnerRequests.Status.Pending',        N'قيد المراجعة',           'Pending'),
    ('Admin.Mahjar.PartnerRequests.Status.Approved',       N'مقبول',                  'Approved'),
    ('Admin.Mahjar.PartnerRequests.Status.Rejected',       N'مرفوض',                  'Rejected'),
    ('Admin.Mahjar.PartnerRequests.Status.All',            N'الكل',                   'All'),

    -- the review screen
    ('Admin.Mahjar.PartnerRequests.Review',                N'مراجعة الطلب',           'Review the request'),
    ('Admin.Mahjar.PartnerRequests.Review.Identity',       N'البيانات المقدمة',       'The submitted details'),
    ('Admin.Mahjar.PartnerRequests.Review.Agreement',      N'الاتفاقية المقبولة',     'The accepted agreement'),
    ('Admin.Mahjar.PartnerRequests.Review.Agreement.Hint', N'النسخة المجمّدة كما قبلها مقدم الطلب، ببصمتها ومرجعها التقني.', 'The frozen copy exactly as the applicant accepted it, with its hash and technical reference.'),
    ('Admin.Mahjar.PartnerRequests.Review.OpenAgreement',  N'فتح نسخة الاتفاقية',     'Open the agreement copy'),
    ('Admin.Mahjar.PartnerRequests.Review.NoAgreement',    N'لم تُسجَّل نسخة اتفاقية لهذا الطلب.', 'No agreement copy was recorded for this request.'),
    ('Admin.Mahjar.PartnerRequests.Review.OpenPartner',    N'فتح صفحة الشريك',        'Open the partner page'),
    ('Admin.Mahjar.PartnerRequests.Approve',               N'قبول الطلب',             'Approve'),
    ('Admin.Mahjar.PartnerRequests.Approve.Confirm',       N'سيتم تفعيل حساب الشريك ومنحه صلاحية إدارة منتجاته. متابعة؟', 'The partner account will be activated and granted access to manage its products. Continue?'),
    ('Admin.Mahjar.PartnerRequests.Approved',              N'تم قبول الطلب وتفعيل حساب الشريك.', 'The request was approved and the partner account activated.'),
    ('Admin.Mahjar.PartnerRequests.Reject',                N'رفض الطلب',              'Reject'),
    ('Admin.Mahjar.PartnerRequests.Reject.Confirm',        N'سيتم رفض الطلب، ويمكن لمقدم الطلب التقدم من جديد. متابعة؟', 'The request will be rejected, and the applicant will be able to apply again. Continue?'),
    ('Admin.Mahjar.PartnerRequests.Rejected',              N'تم رفض الطلب، ويمكن لمقدم الطلب التقدم من جديد.', 'The request was rejected, and the applicant can apply again.'),
    ('Admin.Mahjar.PartnerRequests.Reject.ReasonRequired', N'اكتب سبب الرفض أولًا.',  'Write the reason for the rejection first.'),
    ('Admin.Mahjar.PartnerRequests.AlreadyReviewed',       N'سبق مراجعة هذا الطلب.',  'This request has already been reviewed.'),

    -- what the applicant sees afterwards
    ('Vendors.ApplyAccount.Rejected',                      N'لم يُقبل طلبك السابق. يمكنك تصحيح البيانات والتقدم مرة أخرى.', 'Your previous application was not accepted. You can correct the details and apply again.'),
    ('Vendors.ApplyAccount.Rejected.Reason',               N'سبب الرفض: {0}',         'Reason: {0}')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }

    #endregion
}
