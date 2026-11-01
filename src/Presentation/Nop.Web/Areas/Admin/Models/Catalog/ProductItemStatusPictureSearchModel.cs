// [Mahjar item-status]
using Nop.Web.Framework.Models;

namespace Nop.Web.Areas.Admin.Models.Catalog;

/// <summary>
/// Represents an item status picture search model. [Mahjar item-status]
/// </summary>
public partial record ProductItemStatusPictureSearchModel : BaseSearchModel
{
    #region Properties

    /// <summary>
    /// Gets or sets the product identifier
    /// </summary>
    public int ProductId { get; set; }

    #endregion
}
