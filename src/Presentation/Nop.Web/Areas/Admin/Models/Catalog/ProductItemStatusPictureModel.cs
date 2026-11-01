// [Mahjar item-status]
using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Models.Catalog;

/// <summary>
/// Represents one condition photo attached to a product item status. [Mahjar item-status]
/// </summary>
public partial record ProductItemStatusPictureModel : BaseNopEntityModel
{
    #region Properties

    /// <summary>
    /// Gets or sets the product identifier
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the picture identifier. Also drives the FilePond uploader on the Photos tab.
    /// </summary>
    [UIHint("MultiPicture")]
    [NopResourceDisplayName("Admin.Catalog.Products.ItemStatus.Fields.Picture")]
    public int PictureId { get; set; }

    /// <summary>
    /// Gets or sets the picture URL (not existing in the entity)
    /// </summary>
    [NopResourceDisplayName("Admin.Catalog.Products.ItemStatus.Fields.Picture")]
    public string PictureUrl { get; set; }

    /// <summary>
    /// Gets or sets what the photo shows
    /// </summary>
    [NopResourceDisplayName("Admin.Catalog.Products.ItemStatus.Fields.Caption")]
    public string Caption { get; set; }

    /// <summary>
    /// Gets or sets the display order
    /// </summary>
    [NopResourceDisplayName("Admin.Catalog.Products.ItemStatus.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    #endregion
}
