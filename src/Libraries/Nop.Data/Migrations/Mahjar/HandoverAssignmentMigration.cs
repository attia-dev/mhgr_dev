// [Mahjar handover]
using FluentMigrator;
using Nop.Core.Domain.Mahjar;
using Nop.Data.Extensions;
using Nop.Data.Mapping;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// An order's handover belongs to ONE employee of the desk instead of the whole team: the admin picks them on the
/// order page, and that person alone hands the items over and, for a rental, takes them back.
///
/// 1) The <see cref="HandoverAssignment"/> table, one row per order (a unique index enforces it).
/// 2) The locale resources of the order-page card, the queue column, the refusals, and the new admin notification
///    that a paid order is waiting for someone to be assigned to it.
///
/// NOTE: the date string below IS the migration version — it must differ from every other Mahjar migration's, or the
/// runner silently treats this one as already applied. Locale resources are cached: restart (or POST
/// /Admin/Common/ClearCache) after it runs. SQL Server dialect, matching the sibling Mahjar migrations.
/// [Mahjar handover]
/// </summary>
[NopSchemaMigration("2026-09-16 11:30:00", "Mahjar handover: an order's handover is assigned to one desk employee")]
public class HandoverAssignmentMigration : ForwardOnlyMigration
{
    /// <summary>The Arabic-language predicate every Mahjar resource migration uses.</summary>
    private const string IS_ARABIC = "(l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')";

    /// <summary>The T-SQL-only dialect guard every Mahjar migration uses.</summary>
    private const string DATABASE_TYPE = "sqlserver";

    public override void Up()
    {
        CreateTable();
        CreateIndexes();
        AddResources();
    }

    #region Utilities

    /// <summary>1) Schema — FluentMigrator's version table guards re-runs.</summary>
    protected virtual void CreateTable()
    {
        Create.TableFor<HandoverAssignment>();
    }

    /// <summary>One assignment per order, and the lookup the queues use.</summary>
    protected virtual void CreateIndexes()
    {
        var table = NameCompatibilityManager.GetTableName(typeof(HandoverAssignment));

        if (!Schema.Table(table).Index("IX_HandoverAssignment_Order").Exists())
            IfDatabase(DATABASE_TYPE).Create.Index("IX_HandoverAssignment_Order").OnTable(table)
                .OnColumn(nameof(HandoverAssignment.OrderId)).Ascending()
                .WithOptions().Unique();

        if (!Schema.Table(table).Index("IX_HandoverAssignment_Employee").Exists())
            IfDatabase(DATABASE_TYPE).Create.Index("IX_HandoverAssignment_Employee").OnTable(table)
                .OnColumn(nameof(HandoverAssignment.AssignedToCustomerId)).Ascending();
    }

    /// <summary>2) The order-page card, the queue column, the refusals and the new admin notification (en + ar).</summary>
    protected virtual void AddResources()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN " + IS_ARABIC + @" THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    ('admin.mahjar.handover.assign.card',       N'التسليم والاستلام لهذا الطلب',      N'Handover of this order'),
    ('admin.mahjar.handover.assign.hint',       N'اختر موظف التسليم المسؤول عن هذا الطلب. هو وحده من يسلّم قطعه للعميل، وهو نفسه من يستلمها عند الإرجاع في حالة التأجير.', N'Choose the desk employee responsible for this order. They alone hand its items over to the customer, and the same person takes them back when a rental is returned.'),
    ('admin.mahjar.handover.fields.assignedto', N'الموظف المسؤول',                   N'Assigned to'),
    ('admin.mahjar.handover.fields.assignedto.hint', N'موظف مكتب التسليم المسؤول عن تسليم هذا الطلب واستلامه.', N'The handover employee responsible for delivering this order and taking it back.'),
    ('admin.mahjar.handover.assign.none',       N'لم يُسنَد بعد',                     N'Not assigned yet'),
    ('admin.mahjar.handover.assign.button',     N'إسناد',                            N'Assign'),
    ('admin.mahjar.handover.assign.done',       N'تم إسناد الطلب إلى الموظف، وأُشعِر بالمهمة.', N'The order was assigned to the employee, who has been notified.'),
    ('admin.mahjar.handover.assign.cleared',    N'تم إلغاء الإسناد، والطلب الآن بانتظار إسناده إلى موظف.', N'The assignment was cleared; the order is waiting to be assigned again.'),
    ('admin.mahjar.handover.assign.notstaff',   N'يجب اختيار موظف فعّال من مكتب التسليم.', N'Pick an active member of the handover desk.'),
    ('admin.mahjar.handover.assign.notyours',   N'هذا الطلب مُسنَد إلى موظف آخر، ولا يمكنك تسليمه أو استلامه. راجع الإدارة لإعادة إسناده.', N'This order is assigned to another employee, so you cannot hand it over or take it back. Ask an admin to reassign it.'),
    ('admin.mahjar.handover.assign.nostaff',    N'لا يوجد موظفون في مكتب التسليم بعد. أضف موظفًا إلى دور «موظفو التسليم» أولًا.', N'There are no handover employees yet. Add someone to the Handover staff role first.'),
    ('mahjar.notifications.type.handoverunassignedadmin.title', N'طلب بانتظار إسناد موظف',            N'Order waiting to be assigned'),
    ('mahjar.notifications.type.handoverunassignedadmin.body',  N'الطلب رقم {0} مدفوع وبانتظار التسليم، ولم يُسنَد إلى موظف بعد: {1}. افتح الطلب لإسناده.', N'Order #{0} is paid and waiting to be handed over, but no employee is assigned to it yet: {1}. Open the order to assign it.')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }

    #endregion
}
