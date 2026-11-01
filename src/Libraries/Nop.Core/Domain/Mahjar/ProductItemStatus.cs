// [Mahjar item-status]
namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// The header record of a product's condition report ("item status"): the overall grade, a free-text summary,
/// the recorded video and when the piece was inspected. Exactly ONE row per product — the row is keyed on
/// <see cref="ProductId"/> (not on its own <see cref="BaseEntity.Id"/>), mirroring ProductPicture, so the admin
/// grids and the storefront can read and write it with just a product identifier.
///
/// The photos and the notes live in the sibling <see cref="ProductItemStatusPicture"/> and
/// <see cref="ProductItemStatusNote"/> tables, also keyed on ProductId. [Mahjar item-status]
/// </summary>
public partial class ProductItemStatus : BaseEntity
{
    /// <summary>The product this condition report belongs to (one row per product).</summary>
    public int ProductId { get; set; }

    /// <summary>The overall grade discriminator (backing value for <see cref="ConditionGrade"/>).</summary>
    public int ConditionGradeId { get; set; }

    /// <summary>Free-text condition summary written by the partner or the admin (nullable).</summary>
    public string Summary { get; set; }

    /// <summary>
    /// The site-relative virtual path of an UPLOADED video we host ourselves, e.g.
    /// <c>/images/uploaded/item-status/12/9f3a....mp4</c>; null when no file was uploaded. That tree is writable,
    /// survives redeploys, and is served by the static-file middleware with HTTP range support (needed for
    /// &lt;video&gt; seeking). Wins over <see cref="VideoUrl"/> when both are set.
    /// </summary>
    public string VideoFile { get; set; }

    /// <summary>An EXTERNAL embed/media URL the partner pasted instead of uploading a file (nullable).</summary>
    public string VideoUrl { get; set; }

    /// <summary>When the piece was physically inspected (UTC), if recorded.</summary>
    public DateTime? InspectedOnUtc { get; set; }

    /// <summary>Whether the condition report is shown to customers on the product page and in the app.</summary>
    public bool ShowOnProductPage { get; set; }

    /// <summary>Last edit date (UTC).</summary>
    public DateTime UpdatedOnUtc { get; set; }

    /// <summary>Typed wrapper over <see cref="ConditionGradeId"/>.</summary>
    public ItemConditionGrade ConditionGrade
    {
        get => (ItemConditionGrade)ConditionGradeId;
        set => ConditionGradeId = (int)value;
    }
}
