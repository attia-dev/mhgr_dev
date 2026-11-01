// [Mahjar handover]
using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Mahjar;
using Nop.Data.Extensions;

namespace Nop.Data.Mapping.Builders.Mahjar;

/// <summary>
/// ConditionCheck entity builder. [Mahjar handover]
/// </summary>
public partial class ConditionCheckBuilder : NopEntityBuilder<ConditionCheck>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(ConditionCheck.ProductId)).AsInt32().ForeignKey<Product>()
            .WithColumn(nameof(ConditionCheck.ConditionNotes)).AsString(int.MaxValue).Nullable()
            .WithColumn(nameof(ConditionCheck.DamageDescription)).AsString(int.MaxValue).Nullable()
            .WithColumn(nameof(ConditionCheck.InternalComment)).AsString(int.MaxValue).Nullable()
            .WithColumn(nameof(ConditionCheck.VideoFile)).AsString(1000).Nullable()
            .WithColumn(nameof(ConditionCheck.VideoUrl)).AsString(1000).Nullable();
        //ContractRequestId / OrderId / OrderItemId (0 = none) are deliberately NOT foreign keys: a pre-contract check
        //exists before any order does. They, the type/status/grade/severity ids, the deposit decimals, the employee
        //ids and the dates are auto-mapped by the column conventions.
    }
}
