// [Mahjar home-motion]
using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// The bilingual (en + ar) storefront strings of the "Living Home" page: the Hero carousel controls and a11y labels,
/// the trust marquee, the "Own or Rent" band, the unified section headings (categories, featured watches, partners),
/// the featured "view all" row and the partner call-to-action eyebrow. Watches only; vendors are partners
/// (شريك/شركاء). Idempotent (each string is inserted per language only when missing) and a schema migration, so it
/// auto-applies on startup against an existing database. SQL Server dialect like the sibling Mahjar migrations.
/// It also renames the stock "Featured products" home heading to "Featured Watches" (only while it still has the
/// stock text, so an admin's own wording is never overwritten).
/// NOTE: locale resources are cached — restart (or POST /Admin/Common/ClearCache) after it runs. [Mahjar home-motion]
/// </summary>
[NopSchemaMigration("2026-09-29 10:20:00", "Mahjar home-motion: storefront home resources")]
public class HomeMotionMigration : ForwardOnlyMigration
{
    /// <summary>The Arabic-language predicate every Mahjar resource migration uses.</summary>
    private const string IS_ARABIC = "(l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')";

    /// <summary>The only dialect the seeded T-SQL targets; the SQLite test DB simply skips it.</summary>
    private const string DATABASE_TYPE = "sqlserver";

    public override void Up()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql($@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN {IS_ARABIC} THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    -- hero carousel
    ('mahjar.home.hero.label',            N'واجهة محجر',                          N'The MHGR showcase'),
    ('mahjar.home.hero.carousel',         N'عرض متتابع',                          N'carousel'),
    ('mahjar.home.hero.slide',            N'شريحة',                               N'slide'),
    ('mahjar.home.hero.goto',             N'الانتقال إلى الشريحة {{0}} من {{1}}',     N'Go to slide {{0}} of {{1}}'),
    ('mahjar.home.hero.previous',         N'الشريحة السابقة',                     N'Previous slide'),
    ('mahjar.home.hero.next',             N'الشريحة التالية',                     N'Next slide'),
    ('mahjar.home.hero.pause',            N'إيقاف العرض مؤقتًا',                  N'Pause the slideshow'),
    ('mahjar.home.hero.play',             N'تشغيل العرض',                         N'Play the slideshow'),
    ('mahjar.home.hero.scroll',           N'اكتشف',                               N'Discover'),
    ('mahjar.home.marquee.pause',         N'إيقاف الشريط مؤقتًا',                  N'Pause the ticker'),
    ('mahjar.home.marquee.play',          N'تشغيل الشريط',                        N'Play the ticker'),
    -- trust marquee
    ('mahjar.home.marquee.label',         N'لماذا محجر',                          N'Why MHGR'),
    ('mahjar.home.marquee.authenticity',  N'أصالة مضمونة',                        N'Authenticity assured'),
    ('mahjar.home.marquee.ownorrent',     N'اقتنِ أو استأجر',                     N'Own or rent'),
    ('mahjar.home.marquee.deposit',       N'تأمين مستردّ بالكامل',                N'Fully refundable deposit'),
    ('mahjar.home.marquee.partners',      N'شركاء موثوقون',                       N'Trusted partners'),
    ('mahjar.home.marquee.inspection',    N'فحص وتوثيق عند التسليم',              N'Inspected & documented at handover'),
    ('mahjar.home.marquee.curated',       N'ساعات مختارة بعناية',                 N'Carefully curated watches'),
    -- own or rent band
    ('mahjar.home.ownrent.eyebrow',       N'طريقتان بمعيارٍ واحد',                N'Two ways, one standard'),
    ('mahjar.home.ownrent.title',         N'اقتنِ أو استأجر',                     N'Own or Rent'),
    ('mahjar.home.ownrent.own.eyebrow',   N'للاقتناء',                            N'To own'),
    ('mahjar.home.ownrent.own.title',     N'اقتنِها مدى الحياة',                  N'Own it for life'),
    ('mahjar.home.ownrent.own.text',      N'اشترِها من شريكٍ موثوق — كل قطعةٍ تُفحص وتُوثَّق قبل أن تصلك.',
                                          N'Buy it outright from a trusted partner — every piece is inspected and documented before it reaches you.'),
    ('mahjar.home.ownrent.rent.eyebrow',  N'للاستئجار',                           N'To rent'),
    ('mahjar.home.ownrent.rent.title',    N'استأجرها لمناسبتك',                   N'Rent it for the occasion'),
    ('mahjar.home.ownrent.rent.text',     N'ارتدِها ليومٍ أو أكثر بسعرٍ يومي، وأيّ تأمينٍ يُستردّ بالكامل عند الإرجاع.',
                                          N'Wear it for a day or more at a daily price; any security deposit is fully refunded on return.'),
    ('mahjar.home.ownrent.explore',       N'استكشف {{0}}',                         N'Explore {{0}}'),
    ('mahjar.home.ownrent.seefeatured',   N'شاهد الساعات المميزة',                N'See the featured watches'),
    ('mahjar.home.ownrent.deposit',       N'+ تأمين مستردّ',                      N'+ refundable deposit'),
    ('mahjar.home.ownrent.owncta',        N'اقتنِ {{0}}',                          N'Own the {{0}}'),
    ('mahjar.home.ownrent.rentcta',       N'استأجر {{0}}',                         N'Rent the {{0}}'),
    ('mahjar.home.ownrent.perday',        N'{{0}} لليوم',                          N'{{0}} / day'),
    ('mahjar.home.ownrent.browse',        N'تصفّح {{0}}',                          N'Browse {{0}}'),
    -- unified section headings
    ('mahjar.home.categories.eyebrow',    N'المجموعات',                           N'The collections'),
    ('mahjar.home.categories.title',      N'تسوّق حسب الفئة',                     N'Shop by Category'),
    ('mahjar.home.categories.discover',   N'اكتشف',                               N'Discover'),
    ('mahjar.home.featured.eyebrow',      N'مختارات محجر',                        N'The MHGR selection'),
    ('mahjar.home.featured.viewall',      N'عرض كل الساعات حسب الفئة',            N'View all watches by category'),
    ('mahjar.home.partners.eyebrow',      N'شركاؤنا',                             N'Our partners'),
    ('mahjar.home.sellercta.eyebrow',     N'للشركاء',                             N'For partners'),
    ('mahjar.home.features.label',        N'حكايات محجر',                         N'MHGR stories')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);

-- the home heading of a watches-only store: 'Featured products' -> 'Featured Watches' (stock texts only)
UPDATE r SET r.ResourceValue = CASE WHEN {IS_ARABIC} THEN N'ساعات مميزة' ELSE N'Featured Watches' END
FROM LocaleStringResource r
INNER JOIN Language l ON l.Id = r.LanguageId
WHERE r.ResourceName = 'homepage.products'
  AND r.ResourceValue IN (N'Featured products', N'منتجات مميزة');");
    }
}
