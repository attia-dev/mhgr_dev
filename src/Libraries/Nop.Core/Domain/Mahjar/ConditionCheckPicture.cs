// [Mahjar handover]
namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// One photo of a <see cref="ConditionCheck"/>. The photo itself is a standard nopCommerce Picture row, so it gets
/// the stock storage, thumbnail and resize pipeline. [Mahjar handover]
/// </summary>
public partial class ConditionCheckPicture : BaseEntity
{
    /// <summary>The condition check the photo belongs to.</summary>
    public int ConditionCheckId { get; set; }

    /// <summary>The picture identifier.</summary>
    public int PictureId { get; set; }

    /// <summary>An optional caption (e.g. "scratch on the bezel at 3 o'clock").</summary>
    public string Caption { get; set; }

    /// <summary>The display order.</summary>
    public int DisplayOrder { get; set; }
}
