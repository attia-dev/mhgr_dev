// [Mahjar item-status]
using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Mahjar;
using Nop.Data.Extensions;

namespace Nop.Data.Mapping.Builders.Mahjar;

/// <summary>
/// ProductItemStatusNote entity builder — one condition note of a product. [Mahjar item-status]
/// </summary>
public partial class ProductItemStatusNoteBuilder : NopEntityBuilder<ProductItemStatusNote>
{
    #region Methods

    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(ProductItemStatusNote.ProductId)).AsInt32().ForeignKey<Product>()
            .WithColumn(nameof(ProductItemStatusNote.Text)).AsString(int.MaxValue).NotNullable();
        //NoteTypeId / SeverityId / DisplayOrder / CreatedOnUtc are auto-mapped by the column conventions.
    }

    #endregion
}
