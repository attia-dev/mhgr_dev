// [Mahjar handover]
using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// A desk employee opening "ready to hand over" / "due back" from the bell used to land on the WHOLE queue and had to
/// find the order again. The consumer now writes the order into the link; this repoints the rows that are already in
/// people's bells, so the notifications they can see right now behave the same as new ones.
///
/// Type 71 = HandoverDeliveryStaff → the awaiting-delivery queue (stage 20); type 72 = HandoverReturnStaff → the
/// awaiting-return queue (stage 30). EntityId of those rows is the order id. Rows already carrying a keyword, and
/// rows whose order has no number, are left alone.
///
/// NOTE: the date string below IS the migration version — it must differ from every other Mahjar migration's, or the
/// runner silently treats this one as already applied. SQL Server dialect, matching the sibling Mahjar migrations.
/// [Mahjar handover]
/// </summary>
[NopSchemaMigration("2026-09-16 12:30:00", "Mahjar handover: desk task notifications open the order they are about")]
public class HandoverTaskLinksMigration : ForwardOnlyMigration
{
    /// <summary>The T-SQL-only dialect guard every Mahjar migration uses.</summary>
    private const string DATABASE_TYPE = "sqlserver";

    public override void Up()
    {
        IfDatabase(DATABASE_TYPE).Execute.Sql(@"
UPDATE n
SET Url = '/Admin/Handover/List?stageId=20&keyword=' + o.CustomOrderNumber
FROM Notification n
INNER JOIN [Order] o ON o.Id = n.EntityId
WHERE n.NotificationTypeId = 71
  AND n.Url LIKE '/Admin/Handover/List%'
  AND n.Url NOT LIKE '%keyword=%'
  AND ISNULL(o.CustomOrderNumber, '') <> '';

UPDATE n
SET Url = '/Admin/Handover/List?stageId=30&keyword=' + o.CustomOrderNumber
FROM Notification n
INNER JOIN [Order] o ON o.Id = n.EntityId
WHERE n.NotificationTypeId = 72
  AND n.Url LIKE '/Admin/Handover/List%'
  AND n.Url NOT LIKE '%keyword=%'
  AND ISNULL(o.CustomOrderNumber, '') <> '';");
    }
}
