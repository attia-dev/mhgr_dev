// [Mahjar handover]
using FluentMigrator;
using Nop.Core.Domain.Mahjar;
using Nop.Data.Extensions;
using Nop.Data.Mapping;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// The handover desk: documented condition checks of an item at every checkpoint of its life — intake from the
/// partner, before the customer signs, delivery to the customer, return of a rental — each with photos, video,
/// notes and damage, locked once confirmed.
///
/// 1) Creates the <see cref="ConditionCheck"/> and <see cref="ConditionCheckPicture"/> tables (their builders add the
///    FK indexes IX_ConditionCheck_ProductId, IX_ConditionCheckPicture_ConditionCheckId and
///    IX_ConditionCheckPicture_PictureId themselves) plus the queue lookup indexes, under names that can never
///    collide with those convention ones.
/// 2) Seeds every bilingual (en + ar) resource: the admin handover desk, the customer-facing condition section of
///    the contract pages, the customer-visible order notes and the two new notification types.
///
/// The handover-staff role and its grants live in <see cref="HandoverStaffAclMigration"/>, an update-pass
/// migration, because they need permissions that only exist once the permission installer has run.
/// Idempotent throughout; schema migration so it auto-applies on startup. SQL Server dialect.
/// NOTE: locale resources are cached — restart (or POST /Admin/Common/ClearCache) after it runs. [Mahjar handover]
/// </summary>
[NopSchemaMigration("2026-09-16 10:00:00", "Mahjar handover: condition checks at intake, before signing, delivery and return")]
public class HandoverMigration : ForwardOnlyMigration
{
    /// <summary>The Arabic-language predicate every Mahjar resource migration uses.</summary>
    private const string IS_ARABIC = "(l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')";

    /// <summary>The only dialect the seeded T-SQL targets; the SQLite test DB simply skips it.</summary>
    private const string DATABASE_TYPE = "sqlserver";

    public override void Up()
    {
        CreateTables();
        CreateIndexes();
        AddAdminResources();
        AddCustomerResources();
    }

    #region Utilities

    /// <summary>1) Schema — FluentMigrator's version table guards re-runs (see ContractsMigration for why no Exists guard).</summary>
    protected virtual void CreateTables()
    {
        Create.TableFor<ConditionCheck>();
        Create.TableFor<ConditionCheckPicture>();
    }

    /// <summary>The queue lookups: by order item + checkpoint, by contract + checkpoint, by checkpoint + status.</summary>
    protected virtual void CreateIndexes()
    {
        var checkTable = NameCompatibilityManager.GetTableName(typeof(ConditionCheck));

        if (!Schema.Table(checkTable).Index("IX_ConditionCheck_OrderItem_Type").Exists())
            IfDatabase(DATABASE_TYPE).Create.Index("IX_ConditionCheck_OrderItem_Type").OnTable(checkTable)
                .OnColumn(nameof(ConditionCheck.OrderItemId)).Ascending()
                .OnColumn(nameof(ConditionCheck.CheckTypeId)).Ascending();

        if (!Schema.Table(checkTable).Index("IX_ConditionCheck_Contract_Type").Exists())
            IfDatabase(DATABASE_TYPE).Create.Index("IX_ConditionCheck_Contract_Type").OnTable(checkTable)
                .OnColumn(nameof(ConditionCheck.ContractRequestId)).Ascending()
                .OnColumn(nameof(ConditionCheck.CheckTypeId)).Ascending();

        if (!Schema.Table(checkTable).Index("IX_ConditionCheck_Type_Status").Exists())
            IfDatabase(DATABASE_TYPE).Create.Index("IX_ConditionCheck_Type_Status").OnTable(checkTable)
                .OnColumn(nameof(ConditionCheck.CheckTypeId)).Ascending()
                .OnColumn(nameof(ConditionCheck.StatusId)).Ascending();
    }

    /// <summary>2a) The admin handover desk (en + ar).</summary>
    protected virtual void AddAdminResources()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN " + IS_ARABIC + @" THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    ('admin.mahjar.handover.menu',                        N'الاستلام والتسليم',                     N'Handovers'),
    ('admin.mahjar.handover.list.title',                  N'الاستلام والتسليم وفحص الحالة',          N'Handovers and condition checks'),
    ('admin.mahjar.handover.list.stage',                  N'المرحلة',                               N'Stage'),
    ('admin.mahjar.handover.list.hint',                   N'يُوثَّق كل استلام أو تسليم بصور أو فيديو لحالة القطعة. اختر المرحلة لعرض ما يلزم إنجازه.', N'Every handover is documented with condition photos or a video. Pick a stage to see what needs doing.'),
    ('admin.mahjar.handover.stage.awaitingprecontract',   N'قبل التوقيع: عقود بانتظار صور الحالة',   N'Before signing: contracts awaiting condition media'),
    ('admin.mahjar.handover.stage.awaitingdelivery',      N'بانتظار التسليم للعميل',                 N'Awaiting delivery to the customer'),
    ('admin.mahjar.handover.stage.awaitingreturn',        N'إيجارات بانتظار الإرجاع',                N'Rentals awaiting return'),
    ('admin.mahjar.handover.stage.intakeinprogress',      N'استلام من الشركاء قيد التنفيذ',          N'Partner intakes in progress'),
    ('admin.mahjar.handover.stage.completed',             N'الفحوصات المؤكدة',                      N'Confirmed checks'),
    ('admin.mahjar.handover.type.intake',                 N'الاستلام من الشريك',                    N'Intake from partner'),
    ('admin.mahjar.handover.type.precontract',            N'قبل التوقيع',                           N'Before signing'),
    ('admin.mahjar.handover.type.delivery',               N'التسليم للعميل',                        N'Delivery to customer'),
    ('admin.mahjar.handover.type.return',                 N'الإرجاع من العميل',                     N'Return from customer'),
    ('admin.mahjar.handover.status.notstarted',           N'لم يبدأ',                               N'Not started'),
    ('admin.mahjar.handover.status.draft',                N'قيد التنفيذ',                           N'In progress'),
    ('admin.mahjar.handover.status.confirmed',            N'مؤكد',                                  N'Confirmed'),
    ('admin.mahjar.handover.fields.product',              N'المنتج',                                N'Product'),
    ('admin.mahjar.handover.fields.customer',             N'العميل',                                N'Customer'),
    ('admin.mahjar.handover.fields.reference',            N'العقد / الطلب',                         N'Contract / order'),
    ('admin.mahjar.handover.fields.period',               N'مدة الإيجار',                           N'Rental period'),
    ('admin.mahjar.handover.fields.status',               N'الحالة',                                N'Status'),
    ('admin.mahjar.handover.fields.media',                N'الوسائط',                               N'Media'),
    ('admin.mahjar.handover.fields.type',                 N'نقطة الفحص',                            N'Checkpoint'),
    ('admin.mahjar.handover.fields.date',                 N'التاريخ',                               N'Date'),
    ('admin.mahjar.handover.fields.createdby',            N'بدأه',                                  N'Started by'),
    ('admin.mahjar.handover.fields.confirmedby',          N'أكّده',                                 N'Confirmed by'),
    ('admin.mahjar.handover.fields.confirmedon',          N'تاريخ التأكيد',                         N'Confirmed on'),
    ('admin.mahjar.handover.fields.grade',                N'تقييم الحالة',                          N'Condition grade'),
    ('admin.mahjar.handover.fields.conditionnotes',       N'ملاحظات الحالة (تظهر للعميل)',           N'Condition notes (shown to the customer)'),
    ('admin.mahjar.handover.fields.hasdamage',            N'يوجد ضرر',                              N'Damage found'),
    ('admin.mahjar.handover.fields.damageseverity',       N'درجة الضرر',                            N'Damage severity'),
    ('admin.mahjar.handover.fields.damagedescription',    N'وصف الضرر (يظهر للعميل)',               N'Damage description (shown to the customer)'),
    ('admin.mahjar.handover.fields.internalcomment',      N'ملاحظة داخلية على عملية الاستلام (للموظفين فقط)', N'Internal comment on the handover (staff only)'),
    ('admin.mahjar.handover.fields.deposit',              N'مبلغ التأمين',                          N'Security deposit'),
    ('admin.mahjar.handover.fields.damagededuction',      N'الخصم من مبلغ التأمين',                  N'Deduction from the deposit'),
    ('admin.mahjar.handover.fields.refunddue',            N'المبلغ المسترد من التأمين',              N'Deposit to refund'),
    ('admin.mahjar.handover.fields.videourl',             N'رابط فيديو خارجي',                      N'External video URL'),
    ('admin.mahjar.handover.fields.photo',                N'الصورة',                                N'Photo'),
    ('admin.mahjar.handover.fields.caption',              N'التعليق',                               N'Caption'),
    ('admin.mahjar.handover.fields.displayorder',         N'ترتيب العرض',                           N'Display order'),
    ('admin.mahjar.handover.fields.pictureid',            N'الصور',                                 N'Photos'),
    ('admin.mahjar.handover.photos',                      N'الصور',                                 N'Photos'),
    ('admin.mahjar.handover.photos.addnew',               N'إضافة صور',                             N'Add photos'),
    ('admin.mahjar.handover.video',                       N'الفيديو',                               N'Video'),
    ('admin.mahjar.handover.video.none',                  N'لا يوجد فيديو مرفوع.',                  N'No video uploaded.'),
    ('admin.mahjar.handover.video.upload',                N'رفع فيديو',                             N'Upload video'),
    ('admin.mahjar.handover.video.remove',                N'حذف الفيديو',                           N'Remove video'),
    ('admin.mahjar.handover.open',                        N'فتح',                                   N'Open'),
    ('admin.mahjar.handover.check.title',                 N'فحص الحالة',                            N'Condition check'),
    ('admin.mahjar.handover.backtolist',                  N'العودة إلى الاستلام والتسليم',            N'back to handovers'),
    ('admin.mahjar.handover.buttons.confirmintake',       N'تأكيد الاستلام من الشريك',               N'Confirm intake'),
    ('admin.mahjar.handover.buttons.confirmdelivery',     N'تأكيد التسليم',                          N'Confirm delivery'),
    ('admin.mahjar.handover.buttons.confirmreturn',       N'تأكيد الإرجاع',                          N'Confirm return'),
    ('admin.mahjar.handover.buttons.importfromproduct',   N'نسخ من تقرير حالة المنتج',                N'Copy from the product condition report'),
    ('admin.mahjar.handover.buttons.deletedraft',         N'حذف المسودة',                           N'Delete draft'),
    ('admin.mahjar.handover.confirm.warning',             N'بعد التأكيد يُقفل الفحص ولا يمكن تعديله. هل تريد المتابعة؟', N'Once confirmed, this check is locked and can no longer be edited. Continue?'),
    ('admin.mahjar.handover.saved',                       N'تم حفظ فحص الحالة.',                    N'The condition check has been saved.'),
    ('admin.mahjar.handover.confirmed',                   N'تم تأكيد فحص الحالة وقفله.',             N'The condition check has been confirmed and locked.'),
    ('admin.mahjar.handover.deleted',                     N'تم حذف المسودة.',                       N'The draft has been deleted.'),
    ('admin.mahjar.handover.imported',                    N'تم نسخ {0} من وسائط تقرير حالة المنتج.', N'Copied {0} media item(s) from the product condition report.'),
    ('admin.mahjar.handover.imported.none',               N'لا توجد وسائط في تقرير حالة المنتج لنسخها.', N'The product condition report has no media to copy.'),
    ('admin.mahjar.handover.locked',                      N'هذا الفحص مؤكد ومقفل ولا يمكن تعديله.',  N'This check is confirmed and locked; it can no longer be edited.'),
    ('admin.mahjar.handover.mediarequired',               N'أرفق صورة واحدة على الأقل أو فيديو قبل التأكيد.', N'Attach at least one photo or a video before confirming.'),
    ('admin.mahjar.handover.notfound',                    N'لم يُعثر على العنصر المطلوب.',           N'The requested handover item was not found.'),
    ('admin.mahjar.handover.photo.addfailed',             N'تعذّر إضافة الصورة. استخدم ملف صورة (JPG أو PNG أو WEBP).', N'The photo could not be added. Use an image file (JPG, PNG or WEBP).'),
    ('admin.mahjar.handover.photo.limit',                 N'الحد الأقصى للصور هو {0}.',              N'You can attach at most {0} photos.'),
    ('admin.mahjar.handover.photosfmt',                   N'{0} صورة',                              N'{0} photo(s)'),
    ('admin.mahjar.handover.withvideo',                   N'فيديو',                                 N'video'),
    ('admin.mahjar.handover.nomedia',                     N'لا توجد وسائط',                         N'No media'),
    ('admin.mahjar.handover.precontract.hint',            N'تظهر هذه الصور والفيديو للعميل بجانب العقد قبل التوقيع، وتُقفل عند إرسال العقد.', N'These photos and video are shown to the customer next to the contract before signing, and are locked when the contract is sent.'),
    ('admin.mahjar.handover.precontract.lockedonsend',    N'يُقفل فحص ما قبل التوقيع تلقائيًا عند إرسال العقد.', N'A before-signing check is locked automatically when the contract is sent.'),
    ('admin.mahjar.handover.precontract.notpending',      N'لا يمكن إضافة صور ما قبل التوقيع بعد إرسال العقد.', N'Before-signing media can no longer be added once the contract has been sent.'),
    ('admin.mahjar.handover.precontract.missing',         N'أرفق صور أو فيديو الحالة لكل صنف قبل إرسال العقد. الأصناف الناقصة: {0}', N'Attach condition photos or a video for every item before sending the contract. Missing: {0}'),
    ('admin.mahjar.handover.precontract.card',            N'الحالة قبل التوقيع',                    N'Condition before signing'),
    ('admin.mahjar.handover.precontract.ready',           N'تم إرفاق الوسائط',                      N'Media attached'),
    ('admin.mahjar.handover.precontract.missingshort',    N'لا توجد وسائط بعد',                     N'No media yet'),
    ('admin.mahjar.handover.precontract.blocked',         N'لا يمكن إرسال العقد قبل إرفاق صور أو فيديو الحالة لكل صنف.', N'The contract cannot be sent until every item has condition photos or a video.'),
    ('admin.mahjar.handover.conditionchecks',             N'فحوصات الحالة',                         N'Condition checks'),
    ('admin.mahjar.handover.intake.new',                  N'تسجيل استلام من شريك',                   N'Record an intake'),
    ('admin.mahjar.handover.intake.title',                N'تسجيل استلام منتج من شريك',              N'Record a product intake from a partner'),
    ('admin.mahjar.handover.intake.search',               N'ابحث عن المنتج بالاسم أو رمز SKU',        N'Search the product by name or SKU'),
    ('admin.mahjar.handover.intake.start',                N'بدء الاستلام',                           N'Start intake'),
    ('admin.mahjar.handover.intake.noresults',            N'لا توجد منتجات مطابقة.',                 N'No matching products.'),
    ('admin.mahjar.handover.intake.hint',                 N'عند التأكيد تحل الصور والفيديو المسجلة هنا فورًا محل وسائط تقرير حالة القطعة في صفحة المنتج، وتُضاف الأضرار إلى ملاحظات الحالة.', N'On confirmation the photos and video recorded here immediately replace the media of the Item condition report on the product page, and any damage is added to its condition notes.'),
    ('admin.mahjar.handover.return.hint',                 N'عند التأكيد يُسجَّل الإرجاع ومبلغ التأمين المسترد، وتحل الصور والفيديو فورًا محل وسائط تقرير حالة القطعة في صفحة المنتج.', N'On confirmation the return and the deposit to refund are recorded, and the photos and video immediately replace the media of the Item condition report on the product page.'),
    ('admin.mahjar.handover.delivery.hint',               N'يُسجَّل التسليم في الطلب، ويُحدَّث الطلب إلى تم التسليم تلقائيًا عند تسليم جميع أصنافه.', N'The handover is noted on the order, and the order is marked delivered automatically once all of its items are handed over.'),
    ('admin.mahjar.handover.delivery.notpaid',            N'الطلب غير مدفوع بعد، ولا يمكن تسليم القطعة.', N'This order is not paid yet, so the item cannot be handed over.'),
    ('admin.mahjar.handover.delivery.orderupdated',       N'تم تسليم جميع أصناف الطلب، وتم تحديث حالته إلى تم التسليم.', N'All items of the order are handed over; the order has been marked as delivered.'),
    ('admin.mahjar.handover.delivery.syncfailed',         N'تم تسجيل التسليم، لكن تعذّر تحديث حالة الطلب تلقائيًا: {0}', N'The handover was recorded, but the order status could not be updated automatically: {0}'),
    ('admin.mahjar.handover.published',                   N'تم تحديث تقرير حالة القطعة في صفحة المنتج.', N'The Item condition report on the product page has been updated.'),
    ('admin.mahjar.handover.publishfailed',               N'تم تأكيد الفحص، لكن تعذّر تحديث تقرير حالة المنتج: {0}', N'The check was confirmed, but the product condition report could not be updated: {0}'),
    ('admin.mahjar.handover.return.notrental',            N'يمكن إرجاع الأصناف المؤجرة فقط.',        N'Only rented items can be returned.'),
    ('admin.mahjar.handover.return.notdelivered',         N'لم يتم تسليم هذه القطعة للعميل بعد.',     N'This item has not been handed over to the customer yet.'),
    ('admin.mahjar.handover.return.deductiontoohigh',     N'لا يمكن أن يتجاوز الخصم مبلغ التأمين ({0}).', N'The deduction cannot exceed the security deposit ({0}).'),
    ('admin.mahjar.handover.return.refundnote',           N'استرد {0} من مبلغ التأمين للعميل.',       N'Refund {0} of the security deposit to the customer.'),
    ('admin.mahjar.handover.order',                       N'طلب رقم {0}',                           N'Order #{0}')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }

    /// <summary>2b) The customer-facing strings: contract pages, order notes, notification types (en + ar).</summary>
    protected virtual void AddCustomerResources()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN " + IS_ARABIC + @" THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    ('mahjar.contracts.condition.title',              N'حالة القطع',                            N'Item condition'),
    ('mahjar.contracts.condition.signhint',           N'راجع صور وفيديو الحالة الحالية لكل صنف قبل التوقيع؛ فهي جزء مما توقّع عليه.', N'Review the photos and video of the current condition of each item before signing. They are part of what you sign.'),
    ('mahjar.contracts.condition.type.precontract',   N'قبل التوقيع',                           N'Before signing'),
    ('mahjar.contracts.condition.type.delivery',      N'عند التسليم',                           N'At delivery'),
    ('mahjar.contracts.condition.type.return',        N'عند الإرجاع',                           N'At return'),
    ('mahjar.contracts.condition.recordedon',         N'تاريخ التوثيق',                         N'Recorded on'),
    ('mahjar.contracts.condition.damage',             N'ضرر مسجّل',                             N'Damage recorded'),
    ('mahjar.contracts.condition.deduction',          N'المخصوم من مبلغ التأمين',                N'Deducted from the security deposit'),
    ('mahjar.contracts.condition.refund',             N'المبلغ المسترد من التأمين',              N'Deposit to refund'),
    ('mahjar.contracts.condition.none',               N'لا توجد سجلات حالة بعد.',                N'No condition records yet.'),
    ('mahjar.contracts.view.title',                   N'تفاصيل العقد',                          N'Contract details'),
    ('mahjar.contracts.view.signature',               N'التوقيع',                               N'Signature'),
    ('mahjar.contracts.view.signedby',                N'وقّع بواسطة',                           N'Signed by'),
    ('mahjar.contracts.view.notavailable',            N'هذا العقد غير متاح للعرض بعد.',          N'This contract is not available to view yet.'),
    ('mahjar.handover.ordernote.delivered',           N'تم تسليم {0} للعميل مع توثيق صور وفيديو حالته.', N'{0} was handed over to the customer; its condition was documented with photos and video.'),
    ('mahjar.handover.ordernote.returned',            N'تم استلام {0} المؤجر. مبلغ التأمين: {1}، الخصم: {2}، المبلغ المسترد: {3}.', N'The rented {0} was returned. Security deposit: {1}, deduction: {2}, to refund: {3}.'),
    ('mahjar.notifications.type.rentalreturnrecordedbuyer.title', N'تم استلام القطعة المؤجرة',   N'Rental return recorded'),
    ('mahjar.notifications.type.rentalreturnrecordedbuyer.body',  N'استلمنا القطعة المؤجرة من طلبك رقم {0}. المبلغ المسترد من التأمين: {1}.', N'We received the rented item from your order #{0}. Deposit to refund: {1}.'),
    ('mahjar.notifications.type.rentalreturnrecordedadmin.title', N'إرجاع إيجار: استرداد تأمين مستحق', N'Rental returned: deposit refund due'),
    ('mahjar.notifications.type.rentalreturnrecordedadmin.body',  N'الطلب رقم {0}: تم إرجاع {1}. يُسترد {2} من تأمين قدره {3}.', N'Order #{0}: {1} was returned. Refund {2} of the {3} deposit.')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }

    #endregion
}
