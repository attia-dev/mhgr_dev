// [Mahjar handover]
using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Mahjar;
using Nop.Core.Domain.Media;
using Nop.Data.Extensions;

namespace Nop.Data.Mapping.Builders.Mahjar;

/// <summary>
/// ConditionCheckPicture entity builder — two cascade FKs to two DIFFERENT parents (the check and the picture),
/// which SQL Server allows, mirroring ProductItemStatusPictureBuilder. [Mahjar handover]
/// </summary>
public partial class ConditionCheckPictureBuilder : NopEntityBuilder<ConditionCheckPicture>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(ConditionCheckPicture.ConditionCheckId)).AsInt32().ForeignKey<ConditionCheck>()
            .WithColumn(nameof(ConditionCheckPicture.PictureId)).AsInt32().ForeignKey<Picture>()
            .WithColumn(nameof(ConditionCheckPicture.Caption)).AsString(400).Nullable();
        //DisplayOrder is auto-mapped by the column conventions.
    }
}
