// [Mahjar item-status]
using Nop.Web.Framework.Models;

namespace Nop.Web.Areas.Admin.Models.Catalog;

/// <summary>
/// Represents an item status note search model. [Mahjar item-status]
/// </summary>
public partial record ProductItemStatusNoteSearchModel : BaseSearchModel
{
    #region Properties

    /// <summary>
    /// Gets or sets the product identifier
    /// </summary>
    public int ProductId { get; set; }

    #endregion
}
