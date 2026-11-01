// [Mahjar home-slides]
using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Mahjar;

namespace Nop.Data.Mapping.Builders.Mahjar;

/// <summary>
/// HomeSlide entity builder. [Mahjar home-slides]
/// </summary>
public partial class HomeSlideBuilder : NopEntityBuilder<HomeSlide>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(HomeSlide.VideoUrl)).AsString(1000).Nullable()
            .WithColumn(nameof(HomeSlide.Kicker)).AsString(150).Nullable()
            .WithColumn(nameof(HomeSlide.Title)).AsString(250).NotNullable()
            .WithColumn(nameof(HomeSlide.Subtitle)).AsString(600).Nullable()
            .WithColumn(nameof(HomeSlide.ButtonText)).AsString(100).Nullable()
            .WithColumn(nameof(HomeSlide.TargetValue)).AsString(1000).Nullable();
        //PictureId / MobilePictureId are deliberately NOT foreign keys to Picture: a cascade FK would delete the
        //slide with its picture, MobilePictureId is 0 when unset, and the seed slides share product pictures.
        //Every other column (ints, bools, dates) is auto-mapped by the column conventions; the typed enum
        //wrappers are skipped.
    }
}
