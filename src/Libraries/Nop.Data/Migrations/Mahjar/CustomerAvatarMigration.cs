// [Mahjar app-batch3]
using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Customer profile photos for the app and the website. Switches the stock nopCommerce avatar on for every store
/// (customersettings.allowcustomerstouploadavatars = True: the app's api/customer/avatar and the website's My account →
/// Avatar page both follow it) and raises customersettings.avatarmaximumsizebytes from the stock 20 KB to 5 MB, only
/// while it still has the stock value, so a photo straight from a phone can be uploaded on the website too (both paths
/// store a re-encoded 640 px square, see Nop.Web's CustomerAvatarHelper). Seeds the app API's bilingual messages
/// (mahjar.api.account.avatar*) and brings the website's upload rules in line, only while they still describe the stock
/// 20 KB GIF-or-JPEG rule (an admin's own wording is never overwritten). Idempotent; SQL Server dialect like the sibling
/// Mahjar migrations. NOTE: settings and locale resources are cached — restart (or POST /Admin/Common/ClearCache) after
/// it runs. [Mahjar app-batch3]
/// </summary>
[NopSchemaMigration("2026-10-01 10:00:00", "Mahjar app-batch3: customer avatars (app + web)")]
public class CustomerAvatarMigration : ForwardOnlyMigration
{
    /// <summary>The Arabic-language predicate every Mahjar resource migration uses.</summary>
    private const string IS_ARABIC = "(l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')";

    /// <summary>The only dialect the seeded T-SQL targets; the SQLite test DB simply skips it.</summary>
    private const string DATABASE_TYPE = "sqlserver";

    public override void Up()
    {
        //profile photos on (every row of the setting, and the all-stores row when it is missing)
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
UPDATE Setting SET [Value] = 'True'
WHERE [Name] = 'customersettings.allowcustomerstouploadavatars' AND [Value] <> 'True';

INSERT INTO Setting ([Name], [Value], [StoreId])
SELECT 'customersettings.allowcustomerstouploadavatars', 'True', 0
WHERE NOT EXISTS (
    SELECT 1 FROM Setting
    WHERE [Name] = 'customersettings.allowcustomerstouploadavatars' AND [StoreId] = 0
);");

        //the upload cap: the stock 20 KB becomes 5 MB (a missing row would mean 0 bytes, i.e. every upload refused)
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
UPDATE Setting SET [Value] = '5242880'
WHERE [Name] = 'customersettings.avatarmaximumsizebytes' AND [Value] = '20000';

INSERT INTO Setting ([Name], [Value], [StoreId])
SELECT 'customersettings.avatarmaximumsizebytes', '5242880', 0
WHERE NOT EXISTS (
    SELECT 1 FROM Setting
    WHERE [Name] = 'customersettings.avatarmaximumsizebytes' AND [StoreId] = 0
);");

        //the app API's messages ({0} = the size cap in MB)
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN " + IS_ARABIC + @" THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    ('mahjar.api.account.avatardisabled', N'صور الملف الشخصي غير متاحة حاليًا.',
                                          N'Profile photos are not available at the moment.'),
    ('mahjar.api.account.avatarnofile',   N'اختر صورة لرفعها.',
                                          N'Choose a photo to upload.'),
    ('mahjar.api.account.avatarinvalid',  N'لا يمكن استخدام هذا الملف صورةً شخصية. اختر صورة بصيغة JPEG أو PNG أو WebP.',
                                          N'This file can''t be used as a profile photo. Choose a JPEG, PNG or WebP image.'),
    ('mahjar.api.account.avatartoolarge', N'هذه الصورة كبيرة جدًا. اختر صورة لا يتجاوز حجمها {0} ميغابايت.',
                                          N'This photo is too large. Choose one of up to {0} MB.'),
    ('mahjar.api.account.avatarfailed',   N'تعذّر حفظ صورتك. يُرجى المحاولة مرة أخرى.',
                                          N'We couldn''t save your photo. Please try again.')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");

        //the website's upload rules (My account → Avatar), while they still describe the stock 20 KB GIF/JPEG rule
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
UPDATE r
SET r.ResourceValue = CASE WHEN " + IS_ARABIC + @"
                           THEN N'استخدم صورة بصيغة JPEG أو PNG أو WebP أو GIF لا يتجاوز حجمها 5 ميغابايت.'
                           ELSE N'Use a JPEG, PNG, WebP or GIF photo of up to 5 MB.' END
FROM LocaleStringResource r
INNER JOIN Language l ON l.Id = r.LanguageId
WHERE r.ResourceName = 'account.avatar.uploadrules'
  AND (r.ResourceValue LIKE N'%20 KB%' OR r.ResourceValue LIKE N'%20 كيلوبايت%');");
    }
}
