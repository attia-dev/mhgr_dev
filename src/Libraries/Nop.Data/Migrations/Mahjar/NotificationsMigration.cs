// [Mahjar notifications]
using FluentMigrator;
using Nop.Core.Domain.Mahjar;
using Nop.Data.Extensions;
using Nop.Data.Mapping;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Creates the in-app notification feed: the <see cref="Notification"/> + <see cref="NotificationPreference"/>
/// tables (with a composite index on the hot feed/unread query), and the bilingual (en + ar)
/// <c>mahjar.notifications.*</c> locale resources used to compose titles/bodies and the UI.
///
/// Idempotent (tables created only when missing; resources inserted only when missing), so it is deploy-safe,
/// and a schema migration so it auto-applies on startup. SQL Server dialect. [Mahjar notifications]
/// </summary>
[NopSchemaMigration("2026-06-26 09:00:00", "Mahjar notifications: in-app feed + per-type preferences")]
public class NotificationsMigration : ForwardOnlyMigration
{
    public override void Up()
    {
        //1) schema — create the two tables via the LinqToDB builders. FluentMigrator's version table guards
        //   against re-runs, so no Schema.Table(...).Exists() guard is needed (and that guard misbehaves on the
        //   SQLite test provider, silently skipping creation — matching the base SchemaMigration which also
        //   creates tables unconditionally).
        Create.TableFor<Notification>();
        Create.TableFor<NotificationPreference>();

        //2) composite index for the unread-count / feed-list hot path (FK conventions alone don't cover it).
        //   SQL Server only — keeps the descending key; the SQLite test DB doesn't need it.
        const string databaseType = "sqlserver";
        var notificationTable = NameCompatibilityManager.GetTableName(typeof(Notification));
        if (!Schema.Table(notificationTable).Index("IX_Notification_Recipient_IsRead_Created").Exists())
            IfDatabase(databaseType).Create.Index("IX_Notification_Recipient_IsRead_Created").OnTable(notificationTable)
                .OnColumn(nameof(Notification.RecipientCustomerId)).Ascending()
                .OnColumn(nameof(Notification.IsRead)).Ascending()
                .OnColumn(nameof(Notification.CreatedOnUtc)).Descending();

        //3) per-type title/body templates ({0}=order id / product name, {1}=secondary) — en + ar.
        //   T-SQL dialect (N'…' literals) → SQL Server only; harmless to skip on the SQLite test DB.
        IfDatabase(databaseType).Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN (l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%') THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    ('mahjar.notifications.type.orderplacedbuyer.title',      N'تم إنشاء الطلب',             N'Order placed'),
    ('mahjar.notifications.type.orderplacedbuyer.body',       N'تم إنشاء طلبك رقم {0}.',      N'Your order #{0} has been placed.'),
    ('mahjar.notifications.type.orderpaidbuyer.title',        N'تم استلام الدفعة',           N'Payment received'),
    ('mahjar.notifications.type.orderpaidbuyer.body',         N'تم استلام دفعة طلبك رقم {0}.', N'Payment for order #{0} was received.'),
    ('mahjar.notifications.type.orderstatuschanged.title',    N'تم تحديث الطلب',             N'Order updated'),
    ('mahjar.notifications.type.orderstatuschanged.body',     N'أصبحت حالة طلبك رقم {0}: {1}.', N'Order #{0} is now {1}.'),
    ('mahjar.notifications.type.orderrefunded.title',         N'تم استرداد المبلغ',          N'Order refunded'),
    ('mahjar.notifications.type.orderrefunded.body',          N'تم استرداد مبلغ {1} لطلبك رقم {0}.', N'Order #{0} was refunded {1}.'),
    ('mahjar.notifications.type.ordershipped.title',          N'تم شحن الطلب',               N'Order shipped'),
    ('mahjar.notifications.type.ordershipped.body',           N'تم شحن طلبك رقم {0}.',        N'Your order #{0} has shipped.'),
    ('mahjar.notifications.type.orderdelivered.title',        N'تم توصيل الطلب',             N'Order delivered'),
    ('mahjar.notifications.type.orderdelivered.body',         N'تم توصيل طلبك رقم {0}.',      N'Your order #{0} was delivered.'),
    ('mahjar.notifications.type.orderreadyforpickup.title',   N'جاهز للاستلام',              N'Ready for pickup'),
    ('mahjar.notifications.type.orderreadyforpickup.body',    N'طلبك رقم {0} جاهز للاستلام.', N'Your order #{0} is ready for pickup.'),
    ('mahjar.notifications.type.welcome.title',               N'مرحباً بك',                  N'Welcome'),
    ('mahjar.notifications.type.welcome.body',                N'مرحباً بك في محجر! حسابك جاهز.', N'Welcome to MHGR! Your account is ready.'),
    ('mahjar.notifications.type.orderplacedvendor.title',     N'طلب جديد',                   N'New order'),
    ('mahjar.notifications.type.orderplacedvendor.body',      N'لديك طلب جديد رقم {0}.',      N'You have a new order #{0}.'),
    ('mahjar.notifications.type.orderpaidvendor.title',       N'تم دفع الطلب',               N'Order paid'),
    ('mahjar.notifications.type.orderpaidvendor.body',        N'تم دفع الطلب رقم {0}.',       N'Order #{0} has been paid.'),
    ('mahjar.notifications.type.reviewapprovedvendor.title',  N'تمت الموافقة على مراجعة',     N'Review approved'),
    ('mahjar.notifications.type.reviewapprovedvendor.body',   N'تمت الموافقة على مراجعة للمنتج {0}.', N'A review for {0} was approved.'),
    ('mahjar.notifications.type.orderplacedadmin.title',      N'طلب جديد',                   N'New order'),
    ('mahjar.notifications.type.orderplacedadmin.body',       N'تم إنشاء طلب جديد رقم {0}.',   N'New order #{0} was placed.'),
    ('mahjar.notifications.title',                            N'الإشعارات',                  N'Notifications'),
    ('mahjar.notifications.bell',                             N'الإشعارات',                  N'Notifications'),
    ('mahjar.notifications.empty',                            N'لا توجد إشعارات',            N'No notifications'),
    ('mahjar.notifications.markallread',                      N'تحديد الكل كمقروء',          N'Mark all as read'),
    ('mahjar.notifications.viewall',                          N'عرض الكل',                   N'View all'),
    ('mahjar.notifications.settings.title',                  N'إعدادات الإشعارات',          N'Notification settings'),
    ('mahjar.notifications.settings.inapp',                  N'داخل التطبيق',               N'In-app'),
    ('mahjar.notifications.settings.email',                  N'البريد الإلكتروني',          N'Email'),
    ('mahjar.notifications.settings.saved',                  N'تم حفظ إعدادات الإشعارات',    N'Notification settings saved.')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }
}
