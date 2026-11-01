// [Mahjar item-status]
namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// One condition note on a product's report — a damage, a trace of wear, a previous repair, a missing part, an
/// included accessory or a general remark — carrying its own severity. Keyed on <see cref="ProductId"/> (not on
/// the <see cref="ProductItemStatus"/> row) like the photos, so the admin grid reads and writes it with just a
/// product identifier. [Mahjar item-status]
/// </summary>
public partial class ProductItemStatusNote : BaseEntity
{
    /// <summary>The product whose condition report this note belongs to.</summary>
    public int ProductId { get; set; }

    /// <summary>The note kind discriminator (backing value for <see cref="NoteType"/>).</summary>
    public int NoteTypeId { get; set; }

    /// <summary>The severity discriminator (backing value for <see cref="Severity"/>).</summary>
    public int SeverityId { get; set; }

    /// <summary>The note text shown to the customer.</summary>
    public string Text { get; set; }

    /// <summary>The display order (ascending, then Id).</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Creation date (UTC).</summary>
    public DateTime CreatedOnUtc { get; set; }

    /// <summary>Typed wrapper over <see cref="NoteTypeId"/>.</summary>
    public ItemStatusNoteType NoteType
    {
        get => (ItemStatusNoteType)NoteTypeId;
        set => NoteTypeId = (int)value;
    }

    /// <summary>Typed wrapper over <see cref="SeverityId"/>.</summary>
    public ItemStatusNoteSeverity Severity
    {
        get => (ItemStatusNoteSeverity)SeverityId;
        set => SeverityId = (int)value;
    }
}
