// [Mahjar item-status]
using FluentMigrator;
using Nop.Core.Domain.Mahjar;
using Nop.Data.Extensions;
using Nop.Data.Mapping;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Creates the per-product condition report ("item status"): the <see cref="ProductItemStatus"/> header table
/// plus the <see cref="ProductItemStatusPicture"/> and <see cref="ProductItemStatusNote"/> child tables (all
/// three keyed on ProductId, mirroring ProductPicture), the ProductId lookup indexes, the
/// <c>itemstatussettings.*</c> setting rows, and every bilingual (en + ar) locale resource the feature needs:
/// the storefront/app <c>mahjar.itemstatus.*</c> strings, the three enums under the standard nopCommerce
/// <c>enums.nop.core.domain.mahjar.*</c> convention (so the admin dropdowns and the storefront/API resolve from
/// ONE source), and the admin editor <c>admin.catalog.products.itemstatus*</c> labels, hints and alerts.
///
/// Idempotent throughout (NOT EXISTS on every seeded row; FluentMigrator's version table guards the tables), so
/// it is deploy-safe, and a schema migration so it auto-applies on startup. SQL Server dialect, matching the
/// sibling Mahjar migrations. NOTE: settings and locale resources are cached — restart (or
/// POST /Admin/Common/ClearCache) after it runs. [Mahjar item-status]
/// </summary>
[NopSchemaMigration("2026-09-15 10:00:00", "Mahjar item-status: condition report (photos, video and notes)")]
public class ItemStatusMigration : ForwardOnlyMigration
{
    /// <summary>The Arabic-language predicate every Mahjar resource migration uses.</summary>
    private const string IS_ARABIC = "(l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')";

    /// <summary>The only dialect the seeded T-SQL targets; the SQLite test DB simply skips it.</summary>
    private const string DATABASE_TYPE = "sqlserver";

    public override void Up()
    {
        CreateTables();
        CreateIndexes();
        AddSettings();
        AddStorefrontResources();
        AddEnumResources();
        AddAdminResources();
    }

    #region Utilities

    /// <summary>
    /// 1) Schema — create the three tables via the LinqToDB builders. FluentMigrator's version table guards
    /// against re-runs, so no Schema.Table(...).Exists() guard is used (that guard misbehaves on the SQLite test
    /// provider, silently skipping creation — matching the base SchemaMigration, which also creates tables
    /// unconditionally).
    /// </summary>
    protected virtual void CreateTables()
    {
        Create.TableFor<ProductItemStatus>();
        Create.TableFor<ProductItemStatusPicture>();
        Create.TableFor<ProductItemStatusNote>();
    }

    /// <summary>
    /// 2) The ProductId lookup indexes. Every read of this feature is "give me the condition report of product X".
    ///
    /// The plain ProductId index is already created by <see cref="CreateTables"/>: nopCommerce's
    /// <c>ForeignKey&lt;T&gt;()</c> calls <c>Indexed()</c> first (see FluentMigratorExtensions), which emits an
    /// index named <c>IX_&lt;Table&gt;_&lt;Column&gt;</c>. So only the composite (ProductId, DisplayOrder) indexes
    /// of the two child tables are added here, under a name that spells out both columns — reusing the
    /// convention's <c>IX_&lt;Table&gt;_ProductId</c> name would collide (SQL Server error 1913) and abort startup.
    /// SQL Server only — the SQLite test DB does not need them.
    /// </summary>
    protected virtual void CreateIndexes()
    {
        var pictureTable = NameCompatibilityManager.GetTableName(typeof(ProductItemStatusPicture));
        if (!Schema.Table(pictureTable).Index("IX_ProductItemStatusPicture_ProductId_DisplayOrder").Exists())
            IfDatabase(DATABASE_TYPE).Create.Index("IX_ProductItemStatusPicture_ProductId_DisplayOrder").OnTable(pictureTable)
                .OnColumn(nameof(ProductItemStatusPicture.ProductId)).Ascending()
                .OnColumn(nameof(ProductItemStatusPicture.DisplayOrder)).Ascending();

        var noteTable = NameCompatibilityManager.GetTableName(typeof(ProductItemStatusNote));
        if (!Schema.Table(noteTable).Index("IX_ProductItemStatusNote_ProductId_DisplayOrder").Exists())
            IfDatabase(DATABASE_TYPE).Create.Index("IX_ProductItemStatusNote_ProductId_DisplayOrder").OnTable(noteTable)
                .OnColumn(nameof(ProductItemStatusNote.ProductId)).Ascending()
                .OnColumn(nameof(ProductItemStatusNote.DisplayOrder)).Ascending();
    }

    /// <summary>
    /// 3) ItemStatusSettings rows (the typed ISettings POCO defaults, persisted so admin edits stick).
    /// .mkv and .m3u8 are deliberately absent: the static-file MIME provider does not serve them.
    /// </summary>
    protected virtual void AddSettings()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
INSERT INTO [Setting] ([Name], [Value], [StoreId])
SELECT v.[Name], v.[Value], 0
FROM (VALUES
    ('itemstatussettings.enabled', 'True'),
    ('itemstatussettings.maxvideosizemb', '100'),
    ('itemstatussettings.maxpictures', '12'),
    ('itemstatussettings.allowedvideoextensions', '.mp4,.webm,.mov,.m4v')
) AS v ([Name], [Value])
WHERE NOT EXISTS (SELECT 1 FROM [Setting] s WHERE s.[Name] = v.[Name] AND s.[StoreId] = 0);");
    }

    /// <summary>
    /// 4) The storefront + mobile-app section strings (en + ar). Shared by the web product page and the API,
    /// so both surfaces stay word-for-word identical.
    /// </summary>
    protected virtual void AddStorefrontResources()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql($@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN {IS_ARABIC} THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    ('mahjar.itemstatus.title',             N'حالة القطعة',                                        N'Item Condition'),
    ('mahjar.itemstatus.subtitle',          N'صور وفيديو وملاحظات فعلية لهذه القطعة بالذات',        N'Real photos, video and notes for this exact item'),
    ('mahjar.itemstatus.grade',             N'التقييم العام',                                      N'Overall grade'),
    ('mahjar.itemstatus.inspectedon',       N'تاريخ الفحص',                                        N'Inspected on'),
    ('mahjar.itemstatus.summary',           N'نبذة عن الحالة',                                     N'Condition summary'),
    ('mahjar.itemstatus.photos',            N'صور القطعة',                                         N'Item photos'),
    ('mahjar.itemstatus.video',             N'فيديو القطعة',                                       N'Item video'),
    ('mahjar.itemstatus.notes',             N'ملاحظات الحالة',                                     N'Condition notes'),
    ('mahjar.itemstatus.videounsupported',  N'متصفحك لا يدعم تشغيل الفيديو.',                      N'Your browser does not support video playback.'),
    ('mahjar.itemstatus.watchvideo',        N'شاهد الفيديو',                                       N'Watch video'),
    ('mahjar.itemstatus.nonotes',           N'لا توجد ملاحظات مسجَّلة على هذه القطعة.',             N'No condition notes recorded for this item.')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }

    /// <summary>
    /// 5) The three enums under the standard nopCommerce enum-resource convention
    /// (<c>enums.{full type name}.{value}</c>), so ToSelectListAsync() in admin and GetLocalizedEnumAsync() on
    /// the storefront/API both resolve the very same labels.
    /// </summary>
    protected virtual void AddEnumResources()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql($@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN {IS_ARABIC} THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    --overall condition grade
    ('enums.nop.core.domain.mahjar.itemconditiongrade.notspecified', N'غير محدد',    N'Not specified'),
    ('enums.nop.core.domain.mahjar.itemconditiongrade.brandnew',     N'جديدة',       N'Brand new'),
    ('enums.nop.core.domain.mahjar.itemconditiongrade.likenew',      N'شبه جديدة',   N'Like new'),
    ('enums.nop.core.domain.mahjar.itemconditiongrade.excellent',    N'ممتازة',      N'Excellent'),
    ('enums.nop.core.domain.mahjar.itemconditiongrade.good',         N'جيدة',        N'Good'),
    ('enums.nop.core.domain.mahjar.itemconditiongrade.fair',         N'مقبولة',      N'Fair'),
    --condition note kind
    ('enums.nop.core.domain.mahjar.itemstatusnotetype.general',      N'ملاحظة عامة', N'General'),
    ('enums.nop.core.domain.mahjar.itemstatusnotetype.damage',       N'ضرر',         N'Damage'),
    ('enums.nop.core.domain.mahjar.itemstatusnotetype.wear',         N'أثر استعمال', N'Wear'),
    ('enums.nop.core.domain.mahjar.itemstatusnotetype.repair',       N'إصلاح سابق',  N'Previous repair'),
    ('enums.nop.core.domain.mahjar.itemstatusnotetype.missingpart',  N'قطعة ناقصة',  N'Missing part'),
    ('enums.nop.core.domain.mahjar.itemstatusnotetype.accessory',    N'ملحقات',      N'Accessory'),
    --condition note severity
    ('enums.nop.core.domain.mahjar.itemstatusnoteseverity.info',     N'معلومة',      N'Info'),
    ('enums.nop.core.domain.mahjar.itemstatusnoteseverity.minor',    N'بسيط',        N'Minor'),
    ('enums.nop.core.domain.mahjar.itemstatusnoteseverity.moderate', N'متوسط',       N'Moderate'),
    ('enums.nop.core.domain.mahjar.itemstatusnoteseverity.major',    N'كبير',        N'Major')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }

    /// <summary>
    /// 6) The admin/vendor product-editor tab: tab titles, every field label with its .hint twin, the grid
    /// add-buttons, the video panel and the client-side alerts.
    /// </summary>
    protected virtual void AddAdminResources()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql($@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN {IS_ARABIC} THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    --tab + panel titles
    ('admin.catalog.products.itemstatus',                     N'حالة القطعة',                                                              N'Item status'),
    ('admin.catalog.products.itemstatus.savebeforeedit',      N'احفظ المنتج أولاً لتتمكن من إضافة صور وفيديو وملاحظات الحالة.',            N'Save the product before you can add condition photos, video and notes.'),
    ('admin.catalog.products.itemstatus.info',                N'نظرة عامة',                                                                N'Overview'),
    ('admin.catalog.products.itemstatus.photos',              N'الصور',                                                                    N'Photos'),
    ('admin.catalog.products.itemstatus.video',               N'الفيديو',                                                                  N'Video'),
    ('admin.catalog.products.itemstatus.notes',               N'الملاحظات',                                                                N'Notes'),
    --overview fields
    ('admin.catalog.products.itemstatus.fields.conditiongrade',      N'التقييم العام',                                                     N'Overall grade'),
    ('admin.catalog.products.itemstatus.fields.conditiongrade.hint', N'اختر التقييم العام لحالة القطعة كما يظهر للعميل.',                   N'Choose the overall condition grade shown to the customer.'),
    ('admin.catalog.products.itemstatus.fields.summary',             N'نبذة عن الحالة',                                                    N'Condition summary'),
    ('admin.catalog.products.itemstatus.fields.summary.hint',        N'وصف مختصر لحالة القطعة يظهر أعلى تقرير الحالة.',                     N'A short description of the condition, shown at the top of the report.'),
    ('admin.catalog.products.itemstatus.fields.inspectedon',         N'تاريخ الفحص',                                                       N'Inspected on'),
    ('admin.catalog.products.itemstatus.fields.inspectedon.hint',    N'تاريخ فحص القطعة فعلياً. اتركه فارغاً إذا لم يُسجَّل.',              N'The date the item was physically inspected. Leave it empty when not recorded.'),
    ('admin.catalog.products.itemstatus.fields.showonproductpage',      N'عرض في صفحة المنتج',                                             N'Show on the product page'),
    ('admin.catalog.products.itemstatus.fields.showonproductpage.hint', N'عند التفعيل يظهر تقرير الحالة للعملاء في المتجر والتطبيق.',       N'When enabled, the condition report is shown to customers on the store and in the app.'),
    --video fields
    ('admin.catalog.products.itemstatus.fields.videourl',            N'رابط فيديو خارجي',                                                  N'External video URL'),
    ('admin.catalog.products.itemstatus.fields.videourl.hint',       N'الصق رابط فيديو خارجي إذا لم ترفع ملفاً. الملف المرفوع له الأولوية.', N'Paste an external video link when you do not upload a file. An uploaded file takes priority.'),
    --photo grid fields
    ('admin.catalog.products.itemstatus.fields.picture',             N'الصورة',                                                            N'Photo'),
    ('admin.catalog.products.itemstatus.fields.picture.hint',        N'صورة حقيقية لهذه القطعة بالذات، وليست صورة ترويجية.',               N'A real photo of this exact item, not a promotional render.'),
    ('admin.catalog.products.itemstatus.fields.caption',             N'التعليق',                                                           N'Caption'),
    ('admin.catalog.products.itemstatus.fields.caption.hint',        N'وصف قصير لما تُظهره الصورة، مثل: خدش على الإبزيم.',                  N'A short description of what the photo shows, for example: scratch on the clasp.'),
    ('admin.catalog.products.itemstatus.fields.displayorder',        N'ترتيب العرض',                                                       N'Display order'),
    ('admin.catalog.products.itemstatus.fields.displayorder.hint',   N'يحدد ترتيب الظهور تصاعدياً، الأصغر أولاً.',                          N'Sets the ascending display order, lowest first.'),
    --note grid fields
    ('admin.catalog.products.itemstatus.fields.notetype',            N'نوع الملاحظة',                                                      N'Note type'),
    ('admin.catalog.products.itemstatus.fields.notetype.hint',       N'صنّف الملاحظة: ضرر، أثر استعمال، إصلاح سابق، قطعة ناقصة أو ملحقات.', N'Classify the note: damage, wear, previous repair, missing part or accessory.'),
    ('admin.catalog.products.itemstatus.fields.severity',            N'درجة التأثير',                                                      N'Severity'),
    ('admin.catalog.products.itemstatus.fields.severity.hint',       N'مدى أهمية الملاحظة بالنسبة للعميل، وتُحدد لونها في المتجر والتطبيق.', N'How important the note is for the customer; it also drives its colour on the store and in the app.'),
    ('admin.catalog.products.itemstatus.fields.text',                N'نص الملاحظة',                                                       N'Note text'),
    ('admin.catalog.products.itemstatus.fields.text.hint',           N'اكتب الملاحظة كما سيقرأها العميل تماماً.',                           N'Write the note exactly as the customer will read it.'),
    --buttons + video panel
    ('admin.catalog.products.itemstatus.photos.addnew',       N'إضافة صور',                                                                N'Add photos'),
    ('admin.catalog.products.itemstatus.notes.addnew',        N'إضافة ملاحظة',                                                             N'Add a note'),
    ('admin.catalog.products.itemstatus.video.upload',        N'رفع فيديو',                                                                N'Upload a video'),
    ('admin.catalog.products.itemstatus.video.remove',        N'حذف الفيديو',                                                              N'Remove the video'),
    ('admin.catalog.products.itemstatus.video.current',       N'الفيديو الحالي',                                                           N'Current video'),
    ('admin.catalog.products.itemstatus.video.none',          N'لم يتم رفع فيديو بعد.',                                                    N'No video uploaded yet.'),
    --client-side alerts
    ('admin.catalog.products.itemstatus.alert.pictureadd',    N'تعذّرت إضافة الصورة.',                                                     N'The photo could not be added.'),
    ('admin.catalog.products.itemstatus.alert.noteempty',     N'اكتب نص الملاحظة.',                                                        N'Enter the note text.'),
    ('admin.catalog.products.itemstatus.alert.videomissing',  N'اختر ملف فيديو.',                                                          N'Choose a video file.'),
    ('admin.catalog.products.itemstatus.alert.videotoolarge', N'حجم الفيديو أكبر من الحد المسموح ({{0}} ميجابايت).',                          N'The video is larger than the allowed size ({{0}} MB).'),
    ('admin.catalog.products.itemstatus.alert.videobadformat', N'صيغة الفيديو غير مدعومة. الصيغ المدعومة: {{0}}',                             N'Unsupported video format. Supported: {{0}}'),
    ('admin.catalog.products.itemstatus.alert.videourlinvalid', N'رابط الفيديو غير صالح. استخدم رابطاً كاملاً يبدأ بـ http أو https.',           N'The video link is not valid. Use a full link that starts with http or https.')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }

    #endregion
}
