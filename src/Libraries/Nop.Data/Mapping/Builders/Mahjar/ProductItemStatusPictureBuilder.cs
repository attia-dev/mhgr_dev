// [Mahjar item-status]
using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Mahjar;
using Nop.Core.Domain.Media;
using Nop.Data.Extensions;

namespace Nop.Data.Mapping.Builders.Mahjar;

/// <summary>
/// ProductItemStatusPicture entity builder — one condition photo of a product. Mirrors ProductPictureBuilder
/// (two cascade FKs to two DIFFERENT parents, which SQL Server allows). [Mahjar item-status]
/// </summary>
public partial class ProductItemStatusPictureBuilder : NopEntityBuilder<ProductItemStatusPicture>
{
    #region Methods

    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(ProductItemStatusPicture.PictureId)).AsInt32().ForeignKey<Picture>()
            .WithColumn(nameof(ProductItemStatusPicture.ProductId)).AsInt32().ForeignKey<Product>()
            .WithColumn(nameof(ProductItemStatusPicture.Caption)).AsString(400).Nullable();
        //DisplayOrder is auto-mapped by the column conventions.
    }

    #endregion
}
