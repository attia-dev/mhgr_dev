// [Mahjar home-slides]
using FluentMigrator;
using Nop.Core.Domain.Mahjar;
using Nop.Data.Extensions;
using Nop.Data.Mapping;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Creates the admin-managed home slides: the <see cref="HomeSlide"/> table (with a composite index on the hot
/// "active slides of a placement" read), the bilingual (en + ar) labels of its four enums under the standard
/// nopCommerce <c>enums.nop.core.domain.mahjar.*</c> convention, and three seed Hero slides (English base
/// columns + Arabic <c>LocalizedProperty</c> rows) so the web and app home pages have a carousel out of the box.
///
/// The seed resolves its pictures by the stable <c>Picture.SeoFilename</c> of the watch renders (like
/// <see cref="CategoryImagesMigration"/>), its product targets through <c>Product_Picture_Mapping</c> and its
/// category target by the English category name — never by id — so it is deploy-safe across environments; a
/// slide whose picture is missing is skipped. Idempotent throughout (FluentMigrator's version table guards the
/// table, the seed only runs on an empty table, and every resource / localized row is inserted only when
/// missing), and a schema migration so it auto-applies on startup. SQL Server dialect, matching the sibling
/// Mahjar migrations. NOTE: locale resources are cached — restart (or POST /Admin/Common/ClearCache) after it
/// runs. [Mahjar home-slides]
/// </summary>
[NopSchemaMigration("2026-09-29 10:00:00", "Mahjar home-slides: admin-managed home hero/feature slides")]
public class HomeSlidesMigration : ForwardOnlyMigration
{
    /// <summary>The Arabic-language predicate every Mahjar resource migration uses.</summary>
    private const string IS_ARABIC = "(l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')";

    /// <summary>The only dialect the seeded T-SQL targets; the SQLite test DB simply skips it.</summary>
    private const string DATABASE_TYPE = "sqlserver";

    public override void Up()
    {
        CreateTables();
        CreateIndexes();
        AddEnumResources();
        SeedSlides();
        SeedArabicSlideTexts();
    }

    #region Utilities

    /// <summary>
    /// 1) Schema — create the table via the LinqToDB builder. FluentMigrator's version table guards against
    /// re-runs, so no Schema.Table(...).Exists() guard is used (that guard misbehaves on the SQLite test provider,
    /// silently skipping creation — matching the base SchemaMigration, which also creates tables unconditionally).
    /// </summary>
    protected virtual void CreateTables()
    {
        Create.TableFor<HomeSlide>();
    }

    /// <summary>
    /// 2) The (PlacementId, Published, DisplayOrder) index behind "the published slides of a placement, in
    /// display order". The name spells out all three columns so it can never collide with a convention
    /// <c>IX_&lt;Table&gt;_&lt;Column&gt;</c> index. SQL Server only — the SQLite test DB does not need it.
    /// </summary>
    protected virtual void CreateIndexes()
    {
        var homeSlideTable = NameCompatibilityManager.GetTableName(typeof(HomeSlide));
        if (!Schema.Table(homeSlideTable).Index("IX_HomeSlide_Placement_Published_DisplayOrder").Exists())
            IfDatabase(DATABASE_TYPE).Create.Index("IX_HomeSlide_Placement_Published_DisplayOrder").OnTable(homeSlideTable)
                .OnColumn(nameof(HomeSlide.PlacementId)).Ascending()
                .OnColumn(nameof(HomeSlide.Published)).Ascending()
                .OnColumn(nameof(HomeSlide.DisplayOrder)).Ascending();
    }

    /// <summary>
    /// 3) The four enums under the standard nopCommerce enum-resource convention
    /// (<c>enums.{full type name}.{value}</c>), so ToSelectListAsync() in admin and GetLocalizedEnumAsync() on
    /// the storefront/API resolve the very same labels.
    /// </summary>
    protected virtual void AddEnumResources()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql($@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN {IS_ARABIC} THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    --layout
    ('enums.nop.core.domain.mahjar.homeslidelayout.fullbleed',           N'ملء الشاشة',      N'Full bleed'),
    ('enums.nop.core.domain.mahjar.homeslidelayout.spotlight',           N'عرض القطعة',      N'Spotlight'),
    --text position
    ('enums.nop.core.domain.mahjar.homeslidecontentposition.start',      N'البداية',         N'Start'),
    ('enums.nop.core.domain.mahjar.homeslidecontentposition.center',     N'الوسط',           N'Center'),
    ('enums.nop.core.domain.mahjar.homeslidecontentposition.end',        N'النهاية',         N'End'),
    --placement
    ('enums.nop.core.domain.mahjar.homeslideplacement.hero',             N'الواجهة الرئيسية', N'Hero carousel'),
    ('enums.nop.core.domain.mahjar.homeslideplacement.feature',          N'شريط مميز',       N'Feature band'),
    --link target
    ('enums.nop.core.domain.mahjar.homeslidetargettype.none',            N'بدون رابط',       N'No link'),
    ('enums.nop.core.domain.mahjar.homeslidetargettype.product',         N'منتج',            N'Product'),
    ('enums.nop.core.domain.mahjar.homeslidetargettype.category',        N'فئة',             N'Category'),
    ('enums.nop.core.domain.mahjar.homeslidetargettype.manufacturer',    N'علامة تجارية',    N'Brand'),
    ('enums.nop.core.domain.mahjar.homeslidetargettype.vendor',          N'شريك',            N'Partner'),
    ('enums.nop.core.domain.mahjar.homeslidetargettype.search',          N'بحث',             N'Search'),
    ('enums.nop.core.domain.mahjar.homeslidetargettype.url',             N'رابط خارجي',      N'External link')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }

    /// <summary>
    /// 4) The three seed Hero slides (Spotlight layout, text at the start side, all channels, published), one row
    /// at a time so each can resolve its own picture and target. English base columns; Arabic follows in
    /// <see cref="SeedArabicSlideTexts"/>, matched on the English title. Runs only on an EMPTY table, so an admin
    /// who deleted the seed slides never gets them back. A picture is resolved by SeoFilename among PRODUCT
    /// pictures only (Product_Picture_Mapping), preferring the one mapped to a live product: handover condition
    /// photos (ConditionCheckPicture) and condition-report photos reuse the product's slug as their SeoFilename, and
    /// an internal photo must never be published on the public home carousel. A slide with no such picture is
    /// skipped; a missing product/category target degrades to "no link" (TargetTypeId 0).
    /// Enum backing values: LayoutId 10 = Spotlight, ContentPositionId 0 = Start, PlacementId 0 = Hero,
    /// TargetTypeId 10 = Product / 20 = Category.
    /// </summary>
    protected virtual void SeedSlides()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
IF NOT EXISTS (SELECT 1 FROM HomeSlide)
BEGIN
    DECLARE @now datetime2(6) = SYSUTCDATETIME();
    DECLARE @pic int, @targetType int, @targetId int;

    --1) royal-moonphase -> its product
    SELECT @pic = NULL, @targetType = 0, @targetId = 0;
    SELECT TOP 1 @pic = p.Id
    FROM Picture p
    WHERE p.SeoFilename = N'royal-moonphase'
      AND EXISTS (SELECT 1 FROM Product_Picture_Mapping ppm WHERE ppm.PictureId = p.Id) --a product render only, never a private handover/condition photo sharing the slug
    ORDER BY CASE WHEN EXISTS (SELECT 1 FROM Product_Picture_Mapping ppm JOIN Product pr ON pr.Id = ppm.ProductId
                               WHERE ppm.PictureId = p.Id AND pr.Deleted = 0) THEN 0 ELSE 1 END, p.Id;
    SELECT TOP 1 @targetType = 10, @targetId = ppm.ProductId
    FROM Product_Picture_Mapping ppm JOIN Product pr ON pr.Id = ppm.ProductId
    WHERE ppm.PictureId = @pic AND pr.Deleted = 0
    ORDER BY ppm.DisplayOrder, ppm.Id;
    IF @pic IS NOT NULL
        INSERT INTO HomeSlide ([PictureId], [MobilePictureId], [VideoUrl], [Kicker], [Title], [Subtitle], [ButtonText],
            [LayoutId], [ContentPositionId], [OverlayOpacity], [PlacementId], [TargetTypeId], [TargetId], [TargetValue],
            [ShowOnWeb], [ShowOnApp], [Published], [DisplayOrder], [StartDateUtc], [EndDateUtc], [CreatedOnUtc], [UpdatedOnUtc])
        VALUES (@pic, 0, NULL, N'The MHGR Collection', N'A World of Fine Watches',
            N'Own or rent the finest timepieces from trusted partners across the Kingdom.', N'Discover the watch',
            10, 0, 45, 0, @targetType, @targetId, NULL,
            1, 1, 1, 1, NULL, NULL, @now, @now);

    --2) skeleton-tourbillon -> its product
    SELECT @pic = NULL, @targetType = 0, @targetId = 0;
    SELECT TOP 1 @pic = p.Id
    FROM Picture p
    WHERE p.SeoFilename = N'skeleton-tourbillon'
      AND EXISTS (SELECT 1 FROM Product_Picture_Mapping ppm WHERE ppm.PictureId = p.Id) --a product render only, never a private handover/condition photo sharing the slug
    ORDER BY CASE WHEN EXISTS (SELECT 1 FROM Product_Picture_Mapping ppm JOIN Product pr ON pr.Id = ppm.ProductId
                               WHERE ppm.PictureId = p.Id AND pr.Deleted = 0) THEN 0 ELSE 1 END, p.Id;
    SELECT TOP 1 @targetType = 10, @targetId = ppm.ProductId
    FROM Product_Picture_Mapping ppm JOIN Product pr ON pr.Id = ppm.ProductId
    WHERE ppm.PictureId = @pic AND pr.Deleted = 0
    ORDER BY ppm.DisplayOrder, ppm.Id;
    IF @pic IS NOT NULL
        INSERT INTO HomeSlide ([PictureId], [MobilePictureId], [VideoUrl], [Kicker], [Title], [Subtitle], [ButtonText],
            [LayoutId], [ContentPositionId], [OverlayOpacity], [PlacementId], [TargetTypeId], [TargetId], [TargetValue],
            [ShowOnWeb], [ShowOnApp], [Published], [DisplayOrder], [StartDateUtc], [EndDateUtc], [CreatedOnUtc], [UpdatedOnUtc])
        VALUES (@pic, 0, NULL, N'Rent with confidence', N'Wear Luxury for the Occasion',
            N'Rent your watch for a day or more, with a fully refundable deposit.', N'Rent now',
            10, 0, 45, 0, @targetType, @targetId, NULL,
            1, 1, 1, 2, NULL, NULL, @now, @now);

    --3) diver-pro-300 -> the 'Luxury Watches' category
    SELECT @pic = NULL, @targetType = 0, @targetId = 0;
    SELECT TOP 1 @pic = p.Id
    FROM Picture p
    WHERE p.SeoFilename = N'diver-pro-300'
      AND EXISTS (SELECT 1 FROM Product_Picture_Mapping ppm WHERE ppm.PictureId = p.Id) --a product render only, never a private handover/condition photo sharing the slug
    ORDER BY CASE WHEN EXISTS (SELECT 1 FROM Product_Picture_Mapping ppm JOIN Product pr ON pr.Id = ppm.ProductId
                               WHERE ppm.PictureId = p.Id AND pr.Deleted = 0) THEN 0 ELSE 1 END, p.Id;
    SELECT TOP 1 @targetType = 20, @targetId = c.Id
    FROM Category c
    WHERE c.[Name] = N'Luxury Watches' AND c.Deleted = 0
    ORDER BY c.Id;
    IF @pic IS NOT NULL
        INSERT INTO HomeSlide ([PictureId], [MobilePictureId], [VideoUrl], [Kicker], [Title], [Subtitle], [ButtonText],
            [LayoutId], [ContentPositionId], [OverlayOpacity], [PlacementId], [TargetTypeId], [TargetId], [TargetValue],
            [ShowOnWeb], [ShowOnApp], [Published], [DisplayOrder], [StartDateUtc], [EndDateUtc], [CreatedOnUtc], [UpdatedOnUtc])
        VALUES (@pic, 0, NULL, N'Authenticity assured', N'Every Watch Inspected and Documented',
            N'We record each piece''s condition in photos at intake and at handover.', N'Explore the collection',
            10, 0, 45, 0, @targetType, @targetId, NULL,
            1, 1, 1, 3, NULL, NULL, @now, @now);
END;");
    }

    /// <summary>
    /// 5) The Arabic texts of the seed slides — one LocalizedProperty row per (slide, ar language, text field),
    /// the slide matched on its English seed title. Each row only when missing.
    /// </summary>
    protected virtual void SeedArabicSlideTexts()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql($@"
INSERT INTO LocalizedProperty (EntityId, LanguageId, LocaleKeyGroup, LocaleKey, LocaleValue)
SELECT s.Id, l.Id, 'HomeSlide', v.LocaleKey, v.ArValue
FROM (VALUES
    (N'A World of Fine Watches',              'Kicker',     N'مجموعة محجر'),
    (N'A World of Fine Watches',              'Title',      N'عالمٌ من الساعات الفاخرة'),
    (N'A World of Fine Watches',              'Subtitle',   N'اقتنِ أو استأجر أرقى الساعات من شركاء موثوقين في أنحاء المملكة.'),
    (N'A World of Fine Watches',              'ButtonText', N'اكتشف الساعة'),
    (N'Wear Luxury for the Occasion',         'Kicker',     N'استئجار بثقة'),
    (N'Wear Luxury for the Occasion',         'Title',      N'ارتدِ الفخامة لمناسبتك'),
    (N'Wear Luxury for the Occasion',         'Subtitle',   N'استأجر ساعتك ليومٍ أو أكثر، مع تأمينٍ مستردٍّ بالكامل.'),
    (N'Wear Luxury for the Occasion',         'ButtonText', N'استأجر الآن'),
    (N'Every Watch Inspected and Documented', 'Kicker',     N'أصالةٌ مضمونة'),
    (N'Every Watch Inspected and Documented', 'Title',      N'كل ساعةٍ تُفحص وتُوثَّق'),
    (N'Every Watch Inspected and Documented', 'Subtitle',   N'نوثّق حالة كل قطعة بالصور عند الاستلام وعند التسليم.'),
    (N'Every Watch Inspected and Documented', 'ButtonText', N'استكشف المجموعة')
) AS v (EnTitle, LocaleKey, ArValue)
JOIN HomeSlide s ON s.Title = v.EnTitle
CROSS JOIN Language l
WHERE {IS_ARABIC}
  AND NOT EXISTS (
      SELECT 1 FROM LocalizedProperty lp
      WHERE lp.EntityId = s.Id AND lp.LanguageId = l.Id
        AND lp.LocaleKeyGroup = 'HomeSlide' AND lp.LocaleKey = v.LocaleKey);");
    }

    #endregion
}
