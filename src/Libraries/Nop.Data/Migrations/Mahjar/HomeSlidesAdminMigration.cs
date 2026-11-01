// [Mahjar home-slides]
using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// The bilingual (en + ar) admin copy of Content management → Home slides: the menu entry, the list page (intro,
/// placement filter, grid columns, the live / scheduled / ended / hidden badges), the create/edit form (card titles,
/// every field label with its hint, the link-target helpers and the live preview) and the validation and
/// notification messages. The enum labels (layout, text position, placement, link kind) were added with the table
/// by <see cref="HomeSlidesMigration"/>.
///
/// Idempotent (a resource is inserted only when that language lacks it, so an owner's later wording edits in
/// Admin → Languages survive a re-run), SQL Server only like the sibling Mahjar migrations, and a schema migration
/// so it auto-applies on startup. It also renames the "url" link kind that <see cref="HomeSlidesMigration"/> called
/// "External link": it links store pages too — but only while the label is still that original wording. Picture sizes are spelled out in Arabic ("بعرض 2400 وارتفاع 1050") rather than
/// "2400×1050", which the bidi algorithm would print reversed in a right-to-left line. NOTE: locale resources are
/// cached — restart (or POST /Admin/Common/ClearCache) after it runs. The "Open in rent mode" strings
/// (admin.mahjar.homeslides.fields.targetrentmode*) were added after this migration had already run on the dev
/// database, so they were also inserted there by hand with the same idempotent statement. [Mahjar home-slides]
/// </summary>
[NopSchemaMigration("2026-09-29 10:10:00", "Mahjar home-slides: admin resources")]
public class HomeSlidesAdminMigration : ForwardOnlyMigration
{
    /// <summary>The Arabic-language predicate every Mahjar resource migration uses.</summary>
    private const string IS_ARABIC = "(l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')";

    /// <summary>The only dialect the seeded T-SQL targets; the SQLite test DB simply skips it.</summary>
    private const string DATABASE_TYPE = "sqlserver";

    public override void Up()
    {
        AddListResources();
        AddFormResources();
        AddFieldResources();
        RenameUrlTargetType();
    }

    #region Utilities

    /// <summary>
    /// Inserts resources for every language (Arabic value for an Arabic language, English otherwise), skipping any
    /// a language already has
    /// </summary>
    /// <param name="values">The T-SQL VALUES rows: ('name', N'ar', N'en')</param>
    protected virtual void InsertResources(string values)
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql($@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN {IS_ARABIC} THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
{values}
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }

    /// <summary>
    /// 1) The menu entry, the list page (intro, filter, grid) and the notifications
    /// </summary>
    protected virtual void AddListResources()
    {
        InsertResources(@"
    ('admin.mahjar.homeslides.menu',                      N'شرائح الصفحة الرئيسية',  N'Home slides'),
    ('admin.mahjar.homeslides.list.title',                N'شرائح الصفحة الرئيسية',  N'Home slides'),
    ('admin.mahjar.homeslides.list.intro.title',          N'أول ما يراه عملاؤك',    N'The first thing your customers see'),
    ('admin.mahjar.homeslides.list.intro',                N'الشرائح هي الصور الكبيرة التي تستقبل العملاء في الصفحة الرئيسية للموقع والتطبيق. أضف منها ما تشاء، وستتوالى تلقائيًا بالترتيب الذي تحدده.',
                                                          N'Slides are the large images that greet customers on the home page of the website and the app. Add as many as you like: they rotate automatically, in the order you set.'),
    ('admin.mahjar.homeslides.list.intro.hero',           N'العرض المتحرك أعلى الصفحة الرئيسية للموقع، وشريط الصور في الشاشة الرئيسية لتطبيق الجوال.',
                                                          N'The rotating showcase at the top of the website home page, and the picture carousel on the app home screen.'),
    ('admin.mahjar.homeslides.list.intro.feature',        N'شريط صورة عريض في منتصف الصفحة الرئيسية للموقع، مثالي لإبراز مجموعة أو حملة. يظهر في الموقع فقط.',
                                                          N'A wide picture band in the middle of the website home page, ideal for a collection or a campaign. Website only.'),
    ('admin.mahjar.homeslides.list.intro.sizes.title',    N'المقاسات المقترحة',      N'Recommended sizes'),
    ('admin.mahjar.homeslides.list.intro.sizes',          N'«ملء الشاشة»: صورة رئيسية بعرض 2400 وارتفاع 1050 بكسل، وصورة جوال بعرض 1200 وارتفاع 800 بكسل تكون الساعة في ثلثها الأوسط. «عرض القطعة»: صورة مربعة لا يقل ضلعها عن 1200 بكسل، والساعة في منتصفها على خلفية فاتحة سادة.',
                                                          N'Full bleed: main picture 2400 × 1050 px, mobile picture 1200 × 800 px with the watch in its middle third. Spotlight: a square picture at least 1200 px on each side, the watch centred on a plain light background.'),
    ('admin.mahjar.homeslides.list.intro.order.title',    N'الترتيب والجدولة',       N'Order and schedule'),
    ('admin.mahjar.homeslides.list.intro.order',          N'تظهر الشرائح حسب ترتيب العرض من الأصغر إلى الأكبر، وتُخفى تلقائيًا الشرائح غير المنشورة أو التي تقع خارج فترة عرضها.',
                                                          N'Slides appear by display order, lowest first. Unpublished slides, and slides outside their show dates, are hidden automatically.'),
    ('admin.mahjar.homeslides.list.viewstore',            N'عرض المتجر',             N'View store'),
    ('admin.mahjar.homeslides.list.searchplacement',      N'موضع الظهور',            N'Placement'),
    ('admin.mahjar.homeslides.list.searchplacement.hint', N'اعرض شرائح موضع واحد فقط.', N'Show only the slides of one placement.'),
    ('admin.mahjar.homeslides.list.allplacements',        N'كل المواضع',             N'All placements'),
    ('admin.mahjar.homeslides.grid.picture',              N'الصورة',                 N'Picture'),
    ('admin.mahjar.homeslides.grid.web',                  N'الموقع',                 N'Website'),
    ('admin.mahjar.homeslides.grid.app',                  N'التطبيق',                N'App'),
    ('admin.mahjar.homeslides.status.live',               N'معروضة الآن',            N'Live'),
    ('admin.mahjar.homeslides.status.scheduled',          N'مجدولة',                 N'Scheduled'),
    ('admin.mahjar.homeslides.status.ended',              N'انتهى عرضها',            N'Ended'),
    ('admin.mahjar.homeslides.status.hidden',             N'مخفية',                  N'Hidden'),
    ('admin.mahjar.homeslides.addnew',                    N'إضافة شريحة جديدة',      N'Add a new slide'),
    ('admin.mahjar.homeslides.editdetails',               N'تعديل الشريحة',          N'Edit slide'),
    ('admin.mahjar.homeslides.backtolist',                N'العودة إلى قائمة الشرائح', N'back to the slides list'),
    ('admin.mahjar.homeslides.added',                     N'تمت إضافة الشريحة.',     N'The slide has been added.'),
    ('admin.mahjar.homeslides.updated',                   N'تم تحديث الشريحة.',      N'The slide has been updated.'),
    ('admin.mahjar.homeslides.deleted',                   N'تم حذف الشريحة.',        N'The slide has been deleted.')");
    }

    /// <summary>
    /// 2) The form chrome: card titles, the content help and the live preview
    /// </summary>
    protected virtual void AddFormResources()
    {
        InsertResources(@"
    ('admin.mahjar.homeslides.card.content',              N'المحتوى',                N'Content'),
    ('admin.mahjar.homeslides.card.media',                N'الصور والتصميم',         N'Pictures and layout'),
    ('admin.mahjar.homeslides.card.link',                 N'الرابط',                 N'Link'),
    ('admin.mahjar.homeslides.card.display',              N'مكان الظهور وموعده',     N'Where and when'),
    ('admin.mahjar.homeslides.card.preview',              N'معاينة مباشرة',          N'Live preview'),
    ('admin.mahjar.homeslides.content.hint',              N'اكتب نصوص كل لغة في تبويبها الخاص، ويُستخدم تبويب «اساسي» لأي لغة تتركها فارغة.',
                                                          N'Write the texts of each language in its own tab. The Standard tab is used for any language you leave empty.'),
    ('admin.mahjar.homeslides.preview.nopicture',         N'ارفع الصورة الرئيسية لمعاينة الشريحة', N'Upload the main picture to preview the slide'),
    ('admin.mahjar.homeslides.preview.untitled',          N'عنوان الشريحة',          N'Slide title'),
    ('admin.mahjar.homeslides.preview.hint',              N'معاينة تقريبية؛ يضيف المتجر الحركة ويُكيّف الشريحة مع كل شاشة.',
                                                          N'An approximate preview: the store adds motion and fits the slide to each screen.'),
    ('admin.mahjar.homeslides.preview.language',          N'لغة المعاينة، وتتغير بالنقر على تبويب لغة آخر', N'The language previewed: click another language tab to switch')");
    }

    /// <summary>
    /// 3) Every field: label, hint (the "?" tooltip) and validation messages
    /// </summary>
    protected virtual void AddFieldResources()
    {
        //content
        InsertResources(@"
    ('admin.mahjar.homeslides.fields.kicker',             N'العنوان التمهيدي',       N'Small heading'),
    ('admin.mahjar.homeslides.fields.kicker.hint',        N'سطر قصير يظهر فوق العنوان، مثل: «مجموعة محجر». اختياري.',
                                                          N'A short line above the title, for example “The MHGR Collection”. Optional.'),
    ('admin.mahjar.homeslides.fields.title',              N'العنوان',                N'Title'),
    ('admin.mahjar.homeslides.fields.title.hint',         N'السطر الرئيسي في الشريحة. اجعله قصيرًا، فالكلمات القليلة أوقع أثرًا.',
                                                          N'The main line of the slide. Keep it short: a few words read best.'),
    ('admin.mahjar.homeslides.fields.title.required',     N'يرجى إدخال عنوان الشريحة.', N'Please enter the slide title.'),
    ('admin.mahjar.homeslides.fields.subtitle',           N'النص الوصفي',            N'Subtitle'),
    ('admin.mahjar.homeslides.fields.subtitle.hint',      N'جملة أو جملتان تحت العنوان. اختياري.', N'One or two sentences under the title. Optional.'),
    ('admin.mahjar.homeslides.fields.buttontext',         N'نص الزر',                N'Button text'),
    ('admin.mahjar.homeslides.fields.buttontext.hint',    N'مثل: «اكتشف الساعة». يحتاج الزر إلى رابط تختاره في بطاقة «الرابط». اتركه فارغًا لإخفاء الزر، وتبقى الشريحة قابلة للنقر إن كان لها رابط.',
                                                          N'For example “Discover the watch”. A button needs a link, chosen in the Link card. Leave it empty for no button: the slide stays clickable when it has a link.'),
    ('admin.mahjar.homeslides.fields.buttontext.needslink', N'الزر يحتاج إلى رابط: اختر ما تفتحه الشريحة في بطاقة «الرابط»، أو امسح نص الزر.',
                                                          N'A button needs a link: choose what the slide opens in the Link card, or clear the button text.')");

        //pictures and layout
        InsertResources(@"
    ('admin.mahjar.homeslides.fields.picture',            N'الصورة الرئيسية',        N'Main picture'),
    ('admin.mahjar.homeslides.fields.picture.hint',       N'تظهر في الموقع، وفي التطبيق أيضًا إن لم تُرفع صورة للجوال.',
                                                          N'Shown on the website, and in the app too when no mobile picture is uploaded.'),
    ('admin.mahjar.homeslides.fields.picture.required',   N'يرجى رفع الصورة الرئيسية.', N'Please upload the main picture.'),
    ('admin.mahjar.homeslides.fields.picture.invalid',    N'تعذّر استخدام هذه الصورة، يرجى رفعها من جديد.', N'This picture cannot be used. Please upload it again.'),
    ('admin.mahjar.homeslides.fields.picture.size',       N'المقترح: بعرض 2400 وارتفاع 1050 بكسل بصيغة JPG، ويُفضّل ألا يتجاوز حجمها 1 ميجابايت. أبقِ العنصر الأساسي بعيدًا عن الحواف، فالشاشات الضيقة تقتطع الجوانب.',
                                                          N'Recommended: 2400 × 1050 px JPG, ideally under 1 MB. Keep the subject away from the edges: narrow screens crop the sides.'),
    ('admin.mahjar.homeslides.fields.picture.size.spotlight', N'المقترح لتصميم «عرض القطعة»: صورة مربعة لا يقل ضلعها عن 1200 بكسل، والساعة في منتصفها على خلفية بيضاء أو فاتحة سادة وتشغل نحو 60% من الإطار. تظهر الصورة داخل منصة دائرية وتتلاشى أطرافها.',
                                                          N'Recommended for Spotlight: a square picture at least 1200 px on each side, the watch centred on a plain white or light background and filling about 60% of the frame. It shows inside a round display plate, its edges fading out.'),
    ('admin.mahjar.homeslides.fields.mobilepicture',      N'صورة الجوال',            N'Mobile picture'),
    ('admin.mahjar.homeslides.fields.mobilepicture.hint', N'اختيارية. تُستخدم في التطبيق وعلى شاشات الجوال، وعند تركها فارغة تُستخدم الصورة الرئيسية.',
                                                          N'Optional. Used in the app and on phone screens; when empty, the main picture is used.'),
    ('admin.mahjar.homeslides.fields.mobilepicture.size', N'المقترح: بعرض 1200 وارتفاع 800 بكسل بصيغة JPG. في متصفح الجوال تظهر الصورة في إطار طولي ويُقتطع جانباها، فاجعل الساعة في الثلث الأوسط. اختيارية، وتُستخدم الصورة الرئيسية عند تركها فارغة.',
                                                          N'Recommended: 1200 × 800 px JPG. Phone browsers show it in a tall frame and crop both sides, so keep the watch in the middle third. Optional: the main picture is used when empty.'),
    ('admin.mahjar.homeslides.fields.mobilepicture.size.spotlight', N'المقترح: صورة مربعة كذلك لا يقل ضلعها عن 800 بكسل، والساعة في منتصفها، فالتطبيق يقتطعها دائرةً من الوسط. اختيارية، وتُستخدم الصورة الرئيسية عند تركها فارغة.',
                                                          N'Recommended: square as well, at least 800 px on each side, with the watch centred: the app crops it to a circle from the middle. Optional: the main picture is used when empty.'),
    ('admin.mahjar.homeslides.fields.videourl',           N'رابط فيديو الخلفية',     N'Background video (MP4 link)'),
    ('admin.mahjar.homeslides.fields.videourl.hint',      N'اختياري، ويعمل فقط مع تصميم «ملء الشاشة» في الواجهة الرئيسية للموقع. يُعرض بلا صوت وبشكل متكرر، وتكون الصورة الرئيسية غلافًا له. يجب أن يبدأ الرابط بـ https:// أو /.',
                                                          N'Optional, and only for the Full bleed layout in the website hero. Plays muted on a loop, with the main picture as its cover. The link must start with https:// or /.'),
    ('admin.mahjar.homeslides.fields.videourl.notapplicable', N'لن يظهر هذا الفيديو: يعمل الفيديو مع تصميم «ملء الشاشة» في الواجهة الرئيسية فقط.',
                                                          N'This video will not play: video works only with the Full bleed layout in the hero carousel.'),
    ('admin.mahjar.homeslides.fields.videourl.invalid',   N'أدخل رابط فيديو يبدأ بـ http:// أو https:// أو /.', N'Enter a video link that starts with http://, https:// or /.'),
    ('admin.mahjar.homeslides.fields.layout',             N'التصميم',                N'Layout'),
    ('admin.mahjar.homeslides.fields.layout.hint',        N'ملء الشاشة: تغطي الصورة الشريحة ويظهر النص فوقها، وهو الأنسب للصور الواقعية العريضة. عرض القطعة: تظهر الصورة داخل منصة عرض دائرية فوق خلفية العلامة، وهو الأنسب لصورة مربعة لساعة على خلفية فاتحة سادة.',
                                                          N'Full bleed: the photo fills the slide and the text sits over it, best for wide lifestyle photos. Spotlight: the picture sits inside a round display plate over the brand backdrop, best for a square shot of a watch on a plain light background.'),
    ('admin.mahjar.homeslides.fields.contentposition',    N'موضع النص',              N'Text position'),
    ('admin.mahjar.homeslides.fields.contentposition.hint', N'مكان ظهور النص في الشريحة. «البداية» هي الجهة اليمنى في العربية واليسرى في الإنجليزية.',
                                                          N'Where the text sits on the slide. Start is the right side in Arabic and the left side in English.'),
    ('admin.mahjar.homeslides.fields.overlayopacity',     N'درجة التعتيم',           N'Overlay darkness'),
    ('admin.mahjar.homeslides.fields.overlayopacity.hint', N'لتصميم «ملء الشاشة» فقط: تعتيم الصورة خلف النص ليبقى واضحًا، 0 بلا تعتيم و90 تعتيم شديد، وتناسب القيمة 45 تقريبًا معظم الصور.',
                                                          N'Full bleed only: darkens the photo behind the text so it stays readable. 0 is none and 90 is very dark; around 45 suits most photos.'),
    ('admin.mahjar.homeslides.fields.overlayopacity.spotlightnote', N'لا يحتاج تصميم «عرض القطعة» إلى تعتيم، فالنص يظهر على خلفية العلامة الداكنة.',
                                                          N'Spotlight needs no darkening: the text sits on the dark brand backdrop.'),
    ('admin.mahjar.homeslides.fields.overlayopacity.range', N'يجب أن تكون درجة التعتيم بين 0 و90.', N'The overlay darkness must be between 0 and 90.')");

        //link
        InsertResources(@"
    ('admin.mahjar.homeslides.fields.targettype',         N'الرابط إلى',             N'Link to'),
    ('admin.mahjar.homeslides.fields.targettype.hint',    N'ما يُفتح عندما ينقر العميل على الشريحة أو زرها.', N'What opens when a customer taps the slide or its button.'),
    ('admin.mahjar.homeslides.fields.targetproduct',      N'المنتج',                 N'Product'),
    ('admin.mahjar.homeslides.fields.targetproduct.hint', N'ابحث باسم المنتج بالعربية أو الإنجليزية (ثلاثة أحرف على الأقل)، أو اكتب رقمه.', N'Search by the product name in Arabic or English (at least 3 letters), or type its ID.'),
    ('admin.mahjar.homeslides.fields.targetproduct.search', N'ابحث عن منتج بالاسم…', N'Search products by name…'),
    ('admin.mahjar.homeslides.fields.targetproduct.id',   N'رقم المنتج',             N'Product ID'),
    ('admin.mahjar.homeslides.fields.targetproduct.open', N'فتح المنتج',             N'Open product'),
    ('admin.mahjar.homeslides.fields.targetproduct.unpublished', N'(غير منشور)',     N'(not published)'),
    ('admin.mahjar.homeslides.fields.targetproduct.required', N'يرجى اختيار المنتج الذي تفتحه الشريحة.', N'Please choose the product the slide opens.'),
    ('admin.mahjar.homeslides.fields.targetrentmode',     N'فتح على وضع الاستئجار',  N'Open in rent mode'),
    ('admin.mahjar.homeslides.fields.targetrentmode.hint', N'تُفتح صفحة المنتج وقد اختير فيها «تأجير»، فينتقل العميل مباشرة إلى اختيار تواريخ الاستئجار. يناسب زرًا مثل «استأجر الآن».',
                                                          N'The product page opens with Rent already chosen, so the customer goes straight to the rental dates. Suits a button like “Rent now”.'),
    ('admin.mahjar.homeslides.fields.targetrentmode.note', N'يعمل مع المنتجات القابلة للاستئجار فقط، وتُفتح بقية المنتجات بالوضع المعتاد.',
                                                          N'Only for products that can be rented; any other product opens as usual.'),
    ('admin.mahjar.homeslides.fields.targetrentmode.notrental', N'هذا المنتج غير متاح للاستئجار، لذا ستفتحه الشريحة بالوضع المعتاد.',
                                                          N'This product cannot be rented, so the slide will open it as usual.'),
    ('admin.mahjar.homeslides.fields.targetrentmode.tag', N'(وضع الاستئجار)',        N'(rent mode)'),
    ('admin.mahjar.homeslides.fields.targetcategory',     N'الفئة',                  N'Category'),
    ('admin.mahjar.homeslides.fields.targetcategory.hint', N'تظهر هنا الفئات المنشورة فقط.', N'Only published categories are listed.'),
    ('admin.mahjar.homeslides.fields.targetcategory.required', N'يرجى اختيار الفئة التي تفتحها الشريحة.', N'Please choose the category the slide opens.'),
    ('admin.mahjar.homeslides.fields.targetmanufacturer', N'العلامة التجارية',       N'Brand'),
    ('admin.mahjar.homeslides.fields.targetmanufacturer.hint', N'تظهر هنا العلامات التجارية المنشورة فقط.', N'Only published brands are listed.'),
    ('admin.mahjar.homeslides.fields.targetmanufacturer.required', N'يرجى اختيار العلامة التجارية التي تفتحها الشريحة.', N'Please choose the brand the slide opens.'),
    ('admin.mahjar.homeslides.fields.targetvendor',       N'الشريك',                 N'Partner'),
    ('admin.mahjar.homeslides.fields.targetvendor.hint',  N'يظهر هنا الشركاء النشطون فقط.', N'Only active partners are listed.'),
    ('admin.mahjar.homeslides.fields.targetvendor.required', N'يرجى اختيار الشريك الذي تفتح الشريحة صفحته.', N'Please choose the partner whose page the slide opens.'),
    ('admin.mahjar.homeslides.fields.targetsearch',       N'نص البحث',               N'Search text'),
    ('admin.mahjar.homeslides.fields.targetsearch.hint',  N'تفتح الشريحة نتائج البحث عن هذا النص، مثل: «غوص».', N'The slide opens the search results for this text, for example “diver”.'),
    ('admin.mahjar.homeslides.fields.targetsearch.required', N'يرجى إدخال نص البحث.', N'Please enter the search text.'),
    ('admin.mahjar.homeslides.fields.targeturl',          N'عنوان الرابط',           N'Link address'),
    ('admin.mahjar.homeslides.fields.targeturl.hint',     N'رابط كامل يبدأ بـ https:// أو مسار صفحة في المتجر يبدأ بـ / (مثل /luxury-watches).',
                                                          N'A full link starting with https://, or a store page path starting with / (for example /luxury-watches).'),
    ('admin.mahjar.homeslides.fields.targeturl.invalid',  N'أدخل رابطًا يبدأ بـ http:// أو https:// أو /.', N'Enter a link that starts with http://, https:// or /.'),
    ('admin.mahjar.homeslides.fields.target.choose',      N'— اختر —',               N'— Choose —'),
    ('admin.mahjar.homeslides.fields.target.hidden',      N'(لا يظهر للعملاء)',      N'(not shown to customers)'),
    ('admin.mahjar.homeslides.fields.target.missing',     N'(لم يعد موجودًا)',       N'(no longer exists)'),
    ('admin.mahjar.homeslides.fields.target.notfound',    N'غير موجود، ربما تم حذفه.', N'Not found: it may have been deleted.')");

        //where and when
        InsertResources(@"
    ('admin.mahjar.homeslides.fields.placement',          N'موضع الظهور',            N'Placement'),
    ('admin.mahjar.homeslides.fields.placement.hint',     N'الواجهة الرئيسية: العرض أعلى الصفحة الرئيسية في الموقع والتطبيق. شريط مميز: شريط عريض في منتصف الصفحة الرئيسية للموقع.',
                                                          N'Hero carousel: the showcase at the top of the home page, on the website and in the app. Feature band: a wide band in the middle of the website home page.'),
    ('admin.mahjar.homeslides.fields.placement.featurenote', N'تظهر الأشرطة المميزة في الموقع فقط، أما التطبيق فيعرض شرائح الواجهة الرئيسية.',
                                                          N'Feature bands appear on the website only; the app shows the hero slides.'),
    ('admin.mahjar.homeslides.fields.showonweb',          N'العرض في الموقع',        N'Show on the website'),
    ('admin.mahjar.homeslides.fields.showonweb.hint',     N'عرض هذه الشريحة في الصفحة الرئيسية للموقع.', N'Show this slide on the website home page.'),
    ('admin.mahjar.homeslides.fields.showonapp',          N'العرض في التطبيق',       N'Show in the app'),
    ('admin.mahjar.homeslides.fields.showonapp.hint',     N'عرض هذه الشريحة في شريط صور تطبيق الجوال (لشرائح الواجهة الرئيسية فقط).',
                                                          N'Show this slide in the mobile app carousel (hero slides only).'),
    ('admin.mahjar.homeslides.fields.showonapp.featureonly', N'لا تظهر الأشرطة المميزة في التطبيق.', N'Feature bands do not appear in the app.'),
    ('admin.mahjar.homeslides.fields.published',          N'منشورة',                 N'Published'),
    ('admin.mahjar.homeslides.fields.published.hint',     N'ألغِ التحديد لإخفاء الشريحة دون حذفها.', N'Untick to hide the slide without deleting it.'),
    ('admin.mahjar.homeslides.fields.displayorder',       N'ترتيب العرض',            N'Display order'),
    ('admin.mahjar.homeslides.fields.displayorder.hint',  N'تُعرض الشرائح من الرقم الأصغر إلى الأكبر.', N'Slides are shown lowest number first.'),
    ('admin.mahjar.homeslides.fields.startdate',          N'العرض من',               N'Show from'),
    ('admin.mahjar.homeslides.fields.startdate.hint',     N'اختياري. تبدأ الشريحة بالظهور من هذا التاريخ والوقت (بتوقيتك).',
                                                          N'Optional. The slide appears from this date and time (your time zone).'),
    ('admin.mahjar.homeslides.fields.enddate',            N'العرض حتى',              N'Show until'),
    ('admin.mahjar.homeslides.fields.enddate.hint',       N'اختياري. تختفي الشريحة بعد هذا التاريخ والوقت، وهو مفيد للحملات والعروض.',
                                                          N'Optional. The slide disappears after this date and time: handy for campaigns and offers.'),
    ('admin.mahjar.homeslides.fields.enddate.beforestart', N'يجب أن يكون تاريخ «العرض حتى» بعد تاريخ «العرض من».', N'“Show until” must be after “Show from”.')");
    }

    /// <summary>
    /// 4) The "url" link kind links store pages as well as other sites, so "External link" misled; renamed only while
    /// a language still has the wording <see cref="HomeSlidesMigration"/> gave it (an owner's own wording stays)
    /// </summary>
    protected virtual void RenameUrlTargetType()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql($@"
UPDATE r
SET r.ResourceValue = CASE WHEN {IS_ARABIC} THEN N'رابط مخصص (صفحة في المتجر أو موقع آخر)' ELSE N'Custom link (store page or website)' END
FROM LocaleStringResource r
INNER JOIN Language l ON l.Id = r.LanguageId
WHERE r.ResourceName = 'enums.nop.core.domain.mahjar.homeslidetargettype.url'
  AND ((({IS_ARABIC}) AND r.ResourceValue = N'رابط خارجي')
    OR (NOT ({IS_ARABIC}) AND r.ResourceValue = N'External link'));");
    }

    #endregion
}
