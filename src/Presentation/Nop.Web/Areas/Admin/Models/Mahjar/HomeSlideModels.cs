// [Mahjar home-slides]
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Models.Mahjar;

/// <summary>
/// Represents the home slide search model (the list page and its optional placement filter). The filter is a
/// nullable id — an empty value means "all placements" — because Hero is 0 and a negative sentinel would not
/// survive the Arabic admin's number binding. [Mahjar home-slides]
/// </summary>
public partial record HomeSlideSearchModel : BaseSearchModel
{
    #region Ctor

    public HomeSlideSearchModel()
    {
        AvailablePlacements = new List<SelectListItem>();
    }

    #endregion

    #region Properties

    [NopResourceDisplayName("admin.mahjar.homeslides.list.searchplacement")]
    public int? SearchPlacementId { get; set; }

    public IList<SelectListItem> AvailablePlacements { get; set; }

    /// <summary>The store home page (the "View store" link).</summary>
    public string StoreUrl { get; set; }

    #endregion
}

/// <summary>
/// Represents the home slide list model [Mahjar home-slides]
/// </summary>
public partial record HomeSlideListModel : BasePagedListModel<HomeSlideModel>
{
}

/// <summary>
/// Represents a home slide model: the create/edit form, and (lightened) one grid row. The link target is edited
/// through one input per kind (product id [+ open in rent mode], category / brand / partner dropdowns, search text,
/// url) so switching the kind never loses what was typed; the controller folds the chosen one into TargetId / TargetValue.
/// The schedule is edited in the admin's own time zone and stored in UTC. [Mahjar home-slides]
/// </summary>
public partial record HomeSlideModel : BaseNopEntityModel, ILocalizedModel<HomeSlideLocalizedModel>
{
    #region Ctor

    public HomeSlideModel()
    {
        Locales = new List<HomeSlideLocalizedModel>();
        AvailableLayouts = new List<SelectListItem>();
        AvailableContentPositions = new List<SelectListItem>();
        AvailablePlacements = new List<SelectListItem>();
        AvailableTargetTypes = new List<SelectListItem>();
        AvailableCategories = new List<SelectListItem>();
        AvailableManufacturers = new List<SelectListItem>();
        AvailableVendors = new List<SelectListItem>();
    }

    #endregion

    #region Properties

    //content (localized)
    [NopResourceDisplayName("admin.mahjar.homeslides.fields.kicker")]
    public string Kicker { get; set; }

    [NopResourceDisplayName("admin.mahjar.homeslides.fields.title")]
    public string Title { get; set; }

    [NopResourceDisplayName("admin.mahjar.homeslides.fields.subtitle")]
    public string Subtitle { get; set; }

    [NopResourceDisplayName("admin.mahjar.homeslides.fields.buttontext")]
    public string ButtonText { get; set; }

    public IList<HomeSlideLocalizedModel> Locales { get; set; }

    //media
    [UIHint("Picture")]
    [NopResourceDisplayName("admin.mahjar.homeslides.fields.picture")]
    public int PictureId { get; set; }

    [UIHint("Picture")]
    [NopResourceDisplayName("admin.mahjar.homeslides.fields.mobilepicture")]
    public int MobilePictureId { get; set; }

    [NopResourceDisplayName("admin.mahjar.homeslides.fields.videourl")]
    public string VideoUrl { get; set; }

    [NopResourceDisplayName("admin.mahjar.homeslides.fields.layout")]
    public int LayoutId { get; set; }

    [NopResourceDisplayName("admin.mahjar.homeslides.fields.contentposition")]
    public int ContentPositionId { get; set; }

    [NopResourceDisplayName("admin.mahjar.homeslides.fields.overlayopacity")]
    public int OverlayOpacity { get; set; }

    public IList<SelectListItem> AvailableLayouts { get; set; }

    public IList<SelectListItem> AvailableContentPositions { get; set; }

    //link
    [NopResourceDisplayName("admin.mahjar.homeslides.fields.targettype")]
    public int TargetTypeId { get; set; }

    [NopResourceDisplayName("admin.mahjar.homeslides.fields.targetproduct")]
    public int TargetProductId { get; set; }

    /// <summary>The linked product's name, echoed back next to its id (display only).</summary>
    public string TargetProductName { get; set; }

    /// <summary>
    /// Whether the product opens in rent mode (Rent already chosen), stored as TargetValue "rent" — meaningful only
    /// for a product that can be rented; any other opens as usual.
    /// </summary>
    [NopResourceDisplayName("admin.mahjar.homeslides.fields.targetrentmode")]
    public bool TargetProductRentMode { get; set; }

    /// <summary>Whether the linked product can be rented (display only: drives the rent-mode warning).</summary>
    public bool TargetProductIsRental { get; set; }

    [NopResourceDisplayName("admin.mahjar.homeslides.fields.targetcategory")]
    public int TargetCategoryId { get; set; }

    [NopResourceDisplayName("admin.mahjar.homeslides.fields.targetmanufacturer")]
    public int TargetManufacturerId { get; set; }

    [NopResourceDisplayName("admin.mahjar.homeslides.fields.targetvendor")]
    public int TargetVendorId { get; set; }

    [NopResourceDisplayName("admin.mahjar.homeslides.fields.targetsearch")]
    public string TargetSearch { get; set; }

    [NopResourceDisplayName("admin.mahjar.homeslides.fields.targeturl")]
    public string TargetUrl { get; set; }

    public IList<SelectListItem> AvailableTargetTypes { get; set; }

    public IList<SelectListItem> AvailableCategories { get; set; }

    public IList<SelectListItem> AvailableManufacturers { get; set; }

    public IList<SelectListItem> AvailableVendors { get; set; }

    //display
    [NopResourceDisplayName("admin.mahjar.homeslides.fields.placement")]
    public int PlacementId { get; set; }

    public IList<SelectListItem> AvailablePlacements { get; set; }

    [NopResourceDisplayName("admin.mahjar.homeslides.fields.showonweb")]
    public bool ShowOnWeb { get; set; }

    [NopResourceDisplayName("admin.mahjar.homeslides.fields.showonapp")]
    public bool ShowOnApp { get; set; }

    [NopResourceDisplayName("admin.mahjar.homeslides.fields.published")]
    public bool Published { get; set; }

    [NopResourceDisplayName("admin.mahjar.homeslides.fields.displayorder")]
    public int DisplayOrder { get; set; }

    [UIHint("DateTimeNullable")]
    [NopResourceDisplayName("admin.mahjar.homeslides.fields.startdate")]
    public DateTime? StartDate { get; set; }

    [UIHint("DateTimeNullable")]
    [NopResourceDisplayName("admin.mahjar.homeslides.fields.enddate")]
    public DateTime? EndDate { get; set; }

    //display-only (grid row, preview)
    public string PictureThumbnailUrl { get; set; }

    public string PicturePreviewUrl { get; set; }

    public string MobilePicturePreviewUrl { get; set; }

    public string PlacementName { get; set; }

    public string LayoutName { get; set; }

    public string TargetSummary { get; set; }

    /// <summary>"live" | "scheduled" | "ended" | "hidden" — drives the grid badge.</summary>
    public string StatusCode { get; set; }

    public string StatusName { get; set; }

    public string StoreUrl { get; set; }

    #endregion
}

/// <summary>
/// Represents the localized texts of a home slide (one per language) [Mahjar home-slides]
/// </summary>
public partial record HomeSlideLocalizedModel : ILocalizedLocaleModel
{
    public int LanguageId { get; set; }

    [NopResourceDisplayName("admin.mahjar.homeslides.fields.kicker")]
    public string Kicker { get; set; }

    [NopResourceDisplayName("admin.mahjar.homeslides.fields.title")]
    public string Title { get; set; }

    [NopResourceDisplayName("admin.mahjar.homeslides.fields.subtitle")]
    public string Subtitle { get; set; }

    [NopResourceDisplayName("admin.mahjar.homeslides.fields.buttontext")]
    public string ButtonText { get; set; }
}
