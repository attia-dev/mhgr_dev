// [Mahjar handover]
using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// One check per checkpoint, enforced by the database: two staff (or a double-click) opening the same queue row at the
/// same moment used to be able to insert two drafts, and media attached to the "wrong" one was then ignored by the
/// send-contract guard and the queues. Filtered UNIQUE indexes now make a second insert fail, and
/// ConditionCheckService.GetOrCreateCheckAsync returns the winner's row instead:
///   - before signing: one check per (contract request, product)                  [CheckTypeId = 20]
///   - delivery / return: one check per (order item, checkpoint)                  [OrderItemId &gt; 0]
///   - intake: at most one DRAFT per product (confirmed intakes are history)      [CheckTypeId = 10 AND StatusId = 10]
/// Each index is created only when no duplicates exist yet (never fails a deploy on legacy data). Also seeds the two
/// messages the new guards raise. Idempotent; SQL Server dialect, matching the sibling Mahjar migrations.
/// [Mahjar handover]
/// </summary>
[NopSchemaMigration("2026-09-16 10:10:00", "Mahjar handover: one condition check per checkpoint (unique indexes)")]
public class HandoverIntegrityMigration : ForwardOnlyMigration
{
    /// <summary>The Arabic-language predicate every Mahjar resource migration uses.</summary>
    private const string IS_ARABIC = "(l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%')";

    public override void Up()
    {
        IfDatabase("sqlserver").Execute.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ConditionCheck_PreContract' AND object_id = OBJECT_ID('dbo.ConditionCheck'))
   AND NOT EXISTS (SELECT 1 FROM ConditionCheck WHERE CheckTypeId = 20 GROUP BY ContractRequestId, ProductId HAVING COUNT(*) > 1)
    CREATE UNIQUE NONCLUSTERED INDEX UX_ConditionCheck_PreContract ON dbo.ConditionCheck (ContractRequestId, ProductId) WHERE CheckTypeId = 20;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ConditionCheck_OrderItem' AND object_id = OBJECT_ID('dbo.ConditionCheck'))
   AND NOT EXISTS (SELECT 1 FROM ConditionCheck WHERE OrderItemId > 0 GROUP BY OrderItemId, CheckTypeId HAVING COUNT(*) > 1)
    CREATE UNIQUE NONCLUSTERED INDEX UX_ConditionCheck_OrderItem ON dbo.ConditionCheck (OrderItemId, CheckTypeId) WHERE OrderItemId > 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ConditionCheck_IntakeDraft' AND object_id = OBJECT_ID('dbo.ConditionCheck'))
   AND NOT EXISTS (SELECT 1 FROM ConditionCheck WHERE CheckTypeId = 10 AND StatusId = 10 GROUP BY ProductId HAVING COUNT(*) > 1)
    CREATE UNIQUE NONCLUSTERED INDEX UX_ConditionCheck_IntakeDraft ON dbo.ConditionCheck (ProductId) WHERE CheckTypeId = 10 AND StatusId = 10;");

        IfDatabase("sqlserver").Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN " + IS_ARABIC + @" THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    ('admin.mahjar.handover.alreadyrecorded', N'تم تسجيل هذا التسليم أو الإرجاع مسبقًا.', N'This handover has already been recorded.'),
    ('admin.mahjar.handover.caption.toolong', N'يجب ألا يتجاوز التعليق {0} حرفًا.',          N'The caption cannot be longer than {0} characters.')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");
    }
}
