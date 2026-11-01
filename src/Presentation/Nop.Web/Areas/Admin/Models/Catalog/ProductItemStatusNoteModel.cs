// [Mahjar item-status]
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Models.Catalog;

/// <summary>
/// Represents one condition note attached to a product item status. NoteTypeName / SeverityName are the
/// localized, read-only display columns of the notes grid; the identifiers behind them are chosen once, in the
/// "add a note" form. [Mahjar item-status]
/// </summary>
public partial record ProductItemStatusNoteModel : BaseNopEntityModel
{
    #region Properties

    /// <summary>
    /// Gets or sets the product identifier
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the note kind identifier (see ItemStatusNoteType)
    /// </summary>
    [NopResourceDisplayName("Admin.Catalog.Products.ItemStatus.Fields.NoteType")]
    public int NoteTypeId { get; set; }

    /// <summary>
    /// Gets or sets the localized note kind name (not existing in the entity)
    /// </summary>
    [NopResourceDisplayName("Admin.Catalog.Products.ItemStatus.Fields.NoteType")]
    public string NoteTypeName { get; set; }

    /// <summary>
    /// Gets or sets the severity identifier (see ItemStatusNoteSeverity)
    /// </summary>
    [NopResourceDisplayName("Admin.Catalog.Products.ItemStatus.Fields.Severity")]
    public int SeverityId { get; set; }

    /// <summary>
    /// Gets or sets the localized severity name (not existing in the entity)
    /// </summary>
    [NopResourceDisplayName("Admin.Catalog.Products.ItemStatus.Fields.Severity")]
    public string SeverityName { get; set; }

    /// <summary>
    /// Gets or sets the note text shown to the customer
    /// </summary>
    [NopResourceDisplayName("Admin.Catalog.Products.ItemStatus.Fields.Text")]
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the display order
    /// </summary>
    [NopResourceDisplayName("Admin.Catalog.Products.ItemStatus.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    #endregion
}
