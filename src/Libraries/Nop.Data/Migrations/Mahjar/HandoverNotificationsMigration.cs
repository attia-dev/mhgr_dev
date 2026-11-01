// [Mahjar handover]
using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Bilingual (en + ar) resources of the handover desk's second round of notifications and of the rental-occupancy rule:
/// 1) the handover staff's task notifications — a contract needs before-signing condition media, a paid order waits to be
///    handed over, a rental is now out with a customer;
/// 2) the confirmation notifications — to the buyer and the admins on a delivery, to the admins and the partner on an
///    intake (the return ones already exist);
/// 3) the handover desk's message for an item handed over while others of the order are still waiting;
/// 4) the cart warnings that stop a rentable item from being bought while it is out with a renter or booked, and one
///    cart from both buying and renting the same one-of-a-kind item.
/// Idempotent (insert-if-missing); SQL Server dialect, matching the sibling Mahjar migrations.
/// NOTE: locale resources are cached — restart (or POST /Admin/Common/ClearCache) after it runs. [Mahjar handover]
/// </summary>
[NopSchemaMigration("2026-09-16 10:20:00", "Mahjar handover: staff task + confirmation notifications, rented-out purchase rule")]
public class HandoverNotificationsMigration : ForwardOnlyMigration
{
    /// <summary>The Arabic-language predicate every Mahjar resource migration uses.</summary>
    private const string IS_ARABIC = "(l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')";

    public override void Up()
    {
        IfDatabase("sqlserver").Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN " + IS_ARABIC + @" THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    ('mahjar.notifications.type.handoverprecontractstaff.title', N'مطلوب توثيق الحالة قبل التوقيع', N'Condition check needed before signing'),
    ('mahjar.notifications.type.handoverprecontractstaff.body',  N'العقد {0}: وثّق الحالة الحالية لـ {1} بالصور أو الفيديو، فلا يمكن إرسال العقد للعميل قبل ذلك.', N'Contract {0}: document the current condition of {1} with photos or video. The contract can''t be sent to the customer until you do.'),
    ('mahjar.notifications.type.handoverdeliverystaff.title',    N'طلب جاهز للتسليم',                     N'Order ready for handover'),
    ('mahjar.notifications.type.handoverdeliverystaff.body',     N'الطلب رقم {0} مدفوع: وثّق حالة {1} بالصور أو الفيديو ثم سلّمه للعميل.', N'Order #{0} is paid: document the condition of {1} with photos or video, then hand it over to the customer.'),
    ('mahjar.notifications.type.handoverreturnstaff.title',      N'إيجار بانتظار الإرجاع',                N'Rental awaiting return'),
    ('mahjar.notifications.type.handoverreturnstaff.body',       N'الطلب رقم {0}: {1} الآن لدى العميل، وموعد إرجاعه {2}. عند استلامه وثّق حالته وسجّل أي أضرار.', N'Order #{0}: {1} is now with the customer and is due back on {2}. When it comes back, document its condition and record any damage.'),
    ('mahjar.notifications.type.handoverdeliveredbuyer.title',   N'تم تسليم قطعتك',                       N'Your item was handed over'),
    ('mahjar.notifications.type.handoverdeliveredbuyer.body',    N'الطلب رقم {0}: تم تسليمك {1}، ووُثّقت حالته عند التسليم بالصور والفيديو.', N'Order #{0}: {1} was handed over to you. Its condition at handover was documented with photos and video.'),
    ('mahjar.notifications.type.handoverdeliveredadmin.title',   N'تم التسليم للعميل',                    N'Handed over to the customer'),
    ('mahjar.notifications.type.handoverdeliveredadmin.body',    N'الطلب رقم {0}: سلّم {2} القطعة {1} للعميل بعد توثيق حالتها.', N'Order #{0}: {2} handed {1} over to the customer after documenting its condition.'),
    ('mahjar.notifications.type.intakerecordedadmin.title',      N'تم استلام قطعة من شريك',               N'Item received from a partner'),
    ('mahjar.notifications.type.intakerecordedadmin.body',       N'استلم {1} القطعة {0} من الشريك ووثّق حالتها، وتم تحديث حالة المنتج.', N'{1} received {0} from the partner and documented its condition; the product''s condition report was updated.'),
    ('mahjar.notifications.type.intakerecordedvendor.title',     N'استلمنا قطعتك',                        N'We received your item'),
    ('mahjar.notifications.type.intakerecordedvendor.body',      N'استلم فريق محجر القطعة {0} ووثّق حالتها عند الاستلام.', N'The MHGR team received {0} and documented its condition on receipt.'),
    ('admin.mahjar.handover.delivery.partial',                   N'تم تسليم الصنف. سيُعلَّم الطلب كمسلّم عند تسليم جميع أصنافه.', N'The item was handed over. The order will be marked as delivered once all its items are handed over.'),
    ('shoppingcart.rental.rentedoutnopurchase',                  N'هذه القطعة مؤجرة أو محجوزة للإيجار حتى {0}، لذا لا يمكن شراؤها الآن. يمكنك استئجارها في تواريخ أخرى.', N'This item is rented out or booked for rental until {0}, so it can''t be bought now. You can still rent it for other dates.'),
    ('shoppingcart.rental.buyandrentsameitem',                   N'لا يمكن شراء القطعة نفسها واستئجارها في الطلب نفسه. احذف أحد السطرين.', N'You can''t buy and rent the same item in one order. Remove one of the two lines.'),
    ('shoppingcart.rental.notavailablefordates',                 N'عذراً، هذا المنتج غير متاح للتأجير في التواريخ المختارة. الرجاء اختيار تواريخ أخرى.', N'Sorry, this item is not available to rent for the selected dates. Please choose different dates.'),
    ('mahjar.api.orders.nothingtoreturn',                        N'لا يوجد ما يمكن إرجاعه في هذا الطلب: لم تُسلَّم القطعة المؤجرة إليك بعد، أو سُجّل إرجاعها مسبقًا.', N'There is nothing to return on this order: the rental has not been handed over to you yet, or its return is already recorded.')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }
}
