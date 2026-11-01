// [Mahjar item-status]
using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Models.Catalog;

/// <summary>
/// Represents the header record of a condition report ("item status") as edited on the "Item status" card of
/// the product editor. It is rendered INSIDE the main product form, so every field below round-trips with the
/// ordinary product Save button. [Mahjar item-status]
/// </summary>
public partial record ProductItemStatusModel : BaseNopModel
{
    #region Properties

    /// <summary>
    /// Gets or sets the product identifier this condition report belongs to
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the condition report is shown to customers
    /// </summary>
    [NopResourceDisplayName("Admin.Catalog.Products.ItemStatus.Fields.ShowOnProductPage")]
    public bool ShowOnProductPage { get; set; }

    /// <summary>
    /// Gets or sets the overall condition grade identifier (see ItemConditionGrade)
    /// </summary>
    [NopResourceDisplayName("Admin.Catalog.Products.ItemStatus.Fields.ConditionGrade")]
    public int ConditionGradeId { get; set; }

    /// <summary>
    /// Gets or sets the free-text condition summary
    /// </summary>
    [NopResourceDisplayName("Admin.Catalog.Products.ItemStatus.Fields.Summary")]
    public string Summary { get; set; }

    /// <summary>
    /// Gets or sets the date the piece was physically inspected
    /// </summary>
    [NopResourceDisplayName("Admin.Catalog.Products.ItemStatus.Fields.InspectedOn")]
    [UIHint("DateNullable")]
    public DateTime? InspectedOn { get; set; }

    /// <summary>
    /// Gets or sets an EXTERNAL embed/media URL pasted by the partner (optional)
    /// </summary>
    [NopResourceDisplayName("Admin.Catalog.Products.ItemStatus.Fields.VideoUrl")]
    public string VideoUrl { get; set; }

    /// <summary>
    /// Gets or sets the site-relative path of the UPLOADED video we host ourselves. Display only: it is owned
    /// by the upload/delete actions, so the product form re-reads it from the database instead of trusting the
    /// posted value.
    /// </summary>
    public string VideoFile { get; set; }

    /// <summary>
    /// Gets or sets the largest video size (in megabytes) the uploader accepts (from ItemStatusSettings)
    /// </summary>
    public int MaxVideoSizeMb { get; set; }

    /// <summary>
    /// Gets or sets the comma-separated list of accepted video extensions (from ItemStatusSettings)
    /// </summary>
    public string AllowedVideoExtensions { get; set; }

    #endregion
}
