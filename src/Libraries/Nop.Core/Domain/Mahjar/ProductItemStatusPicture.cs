// [Mahjar item-status]
namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// One real photo OF THIS EXACT PIECE attached to the product's condition report, with an optional caption.
/// Keyed on <see cref="ProductId"/> (not on the <see cref="ProductItemStatus"/> row) exactly like ProductPicture,
/// and pointing at a regular <c>Picture</c> row so the whole existing thumbnail/resize pipeline applies.
/// [Mahjar item-status]
/// </summary>
public partial class ProductItemStatusPicture : BaseEntity
{
    /// <summary>The product whose condition report this photo belongs to.</summary>
    public int ProductId { get; set; }

    /// <summary>The picture identifier (a standard nopCommerce Picture row).</summary>
    public int PictureId { get; set; }

    /// <summary>What the photo shows, e.g. "scratch on the clasp" (nullable).</summary>
    public string Caption { get; set; }

    /// <summary>The display order (ascending, then Id).</summary>
    public int DisplayOrder { get; set; }
}
