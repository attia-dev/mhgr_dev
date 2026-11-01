// [Mahjar item-status]
using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Mahjar;
using Nop.Data.Extensions;

namespace Nop.Data.Mapping.Builders.Mahjar;

/// <summary>
/// ProductItemStatus entity builder — the header row of a product's condition report. [Mahjar item-status]
/// </summary>
public partial class ProductItemStatusBuilder : NopEntityBuilder<ProductItemStatus>
{
    #region Methods

    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(ProductItemStatus.ProductId)).AsInt32().ForeignKey<Product>()
            .WithColumn(nameof(ProductItemStatus.Summary)).AsString(int.MaxValue).Nullable()
            .WithColumn(nameof(ProductItemStatus.VideoFile)).AsString(1000).Nullable()
            .WithColumn(nameof(ProductItemStatus.VideoUrl)).AsString(1000).Nullable();
        //ConditionGradeId / InspectedOnUtc / ShowOnProductPage / UpdatedOnUtc are auto-mapped by the
        //column conventions.
    }

    #endregion
}
