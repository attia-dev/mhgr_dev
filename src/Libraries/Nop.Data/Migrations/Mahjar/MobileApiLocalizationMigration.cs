using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Locale-string resources for the mobile REST API: every success/failure message the API returns to the
/// app becomes a `mahjar.api.*` resource (en + ar), resolved per request via the Accept-Language header
/// (see WebWorkContext.GetLanguageFromApiRequestAsync). Also upgrades the stock Arabic order/payment/
/// shipping/return status enum labels to the curated wording the app used to hardcode, so
/// GetLocalizedEnumAsync returns polished Arabic on both web and app. Idempotent. SQL Server dialect.
/// [Mahjar i18n]
/// </summary>
[NopSchemaMigration("2026-07-05 10:00:00", "Mahjar: mobile API bilingual messages + curated Arabic status labels")]
public class MobileApiLocalizationMigration : ForwardOnlyMigration
{
    public override void Up()
    {
        // 1) API messages — one row per installed language (Arabic text for ar, English otherwise)
        Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN (l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%') THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    ('mahjar.api.common.invalidtoken',            N'انتهت صلاحية الجلسة، يرجى تسجيل الدخول من جديد.', N'Your session has expired. Please sign in again.'),
    ('mahjar.api.common.invalidrequest',          N'طلب غير صالح.', N'Invalid request.'),
    ('mahjar.api.common.productrequired',         N'المنتج مطلوب.', N'Product is required.'),
    ('mahjar.api.common.productnotfound',         N'المنتج غير موجود.', N'Product not found.'),
    ('mahjar.api.common.ordernotfound',           N'الطلب غير موجود.', N'Order not found.'),
    ('mahjar.api.common.vendornotfound',          N'الشريك غير موجود.', N'Partner not found.'),
    ('mahjar.api.common.customerfallback',        N'عميل', N'Customer'),
    ('mahjar.api.common.emailrequired',           N'يرجى إدخال بريد إلكتروني صحيح.', N'Please enter a valid email address.'),
    ('mahjar.api.common.coderequired',            N'الرمز مطلوب.', N'Code is required.'),
    ('mahjar.api.auth.credentialsrequired',       N'البريد الإلكتروني وكلمة المرور مطلوبان.', N'Email and password are required.'),
    ('mahjar.api.auth.emailexists',               N'يوجد حساب مسجّل بهذا البريد الإلكتروني.', N'An account with this email already exists.'),
    ('mahjar.api.auth.registered.checkemail',     N'تم إنشاء حسابك. تحقق من بريدك الإلكتروني للحصول على رمز التفعيل ثم سجّل الدخول.', N'Your account has been created. Check your email for the activation code, then sign in.'),
    ('mahjar.api.auth.registered.pendingapproval', N'تم إنشاء حسابك وهو بانتظار الموافقة.', N'Your account has been created and is awaiting approval.'),
    ('mahjar.api.auth.activationrequest',         N'البريد الإلكتروني ورمز التفعيل مطلوبان.', N'Email and activation code are required.'),
    ('mahjar.api.auth.resetrequest',              N'البريد الإلكتروني والرمز وكلمة المرور الجديدة مطلوبة.', N'Email, code and new password are required.'),
    ('mahjar.api.auth.invalidemailortoken',       N'البريد الإلكتروني أو الرمز غير صحيح.', N'Invalid email or code.'),
    ('mahjar.api.auth.alreadyactivated',          N'هذا الحساب مفعّل بالفعل.', N'This account is already activated.'),
    ('mahjar.api.auth.invalidactivationtoken',    N'رمز التفعيل غير صحيح.', N'Invalid activation code.'),
    ('mahjar.api.auth.invalidcredentials',        N'البريد الإلكتروني أو كلمة المرور غير صحيح.', N'Incorrect email or password.'),
    ('mahjar.api.auth.resetsent',                 N'إذا كان هناك حساب بهذا البريد، فقد أرسلنا رسالة لإعادة تعيين كلمة المرور.', N'If an account exists for this email, a password-reset message has been sent.'),
    ('mahjar.api.auth.noresetrequest',            N'لا يوجد طلب فعّال لإعادة تعيين كلمة المرور.', N'No active password-reset request.'),
    ('mahjar.api.auth.invalidorexpiredtoken',     N'الرمز غير صالح أو منتهي الصلاحية.', N'This code is invalid or has expired.'),
    ('mahjar.api.auth.resetexpired',              N'انتهت صلاحية رابط إعادة التعيين.', N'The reset link has expired.'),
    ('mahjar.api.auth.loggedout',                 N'تم تسجيل الخروج من جميع الأجهزة.', N'Signed out from all devices.'),
    ('mahjar.api.account.newpasswordrequired',    N'كلمة المرور الجديدة مطلوبة.', N'New password is required.'),
    ('mahjar.api.account.passwordchanged',        N'تم تغيير كلمة المرور.', N'Your password has been changed.'),
    ('mahjar.api.account.addressnotfound',        N'العنوان غير موجود.', N'Address not found.'),
    ('mahjar.api.account.addressdeleted',         N'تم حذف العنوان.', N'Address deleted.'),
    ('mahjar.api.cart.itemnotfound',              N'العنصر غير موجود في السلة.', N'Cart item not found.'),
    ('mahjar.api.checkout.addressrequired',       N'عنوان الشحن مطلوب.', N'Shipping address is required.'),
    ('mahjar.api.checkout.cartempty',             N'سلتك فارغة.', N'Your cart is empty.'),
    ('mahjar.api.checkout.noshippingoptions',     N'لا تتوفر خيارات شحن لهذا العنوان.', N'No shipping options are available for this address.'),
    ('mahjar.api.checkout.addressidrequired',     N'يرجى اختيار عنوان صحيح.', N'Please choose a valid address.'),
    ('mahjar.api.checkout.countryrequired',       N'الدولة مطلوبة.', N'Country is required.'),
    ('mahjar.api.checkout.placeorderfailed',      N'تعذّر إتمام الطلب، يرجى المحاولة مرة أخرى.', N'We couldn''t place your order. Please try again.'),
    ('mahjar.api.orders.cannotcancel',            N'لم يعد بالإمكان إلغاء هذا الطلب.', N'This order can no longer be cancelled.'),
    ('mahjar.api.orders.norentalitems',           N'لا يوجد عنصر مستأجر لإرجاعه في هذا الطلب.', N'No rental item found to return on this order.'),
    ('mahjar.api.orders.returnnote',              N'تم طلب إرجاع {0} من العناصر المستأجرة.{1}', N'Rental return requested for {0} item(s).{1}'),
    ('mahjar.api.orders.returnnotedetail',        N' ملاحظة: {0}', N' Note: {0}'),
    ('mahjar.api.orders.returnrequested',         N'تم طلب الإرجاع. سيفحص الشريك المنتج ثم يُعاد مبلغ التأمين.', N'Return requested. The partner will inspect the item(s) and release your deposit.'),
    ('mahjar.api.orders.reordered',               N'تمت إضافة العناصر إلى سلتك.', N'Items added to your cart.'),
    ('mahjar.api.wishlist.itemnotfound',          N'العنصر غير موجود في المفضلة.', N'Wishlist item not found.'),
    ('mahjar.api.reviews.ratingrequired',         N'يرجى اختيار تقييم من 1 إلى 5.', N'A rating from 1 to 5 is required.'),
    ('mahjar.api.reviews.textrequired',           N'نص المراجعة مطلوب.', N'Review text is required.'),
    ('mahjar.api.vendorapply.disabled',           N'التقديم للشراكة غير متاح حالياً.', N'Partner applications are not open at the moment.'),
    ('mahjar.api.vendorapply.adminsnotallowed',   N'لا يمكن للمسؤولين التقديم على حساب شريك.', N'Administrators cannot apply for a partner account.'),
    ('mahjar.api.vendorapply.alreadyapplied',     N'لقد قدّمت طلب شراكة من قبل.', N'You have already applied for a partner account.'),
    ('mahjar.api.vendorapply.namerequired',       N'الاسم مطلوب.', N'Name is required.'),
    ('mahjar.api.reference.topicnotfound',        N'الصفحة غير موجودة.', N'Page not found.'),
    ('mahjar.api.returns.notallowed',             N'الإرجاع غير متاح لهذا الطلب.', N'Returns are not allowed for this order.'),
    ('mahjar.api.returns.selectitems',            N'اختر عنصراً واحداً على الأقل للإرجاع.', N'Select at least one item to return.'),
    ('mahjar.api.returns.novaliditems',           N'لا توجد عناصر صالحة للإرجاع.', N'No valid items to return.'),
    ('mahjar.api.returns.submitted',              N'تم إرسال طلب الإرجاع.', N'Your return request has been submitted.'),
    ('mahjar.api.backinstock.subscribed',         N'سنُعلمك عند توفّر هذا المنتج من جديد.', N'You will be notified when this item is back in stock.'),
    ('mahjar.api.backinstock.unsubscribed',       N'تم إلغاء التنبيه.', N'Notification removed.'),
    ('mahjar.api.blog.postnotfound',              N'التدوينة غير موجودة.', N'Blog post not found.'),
    ('mahjar.api.news.itemnotfound',              N'الخبر غير موجود.', N'News item not found.'),
    ('mahjar.api.content.commentrequired',        N'نص التعليق مطلوب.', N'Comment text is required.'),
    ('mahjar.api.content.commentsnotallowed',     N'التعليقات غير متاحة.', N'Comments are not allowed.'),
    ('mahjar.api.newsletter.subscribed',          N'تم اشتراكك في النشرة البريدية.', N'You''re subscribed to our newsletter.'),
    ('mahjar.api.notifications.markedread',       N'تم التحديد كمقروء.', N'Marked as read.'),
    ('mahjar.api.notifications.allmarkedread',    N'تم تحديد الكل كمقروء.', N'All marked as read.'),
    ('mahjar.api.support.bodyrequired',           N'نص الرسالة مطلوب.', N'Message text is required.'),
    ('mahjar.api.support.sent',                   N'تم إرسال رسالتك.', N'Your message has been sent.'),
    ('mahjar.api.support.vendordisabled',         N'التواصل مع الشركاء غير متاح.', N'Contacting partners is disabled.'),
    ('mahjar.api.support.senttovendor',           N'تم إرسال رسالتك إلى الشريك.', N'Your message has been sent to the partner.')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");

        // 2) Curated Arabic status labels — the wording the app's status maps used (better than the stock
        //    Arabic pack, e.g. 'مكتمل' instead of 'انهاء'). Update existing Arabic rows, insert any missing.
        Execute.Sql(@"
DECLARE @statuses TABLE (ResourceName NVARCHAR(200), ArValue NVARCHAR(200), EnValue NVARCHAR(200));
INSERT INTO @statuses VALUES
    ('enums.nop.core.domain.orders.orderstatus.pending',                N'قيد الانتظار',              N'Pending'),
    ('enums.nop.core.domain.orders.orderstatus.processing',             N'قيد المعالجة',              N'Processing'),
    ('enums.nop.core.domain.orders.orderstatus.complete',               N'مكتمل',                     N'Complete'),
    ('enums.nop.core.domain.orders.orderstatus.cancelled',              N'ملغى',                      N'Cancelled'),
    ('enums.nop.core.domain.payments.paymentstatus.pending',            N'بانتظار الدفع',             N'Pending'),
    ('enums.nop.core.domain.payments.paymentstatus.authorized',         N'معتمد',                     N'Authorized'),
    ('enums.nop.core.domain.payments.paymentstatus.paid',               N'مدفوع',                     N'Paid'),
    ('enums.nop.core.domain.payments.paymentstatus.partiallyrefunded',  N'مسترد جزئياً',              N'Partially refunded'),
    ('enums.nop.core.domain.payments.paymentstatus.refunded',           N'مسترد',                     N'Refunded'),
    ('enums.nop.core.domain.payments.paymentstatus.voided',             N'ملغى',                      N'Voided'),
    ('enums.nop.core.domain.shipping.shippingstatus.shippingnotrequired', N'لا يتطلب شحن',            N'Shipping not required'),
    ('enums.nop.core.domain.shipping.shippingstatus.notyetshipped',     N'لم يُشحن بعد',              N'Not yet shipped'),
    ('enums.nop.core.domain.shipping.shippingstatus.partiallyshipped',  N'مشحون جزئياً',              N'Partially shipped'),
    ('enums.nop.core.domain.shipping.shippingstatus.shipped',           N'تم الشحن',                  N'Shipped'),
    ('enums.nop.core.domain.shipping.shippingstatus.delivered',         N'تم التسليم',                N'Delivered'),
    ('enums.nop.core.domain.orders.returnrequeststatus.pending',        N'بانتظار المراجعة',          N'Pending'),
    ('enums.nop.core.domain.orders.returnrequeststatus.received',       N'تم الاستلام',               N'Received'),
    ('enums.nop.core.domain.orders.returnrequeststatus.returnauthorized', N'تمت الموافقة على الإرجاع', N'Return authorized');

UPDATE r SET r.ResourceValue = v.ArValue
FROM LocaleStringResource r
JOIN Language l ON l.Id = r.LanguageId AND (l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')
JOIN @statuses v ON v.ResourceName = r.ResourceName;

INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN (l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%') THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN @statuses v
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }
}
