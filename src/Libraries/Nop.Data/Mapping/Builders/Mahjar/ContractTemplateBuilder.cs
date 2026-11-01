// [Mahjar contracts]
using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Mahjar;

namespace Nop.Data.Mapping.Builders.Mahjar;

/// <summary>
/// ContractTemplate entity builder. [Mahjar contracts]
/// </summary>
public partial class ContractTemplateBuilder : NopEntityBuilder<ContractTemplate>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(ContractTemplate.Name)).AsString(400).NotNullable()
            .WithColumn(nameof(ContractTemplate.Body)).AsString(int.MaxValue).NotNullable();
        //TemplateTypeId and UpdatedOnUtc are auto-mapped by the column conventions. On an already-installed DB
        //TemplateTypeId is added by ContractTemplateTypesMigration; on a fresh install it lands here.
    }
}
