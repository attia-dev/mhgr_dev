// [Mahjar home-slides]
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Mahjar;
using Nop.Services.Catalog;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Mahjar.HomeSlides;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Vendors;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Models.Mahjar;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Web.Areas.Admin.Controllers;

/// <summary>
/// Content management → Home slides: the admin-managed slides of the web home hero carousel, the web feature bands
/// and the mobile app home carousel (list, create, edit, delete). Guarded by the stock topic permissions, since
/// slides are home page content like the topics. A replaced or deleted slide picture is removed only when nothing
/// else uses it (seed slides share product pictures). [Mahjar home-slides]
/// </summary>
public partial class HomeSlideController : BaseAdminController
{
    #region Constants

    /// <summary>The live preview picture size (longest side, px).</summary>
    protected const int PREVIEW_SIZE = 900;

    /// <summary>The product search: shortest term and most suggestions (as the stock admin search).</summary>
    protected const int PRODUCT_SEARCH_MIN_LENGTH = 3;
    protected const int PRODUCT_SEARCH_SIZE = 15;

    #endregion

    #region Fields

    protected readonly ICategoryService _categoryService;
    protected readonly IDateTimeHelper _dateTimeHelper;
    protected readonly IHomeSlideModelFactory _homeSlideModelFactory;
    protected readonly IHomeSlideService _homeSlideService;
    protected readonly ILanguageService _languageService;
    protected readonly ILocalizationService _localizationService;
    protected readonly ILocalizedEntityService _localizedEntityService;
    protected readonly IManufacturerService _manufacturerService;
    protected readonly INotificationService _notificationService;
    protected readonly IPictureService _pictureService;
    protected readonly IProductService _productService;
    protected readonly IVendorService _vendorService;
    protected readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public HomeSlideController(ICategoryService categoryService,
        IDateTimeHelper dateTimeHelper,
        IHomeSlideModelFactory homeSlideModelFactory,
        IHomeSlideService homeSlideService,
        ILanguageService languageService,
        ILocalizationService localizationService,
        ILocalizedEntityService localizedEntityService,
        IManufacturerService manufacturerService,
        INotificationService notificationService,
        IPictureService pictureService,
        IProductService productService,
        IVendorService vendorService,
        IWorkContext workContext)
    {
        _categoryService = categoryService;
        _dateTimeHelper = dateTimeHelper;
        _homeSlideModelFactory = homeSlideModelFactory;
        _homeSlideService = homeSlideService;
        _languageService = languageService;
        _localizationService = localizationService;
        _localizedEntityService = localizedEntityService;
        _manufacturerService = manufacturerService;
        _notificationService = notificationService;
        _pictureService = pictureService;
        _productService = productService;
        _vendorService = vendorService;
        _workContext = workContext;
    }

    #endregion

    #region Utilities

    /// <summary>Trims a text; empty becomes null (an empty kicker / subtitle / button is simply not shown).</summary>
    protected static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Checks that the chosen link target still exists (the validator already required an id for an entity
    /// link; whether the id is real is a database lookup, so it runs here, before ModelState.IsValid)
    /// </summary>
    protected virtual async Task ValidateTargetExistsAsync(HomeSlideModel model)
    {
        var exists = true;
        string field = null;

        switch ((HomeSlideTargetType)model.TargetTypeId)
        {
            case HomeSlideTargetType.Product when model.TargetProductId > 0:
                var product = await _productService.GetProductByIdAsync(model.TargetProductId);
                exists = product is { Deleted: false };
                field = nameof(HomeSlideModel.TargetProductId);
                break;

            case HomeSlideTargetType.Category when model.TargetCategoryId > 0:
                var category = await _categoryService.GetCategoryByIdAsync(model.TargetCategoryId);
                exists = category is { Deleted: false };
                field = nameof(HomeSlideModel.TargetCategoryId);
                break;

            case HomeSlideTargetType.Manufacturer when model.TargetManufacturerId > 0:
                var manufacturer = await _manufacturerService.GetManufacturerByIdAsync(model.TargetManufacturerId);
                exists = manufacturer is { Deleted: false };
                field = nameof(HomeSlideModel.TargetManufacturerId);
                break;

            case HomeSlideTargetType.Vendor when model.TargetVendorId > 0:
                var vendor = await _vendorService.GetVendorByIdAsync(model.TargetVendorId);
                exists = vendor is { Deleted: false };
                field = nameof(HomeSlideModel.TargetVendorId);
                break;
        }

        if (!exists)
            ModelState.AddModelError(field, await _localizationService.GetResourceAsync("admin.mahjar.homeslides.fields.target.notfound"));
    }

    /// <summary>
    /// Checks the picture ids the form posts (plain hidden inputs written by the uploader). A picture the slide
    /// already has is kept as is; any other must exist and be used by nothing yet — a fresh upload. So a doctored
    /// post can never point a slide at someone else's picture (a product photo, an avatar, a logo, a condition
    /// photo), which the slide would later delete as "its own" when replaced or deleted. A rejected id is reset to
    /// the slide's own picture, so the redisplayed form never shows the foreign picture either.
    /// </summary>
    /// <param name="model">The posted form</param>
    /// <param name="slide">The slide being edited; null on create</param>
    protected virtual async Task ValidatePicturesAsync(HomeSlideModel model, HomeSlide slide)
    {
        var ownPictureIds = slide == null
            ? Array.Empty<int>()
            : new[] { slide.PictureId, slide.MobilePictureId }.Where(pictureId => pictureId > 0).ToArray();

        async Task<bool> isAcceptableAsync(int pictureId)
        {
            if (pictureId <= 0 || ownPictureIds.Contains(pictureId))
                return true;

            return await _pictureService.GetPictureByIdAsync(pictureId) != null
                && !await _homeSlideService.IsPictureInUseAsync(pictureId);
        }

        var message = await _localizationService.GetResourceAsync("admin.mahjar.homeslides.fields.picture.invalid");

        if (!await isAcceptableAsync(model.PictureId))
        {
            model.PictureId = slide?.PictureId ?? 0;
            ModelState.SetModelValue(nameof(HomeSlideModel.PictureId), model.PictureId, model.PictureId.ToString(CultureInfo.InvariantCulture));
            ModelState.AddModelError(nameof(HomeSlideModel.PictureId), message);
        }

        if (!await isAcceptableAsync(model.MobilePictureId))
        {
            model.MobilePictureId = slide?.MobilePictureId ?? 0;
            ModelState.SetModelValue(nameof(HomeSlideModel.MobilePictureId), model.MobilePictureId, model.MobilePictureId.ToString(CultureInfo.InvariantCulture));
            ModelState.AddModelError(nameof(HomeSlideModel.MobilePictureId), message);
        }
    }

    /// <summary>
    /// Copies the form onto the entity: the texts (trimmed), the media and display values, the link of the chosen
    /// kind folded into TargetId / TargetValue (every other kind's input is ignored; a product's "open in rent mode"
    /// is TargetValue "rent", else null), and the schedule converted from the admin's time zone to UTC
    /// </summary>
    protected virtual async Task ApplyModelAsync(HomeSlide slide, HomeSlideModel model)
    {
        slide.Kicker = Normalize(model.Kicker);
        slide.Title = model.Title?.Trim();
        slide.Subtitle = Normalize(model.Subtitle);
        slide.ButtonText = Normalize(model.ButtonText);

        slide.PictureId = model.PictureId;
        slide.MobilePictureId = Math.Max(model.MobilePictureId, 0);
        slide.VideoUrl = Normalize(model.VideoUrl);
        slide.LayoutId = model.LayoutId;
        slide.ContentPositionId = model.ContentPositionId;
        slide.OverlayOpacity = model.OverlayOpacity;

        slide.PlacementId = model.PlacementId;
        slide.ShowOnWeb = model.ShowOnWeb;
        slide.ShowOnApp = model.ShowOnApp;
        slide.Published = model.Published;
        slide.DisplayOrder = model.DisplayOrder;

        slide.TargetTypeId = model.TargetTypeId;
        (slide.TargetId, slide.TargetValue) = (HomeSlideTargetType)model.TargetTypeId switch
        {
            HomeSlideTargetType.Product => (model.TargetProductId, model.TargetProductRentMode ? NopHomeSlideDefaults.RentModeTargetValue : null),
            HomeSlideTargetType.Category => (model.TargetCategoryId, null),
            HomeSlideTargetType.Manufacturer => (model.TargetManufacturerId, null),
            HomeSlideTargetType.Vendor => (model.TargetVendorId, null),
            HomeSlideTargetType.Search => (0, Normalize(model.TargetSearch)),
            HomeSlideTargetType.Url => (0, Normalize(model.TargetUrl)),
            _ => (0, null)
        };

        var timeZone = await _dateTimeHelper.GetCurrentTimeZoneAsync();
        slide.StartDateUtc = model.StartDate.HasValue
            ? _dateTimeHelper.ConvertToUtcTime(model.StartDate.Value, timeZone)
            : null;
        slide.EndDateUtc = model.EndDate.HasValue
            ? _dateTimeHelper.ConvertToUtcTime(model.EndDate.Value, timeZone)
            : null;
    }

    /// <summary>Saves the per-language texts (an empty value removes that language's override).</summary>
    protected virtual async Task UpdateLocalesAsync(HomeSlide slide, HomeSlideModel model)
    {
        foreach (var localized in model.Locales)
        {
            await _localizedEntityService.SaveLocalizedValueAsync(slide, x => x.Kicker, Normalize(localized.Kicker), localized.LanguageId);
            await _localizedEntityService.SaveLocalizedValueAsync(slide, x => x.Title, Normalize(localized.Title), localized.LanguageId);
            await _localizedEntityService.SaveLocalizedValueAsync(slide, x => x.Subtitle, Normalize(localized.Subtitle), localized.LanguageId);
            await _localizedEntityService.SaveLocalizedValueAsync(slide, x => x.ButtonText, Normalize(localized.ButtonText), localized.LanguageId);
        }
    }

    #endregion

    #region List

    public virtual IActionResult Index()
    {
        return RedirectToAction("List");
    }

    [CheckPermission(StandardPermission.ContentManagement.TOPICS_VIEW)]
    public virtual async Task<IActionResult> List()
    {
        //prepare model
        var model = await _homeSlideModelFactory.PrepareHomeSlideSearchModelAsync(new HomeSlideSearchModel());

        return View(model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.ContentManagement.TOPICS_VIEW)]
    public virtual async Task<IActionResult> List(HomeSlideSearchModel searchModel)
    {
        //prepare model
        var model = await _homeSlideModelFactory.PrepareHomeSlideListModelAsync(searchModel);

        return Json(model);
    }

    #endregion

    #region Create / Edit / Delete

    [CheckPermission(StandardPermission.ContentManagement.TOPICS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> Create()
    {
        //prepare model
        var model = await _homeSlideModelFactory.PrepareHomeSlideModelAsync(new HomeSlideModel(), null);

        return View(model);
    }

    [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
    [FormValueRequired("save", "save-continue")]
    [CheckPermission(StandardPermission.ContentManagement.TOPICS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> Create(HomeSlideModel model, bool continueEditing)
    {
        await ValidatePicturesAsync(model, null);
        await ValidateTargetExistsAsync(model);

        if (ModelState.IsValid)
        {
            var slide = new HomeSlide();
            await ApplyModelAsync(slide, model);
            await _homeSlideService.InsertHomeSlideAsync(slide);

            //locales
            await UpdateLocalesAsync(slide, model);

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("admin.mahjar.homeslides.added"));

            if (!continueEditing)
                return RedirectToAction("List");

            return RedirectToAction("Edit", new { id = slide.Id });
        }

        //prepare model
        model = await _homeSlideModelFactory.PrepareHomeSlideModelAsync(model, null, true);

        //if we got this far, something failed, redisplay form
        return View(model);
    }

    [CheckPermission(StandardPermission.ContentManagement.TOPICS_VIEW)]
    public virtual async Task<IActionResult> Edit(int id)
    {
        //try to get a home slide with the specified id
        var slide = await _homeSlideService.GetHomeSlideByIdAsync(id);
        if (slide == null)
            return RedirectToAction("List");

        //prepare model
        var model = await _homeSlideModelFactory.PrepareHomeSlideModelAsync(null, slide);

        return View(model);
    }

    [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
    [FormValueRequired("save", "save-continue")]
    [CheckPermission(StandardPermission.ContentManagement.TOPICS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> Edit(HomeSlideModel model, bool continueEditing)
    {
        //try to get a home slide with the specified id
        var slide = await _homeSlideService.GetHomeSlideByIdAsync(model.Id);
        if (slide == null)
            return RedirectToAction("List");

        await ValidatePicturesAsync(model, slide);
        await ValidateTargetExistsAsync(model);

        if (ModelState.IsValid)
        {
            var previousPictureIds = new[] { slide.PictureId, slide.MobilePictureId };

            await ApplyModelAsync(slide, model);
            await _homeSlideService.UpdateHomeSlideAsync(slide);

            //locales
            await UpdateLocalesAsync(slide, model);

            //a replaced (or removed) picture goes too — but only when nothing else uses it: the seed slides show
            //product pictures, which must survive the slide getting a picture of its own
            foreach (var previousPictureId in previousPictureIds.Distinct())
            {
                if (previousPictureId > 0 && previousPictureId != slide.PictureId && previousPictureId != slide.MobilePictureId)
                    await _homeSlideService.DeletePictureIfUnusedAsync(previousPictureId);
            }

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("admin.mahjar.homeslides.updated"));

            if (!continueEditing)
                return RedirectToAction("List");

            return RedirectToAction("Edit", new { id = slide.Id });
        }

        //prepare model
        model = await _homeSlideModelFactory.PrepareHomeSlideModelAsync(model, slide, true);

        //if we got this far, something failed, redisplay form
        return View(model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.ContentManagement.TOPICS_CREATE_EDIT_DELETE)]
    public virtual async Task<IActionResult> Delete(int id)
    {
        //try to get a home slide with the specified id
        var slide = await _homeSlideService.GetHomeSlideByIdAsync(id);
        if (slide == null)
            return RedirectToAction("List");

        //its per-language texts first (an empty value removes the LocalizedProperty row)
        foreach (var language in await _languageService.GetAllLanguagesAsync(showHidden: true))
        {
            await _localizedEntityService.SaveLocalizedValueAsync(slide, x => x.Kicker, null, language.Id);
            await _localizedEntityService.SaveLocalizedValueAsync(slide, x => x.Title, null, language.Id);
            await _localizedEntityService.SaveLocalizedValueAsync(slide, x => x.Subtitle, null, language.Id);
            await _localizedEntityService.SaveLocalizedValueAsync(slide, x => x.ButtonText, null, language.Id);
        }

        //the slide, then its pictures when nothing else uses them
        await _homeSlideService.DeleteHomeSlideAsync(slide);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("admin.mahjar.homeslides.deleted"));

        return RedirectToAction("List");
    }

    #endregion

    #region Form helpers (product search and echo, live preview)

    /// <summary>
    /// The product autocomplete: matches the base (English) name and every language's localized name — the stock
    /// admin search only matches the base name, so an Arabic name found nothing — and labels each product in the
    /// admin's own language
    /// </summary>
    [CheckPermission(StandardPermission.ContentManagement.TOPICS_VIEW)]
    public virtual async Task<IActionResult> ProductSearch(string term)
    {
        term = term?.Trim();
        if (string.IsNullOrEmpty(term) || term.Length < PRODUCT_SEARCH_MIN_LENGTH)
            return Json(Array.Empty<object>());

        //one search per language (each also matches the base name), the admin's own language first
        var workingLanguageId = (await _workContext.GetWorkingLanguageAsync()).Id;
        var languageIds = (await _languageService.GetAllLanguagesAsync(showHidden: true))
            .Select(language => language.Id)
            .OrderBy(languageId => languageId == workingLanguageId ? 0 : 1)
            .ToList();

        var products = new List<Product>();
        foreach (var languageId in languageIds)
        {
            var found = await _productService.SearchProductsAsync(keywords: term, languageId: languageId,
                pageSize: PRODUCT_SEARCH_SIZE, showHidden: true);

            products.AddRange(found.Where(product => products.All(known => known.Id != product.Id)));
            if (products.Count >= PRODUCT_SEARCH_SIZE)
                break;
        }

        var unpublished = " " + await _localizationService.GetResourceAsync("admin.mahjar.homeslides.fields.targetproduct.unpublished");
        var result = await products.Take(PRODUCT_SEARCH_SIZE).SelectAwait(async product =>
        {
            var name = await _localizationService.GetLocalizedAsync(product, entity => entity.Name, workingLanguageId);

            return new
            {
                label = product.Published ? name : name + unpublished,
                productid = product.Id
            };
        }).ToListAsync();

        return Json(result);
    }

    /// <summary>
    /// The linked product's name for the product-id input (echoed as the admin types an id), and whether it can be
    /// rented (the "open in rent mode" warning)
    /// </summary>
    [CheckPermission(StandardPermission.ContentManagement.TOPICS_VIEW)]
    public virtual async Task<IActionResult> ProductInfo(int productId)
    {
        var product = productId > 0 ? await _productService.GetProductByIdAsync(productId) : null;
        if (product == null || product.Deleted)
            return Json(new { found = false });

        return Json(new
        {
            found = true,
            name = await _localizationService.GetLocalizedAsync(product, entity => entity.Name),
            published = product.Published,
            rental = product.IsRental
        });
    }

    /// <summary>
    /// A preview-sized url of a just-uploaded picture (the uploader itself only returns a 100px thumbnail). Only for
    /// a slide's picture or a fresh upload nothing uses yet — never a url of any other picture by its id (a
    /// condition photo, an avatar)
    /// </summary>
    [CheckPermission(StandardPermission.ContentManagement.TOPICS_VIEW)]
    public virtual async Task<IActionResult> PicturePreview(int pictureId)
    {
        if (pictureId <= 0)
            return Json(new { url = string.Empty });

        var slidePicture = (await _homeSlideService.SearchHomeSlidesAsync())
            .Any(slide => slide.PictureId == pictureId || slide.MobilePictureId == pictureId);
        if (!slidePicture && await _homeSlideService.IsPictureInUseAsync(pictureId))
            return Json(new { url = string.Empty });

        var url = await _pictureService.GetPictureUrlAsync(pictureId, PREVIEW_SIZE, showDefaultPicture: false);

        return Json(new { url });
    }

    #endregion
}
