// [Mahjar home-slides]
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Mahjar;
using Nop.Services.Catalog;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Mahjar.HomeSlides;
using Nop.Services.Media;
using Nop.Services.Vendors;
using Nop.Web.Areas.Admin.Models.Mahjar;
using Nop.Web.Framework.Factories;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Web.Areas.Admin.Factories;

/// <summary>
/// Represents the home slide model factory implementation (admin area): the slides list, and the create/edit form
/// with its dropdowns, the echoed link target and the live preview pictures. [Mahjar home-slides]
/// </summary>
public partial class HomeSlideModelFactory : IHomeSlideModelFactory
{
    #region Constants

    /// <summary>The grid thumbnail size (longest side, px) — twice the 90px cell, so it stays sharp on HiDPI screens.</summary>
    protected const int THUMBNAIL_SIZE = 240;

    /// <summary>The live preview picture size (longest side, px).</summary>
    protected const int PREVIEW_SIZE = 900;

    #endregion

    #region Fields

    protected readonly ICategoryService _categoryService;
    protected readonly IDateTimeHelper _dateTimeHelper;
    protected readonly IHomeSlideService _homeSlideService;
    protected readonly ILocalizationService _localizationService;
    protected readonly ILocalizedModelFactory _localizedModelFactory;
    protected readonly IManufacturerService _manufacturerService;
    protected readonly IPictureService _pictureService;
    protected readonly IProductService _productService;
    protected readonly IVendorService _vendorService;
    protected readonly IWebHelper _webHelper;
    protected readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public HomeSlideModelFactory(ICategoryService categoryService,
        IDateTimeHelper dateTimeHelper,
        IHomeSlideService homeSlideService,
        ILocalizationService localizationService,
        ILocalizedModelFactory localizedModelFactory,
        IManufacturerService manufacturerService,
        IPictureService pictureService,
        IProductService productService,
        IVendorService vendorService,
        IWebHelper webHelper,
        IWorkContext workContext)
    {
        _categoryService = categoryService;
        _dateTimeHelper = dateTimeHelper;
        _homeSlideService = homeSlideService;
        _localizationService = localizationService;
        _localizedModelFactory = localizedModelFactory;
        _manufacturerService = manufacturerService;
        _pictureService = pictureService;
        _productService = productService;
        _vendorService = vendorService;
        _webHelper = webHelper;
        _workContext = workContext;
    }

    #endregion

    #region Utilities

    /// <summary>Builds a dropdown from an enum, labelled by its standard enum resources.</summary>
    protected virtual async Task<IList<SelectListItem>> PrepareEnumItemsAsync<TEnum>(int selectedValue) where TEnum : struct, Enum
    {
        var items = new List<SelectListItem>();
        foreach (var value in Enum.GetValues<TEnum>())
        {
            var id = Convert.ToInt32(value);
            items.Add(new SelectListItem
            {
                Value = id.ToString(),
                Text = await _localizationService.GetLocalizedEnumAsync(value),
                Selected = id == selectedValue
            });
        }

        return items;
    }

    /// <summary>The "— Choose —" first item of an entity dropdown (value 0 = nothing chosen).</summary>
    protected virtual async Task<SelectListItem> PrepareChooseItemAsync()
    {
        return new SelectListItem
        {
            Value = "0",
            Text = await _localizationService.GetResourceAsync("admin.mahjar.homeslides.fields.target.choose")
        };
    }

    /// <summary>
    /// Fills the link-target dropdowns: published categories (as breadcrumbs), published brands and active partners.
    /// A slide already pointing at a hidden one keeps it in its list (marked hidden), so opening the form never
    /// silently re-targets the slide.
    /// </summary>
    protected virtual async Task PrepareTargetListsAsync(HomeSlideModel model)
    {
        var hiddenSuffix = " " + await _localizationService.GetResourceAsync("admin.mahjar.homeslides.fields.target.hidden");

        //categories
        model.AvailableCategories.Clear();
        model.AvailableCategories.Add(await PrepareChooseItemAsync());
        //the names in the admin's own language, as the grid shows them
        var languageId = (await _workContext.GetWorkingLanguageAsync()).Id;
        var allCategories = await _categoryService.GetAllCategoriesAsync(showHidden: true);
        foreach (var category in allCategories)
        {
            if (!category.Published && category.Id != model.TargetCategoryId)
                continue;

            var text = await _categoryService.GetFormattedBreadCrumbAsync(category, allCategories, " » ", languageId);
            model.AvailableCategories.Add(new SelectListItem
            {
                Value = category.Id.ToString(),
                Text = category.Published ? text : text + hiddenSuffix
            });
        }

        //brands
        model.AvailableManufacturers.Clear();
        model.AvailableManufacturers.Add(await PrepareChooseItemAsync());
        foreach (var manufacturer in await _manufacturerService.GetAllManufacturersAsync(showHidden: true))
        {
            if (!manufacturer.Published && manufacturer.Id != model.TargetManufacturerId)
                continue;

            var name = await _localizationService.GetLocalizedAsync(manufacturer, entity => entity.Name);
            model.AvailableManufacturers.Add(new SelectListItem
            {
                Value = manufacturer.Id.ToString(),
                Text = manufacturer.Published ? name : name + hiddenSuffix
            });
        }

        //partners
        model.AvailableVendors.Clear();
        model.AvailableVendors.Add(await PrepareChooseItemAsync());
        foreach (var vendor in await _vendorService.GetAllVendorsAsync(showHidden: true))
        {
            if (!vendor.Active && vendor.Id != model.TargetVendorId)
                continue;

            var name = await _localizationService.GetLocalizedAsync(vendor, entity => entity.Name);
            model.AvailableVendors.Add(new SelectListItem
            {
                Value = vendor.Id.ToString(),
                Text = vendor.Active ? name : name + hiddenSuffix
            });
        }
    }

    /// <summary>The linked product's (localized) name, or null when the id points at nothing live.</summary>
    protected virtual async Task<string> GetProductNameAsync(int productId)
    {
        if (productId <= 0)
            return null;

        var product = await _productService.GetProductByIdAsync(productId);
        if (product == null || product.Deleted)
            return null;

        return await _localizationService.GetLocalizedAsync(product, entity => entity.Name);
    }

    /// <summary>
    /// A one-line "Link to: what" summary for the grid, e.g. "Product: Royal Moonphase", "Product: Skeleton Tourbillon
    /// (rent mode)" or "Search: diver".
    /// </summary>
    protected virtual async Task<string> PrepareTargetSummaryAsync(HomeSlide slide)
    {
        var kind = await _localizationService.GetLocalizedEnumAsync(slide.TargetType);
        string what = null;

        switch (slide.TargetType)
        {
            case HomeSlideTargetType.Product:
                what = await GetProductNameAsync(slide.TargetId);
                if (!string.IsNullOrWhiteSpace(what) && slide.TargetValue == NopHomeSlideDefaults.RentModeTargetValue)
                    what += " " + await _localizationService.GetResourceAsync("admin.mahjar.homeslides.fields.targetrentmode.tag");
                break;

            case HomeSlideTargetType.Category:
                var category = await _categoryService.GetCategoryByIdAsync(slide.TargetId);
                if (category != null && !category.Deleted)
                    what = await _localizationService.GetLocalizedAsync(category, entity => entity.Name);
                break;

            case HomeSlideTargetType.Manufacturer:
                var manufacturer = await _manufacturerService.GetManufacturerByIdAsync(slide.TargetId);
                if (manufacturer != null && !manufacturer.Deleted)
                    what = await _localizationService.GetLocalizedAsync(manufacturer, entity => entity.Name);
                break;

            case HomeSlideTargetType.Vendor:
                var vendor = await _vendorService.GetVendorByIdAsync(slide.TargetId);
                if (vendor != null && !vendor.Deleted)
                    what = await _localizationService.GetLocalizedAsync(vendor, entity => entity.Name);
                break;

            case HomeSlideTargetType.Search:
            case HomeSlideTargetType.Url:
                what = slide.TargetValue;
                break;

            default:
                return kind;
        }

        if (string.IsNullOrWhiteSpace(what))
            what = await _localizationService.GetResourceAsync("admin.mahjar.homeslides.fields.target.missing");

        return $"{kind}: {what}";
    }

    /// <summary>
    /// Where the slide stands right now, as the store sees it: hidden (unpublished, or on no channel its placement
    /// can use), scheduled (start still ahead), ended (end passed) or live.
    /// </summary>
    protected virtual string GetStatusCode(HomeSlide slide)
    {
        var nowUtc = DateTime.UtcNow;

        //a feature band is a web-only surface, so its app flag alone never makes it visible
        var onAnyChannel = slide.ShowOnWeb || (slide.ShowOnApp && slide.Placement == HomeSlidePlacement.Hero);
        if (!slide.Published || !onAnyChannel || slide.PictureId <= 0)
            return "hidden";

        if (slide.StartDateUtc.HasValue && slide.StartDateUtc.Value > nowUtc)
            return "scheduled";

        if (slide.EndDateUtc.HasValue && slide.EndDateUtc.Value < nowUtc)
            return "ended";

        return "live";
    }

    /// <summary>A picture url, or empty when the picture is gone (never the "no image" placeholder).</summary>
    protected virtual async Task<string> GetPictureUrlOrEmptyAsync(int pictureId, int size)
    {
        return pictureId > 0
            ? await _pictureService.GetPictureUrlAsync(pictureId, size, showDefaultPicture: false)
            : string.Empty;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Prepare home slide search model
    /// </summary>
    /// <param name="searchModel">Home slide search model</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the home slide search model
    /// </returns>
    public virtual async Task<HomeSlideSearchModel> PrepareHomeSlideSearchModelAsync(HomeSlideSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        searchModel.AvailablePlacements = await PrepareEnumItemsAsync<HomeSlidePlacement>(searchModel.SearchPlacementId ?? int.MinValue);
        searchModel.AvailablePlacements.Insert(0, new SelectListItem
        {
            Value = string.Empty,
            Text = await _localizationService.GetResourceAsync("admin.mahjar.homeslides.list.allplacements"),
            Selected = !searchModel.SearchPlacementId.HasValue
        });

        searchModel.StoreUrl = _webHelper.GetStoreLocation();
        searchModel.SetGridPageSize();

        return searchModel;
    }

    /// <summary>
    /// Prepare paged home slide list model
    /// </summary>
    /// <param name="searchModel">Home slide search model</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the home slide list model
    /// </returns>
    public virtual async Task<HomeSlideListModel> PrepareHomeSlideListModelAsync(HomeSlideSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        HomeSlidePlacement? placement = searchModel.SearchPlacementId.HasValue
            && Enum.IsDefined(typeof(HomeSlidePlacement), searchModel.SearchPlacementId.Value)
                ? (HomeSlidePlacement)searchModel.SearchPlacementId.Value
                : null;

        var slides = await _homeSlideService.SearchHomeSlidesAsync(placement, searchModel.Page - 1, searchModel.PageSize);

        //the rows stay light: no locales, dropdowns or preview pictures in the grid payload
        var model = await new HomeSlideListModel().PrepareToGridAsync(searchModel, slides, () =>
        {
            return slides.SelectAwait(async slide =>
            {
                var statusCode = GetStatusCode(slide);

                return new HomeSlideModel
                {
                    Id = slide.Id,
                    Kicker = await _localizationService.GetLocalizedAsync(slide, entity => entity.Kicker),
                    Title = await _localizationService.GetLocalizedAsync(slide, entity => entity.Title),
                    PictureThumbnailUrl = await GetPictureUrlOrEmptyAsync(slide.PictureId, THUMBNAIL_SIZE),
                    PlacementId = slide.PlacementId,
                    PlacementName = await _localizationService.GetLocalizedEnumAsync(slide.Placement),
                    LayoutId = slide.LayoutId,
                    LayoutName = await _localizationService.GetLocalizedEnumAsync(slide.Layout),
                    TargetSummary = await PrepareTargetSummaryAsync(slide),
                    ShowOnWeb = slide.ShowOnWeb,
                    //the effective value: a feature band never reaches the app, whatever its flag says
                    ShowOnApp = slide.ShowOnApp && slide.Placement == HomeSlidePlacement.Hero,
                    Published = slide.Published,
                    DisplayOrder = slide.DisplayOrder,
                    StatusCode = statusCode,
                    StatusName = await _localizationService.GetResourceAsync($"admin.mahjar.homeslides.status.{statusCode}")
                };
            });
        });

        return model;
    }

    /// <summary>
    /// Prepare home slide model
    /// </summary>
    /// <param name="model">Home slide model; null to build it from the entity</param>
    /// <param name="homeSlide">Home slide; null for a new slide</param>
    /// <param name="excludeProperties">Whether to keep the posted values (a redisplayed form) instead of the entity's</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the home slide model
    /// </returns>
    public virtual async Task<HomeSlideModel> PrepareHomeSlideModelAsync(HomeSlideModel model, HomeSlide homeSlide, bool excludeProperties = false)
    {
        Func<HomeSlideLocalizedModel, int, Task> localizedModelConfiguration = null;

        if (homeSlide != null)
        {
            //fill in model values from the entity
            if (model == null)
            {
                model = new HomeSlideModel
                {
                    Id = homeSlide.Id,
                    Kicker = homeSlide.Kicker,
                    Title = homeSlide.Title,
                    Subtitle = homeSlide.Subtitle,
                    ButtonText = homeSlide.ButtonText,
                    PictureId = homeSlide.PictureId,
                    MobilePictureId = homeSlide.MobilePictureId,
                    VideoUrl = homeSlide.VideoUrl,
                    LayoutId = homeSlide.LayoutId,
                    ContentPositionId = homeSlide.ContentPositionId,
                    OverlayOpacity = homeSlide.OverlayOpacity,
                    PlacementId = homeSlide.PlacementId,
                    TargetTypeId = homeSlide.TargetTypeId,
                    ShowOnWeb = homeSlide.ShowOnWeb,
                    ShowOnApp = homeSlide.ShowOnApp,
                    Published = homeSlide.Published,
                    DisplayOrder = homeSlide.DisplayOrder
                };

                //one input per link kind: only the active one carries the stored target
                switch (homeSlide.TargetType)
                {
                    case HomeSlideTargetType.Product:
                        model.TargetProductId = homeSlide.TargetId;
                        model.TargetProductRentMode = homeSlide.TargetValue == NopHomeSlideDefaults.RentModeTargetValue;
                        break;
                    case HomeSlideTargetType.Category:
                        model.TargetCategoryId = homeSlide.TargetId;
                        break;
                    case HomeSlideTargetType.Manufacturer:
                        model.TargetManufacturerId = homeSlide.TargetId;
                        break;
                    case HomeSlideTargetType.Vendor:
                        model.TargetVendorId = homeSlide.TargetId;
                        break;
                    case HomeSlideTargetType.Search:
                        model.TargetSearch = homeSlide.TargetValue;
                        break;
                    case HomeSlideTargetType.Url:
                        model.TargetUrl = homeSlide.TargetValue;
                        break;
                }

                //the schedule is edited in the admin's own time zone
                if (homeSlide.StartDateUtc.HasValue)
                    model.StartDate = await _dateTimeHelper.ConvertToUserTimeAsync(homeSlide.StartDateUtc.Value, DateTimeKind.Utc);
                if (homeSlide.EndDateUtc.HasValue)
                    model.EndDate = await _dateTimeHelper.ConvertToUserTimeAsync(homeSlide.EndDateUtc.Value, DateTimeKind.Utc);
            }

            //define localized model configuration action
            localizedModelConfiguration = async (locale, languageId) =>
            {
                locale.Kicker = await _localizationService.GetLocalizedAsync(homeSlide, entity => entity.Kicker, languageId, false, false);
                locale.Title = await _localizationService.GetLocalizedAsync(homeSlide, entity => entity.Title, languageId, false, false);
                locale.Subtitle = await _localizationService.GetLocalizedAsync(homeSlide, entity => entity.Subtitle, languageId, false, false);
                locale.ButtonText = await _localizationService.GetLocalizedAsync(homeSlide, entity => entity.ButtonText, languageId, false, false);
            };
        }

        model ??= new HomeSlideModel();

        //set default values for a new slide: the entity's own defaults, and next in line
        if (homeSlide == null && !excludeProperties)
        {
            var defaults = new HomeSlide();
            model.LayoutId = (int)HomeSlideLayout.FullBleed;
            model.ContentPositionId = (int)HomeSlideContentPosition.Start;
            model.PlacementId = (int)HomeSlidePlacement.Hero;
            model.OverlayOpacity = defaults.OverlayOpacity;
            model.ShowOnWeb = defaults.ShowOnWeb;
            model.ShowOnApp = defaults.ShowOnApp;
            model.Published = defaults.Published;

            var existing = await _homeSlideService.SearchHomeSlidesAsync(HomeSlidePlacement.Hero);
            model.DisplayOrder = existing.Any() ? existing.Max(slide => slide.DisplayOrder) + 1 : 1;
        }

        //prepare localized models
        if (!excludeProperties)
            model.Locales = await _localizedModelFactory.PrepareLocalizedModelsAsync(localizedModelConfiguration);

        //dropdowns
        model.AvailableLayouts = await PrepareEnumItemsAsync<HomeSlideLayout>(model.LayoutId);
        model.AvailableContentPositions = await PrepareEnumItemsAsync<HomeSlideContentPosition>(model.ContentPositionId);
        model.AvailablePlacements = await PrepareEnumItemsAsync<HomeSlidePlacement>(model.PlacementId);
        model.AvailableTargetTypes = await PrepareEnumItemsAsync<HomeSlideTargetType>(model.TargetTypeId);
        await PrepareTargetListsAsync(model);

        //echo the linked product (and whether it can be rented: the rent-mode warning), and seed the live preview
        model.TargetProductName = await GetProductNameAsync(model.TargetProductId);
        var targetProduct = model.TargetProductId > 0 ? await _productService.GetProductByIdAsync(model.TargetProductId) : null;
        model.TargetProductIsRental = targetProduct is { Deleted: false, IsRental: true };
        model.PicturePreviewUrl = await GetPictureUrlOrEmptyAsync(model.PictureId, PREVIEW_SIZE);
        model.MobilePicturePreviewUrl = await GetPictureUrlOrEmptyAsync(model.MobilePictureId, PREVIEW_SIZE);
        model.StoreUrl = _webHelper.GetStoreLocation();

        return model;
    }

    #endregion
}
