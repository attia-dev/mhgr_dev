// [Mahjar home-slides]
using System;
using System.Collections.Generic;
using System.Linq; //[Mahjar app-home]
using System.Linq.Expressions;
using System.Threading.Tasks;
using MHGR.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Mahjar;
using Nop.Services.Localization;
using Nop.Services.Mahjar.HomeSlides;
using Nop.Services.Media;
using Nop.Web.Factories; //[Mahjar app-home]

namespace Nop.Web.Controllers;

/// <summary>
/// The mobile app home: the admin-managed Hero carousel slides (Content management → Home slides). Anonymous.
/// Texts are localized per Accept-Language (the working language of an /api request), image and link urls are
/// ABSOLUTE off the request host. Returns shared MHGR.Api.Contracts DTOs. [Mahjar home-slides]
/// Also the home category cards (GET api/home/categories): the website's home categories. [Mahjar app-home]
/// </summary>
public partial class HomeApiController : Controller
{
    #region Constants

    /// <summary>The picture size the app gets (longest side, px) — sharp on a phone, light on the wire.</summary>
    protected const int SLIDE_PICTURE_SIZE = 1200;

    #endregion

    #region Fields

    protected readonly IHomeCategoryModelFactory _homeCategoryModelFactory; //[Mahjar app-home]
    protected readonly IHomeSlideService _homeSlideService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IPictureService _pictureService;
    protected readonly IWebHelper _webHelper;

    #endregion

    #region Ctor

    public HomeApiController(IHomeCategoryModelFactory homeCategoryModelFactory, //[Mahjar app-home]
        IHomeSlideService homeSlideService,
        ILocalizationService localizationService,
        IPictureService pictureService,
        IWebHelper webHelper)
    {
        _homeCategoryModelFactory = homeCategoryModelFactory; //[Mahjar app-home]
        _homeSlideService = homeSlideService;
        _localizationService = localizationService;
        _pictureService = pictureService;
        _webHelper = webHelper;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Gets the slide image url: the mobile picture when the slide has one, else the main picture; empty when
    /// neither picture exists any more (no placeholder is ever emitted)
    /// </summary>
    protected virtual async Task<string> GetSlideImageUrlAsync(HomeSlide slide)
    {
        var url = string.Empty;
        if (slide.MobilePictureId > 0)
            url = await _pictureService.GetPictureUrlAsync(slide.MobilePictureId, SLIDE_PICTURE_SIZE, showDefaultPicture: false);

        if (string.IsNullOrEmpty(url) && slide.PictureId > 0)
            url = await _pictureService.GetPictureUrlAsync(slide.PictureId, SLIDE_PICTURE_SIZE, showDefaultPicture: false);

        return url;
    }

    /// <summary>
    /// Maps the slide link to the DTO triple. An entity target is emitted only while a customer can still open it
    /// (IHomeSlideService.GetLiveTargetEntityAsync — the same rule the storefront applies), so a sold-and-unpublished
    /// or deleted watch never gets a button that opens an error page in the app; a search needs a query; a link is
    /// emitted only when HomeSlideLinkHelper accepts it (an absolute http(s) url, or a site-relative "/…" path made
    /// absolute off the request host). Anything else degrades to "no link" (TargetType null): the app then shows the
    /// slide without a button. A product carries TargetValue "rent" when the slide opens a rentable piece in rent mode
    /// (exactly that value, else null).
    /// </summary>
    protected virtual async Task<(string TargetType, int? TargetId, string TargetValue)> PrepareTargetAsync(HomeSlide slide)
    {
        switch (slide.TargetType)
        {
            case HomeSlideTargetType.Product:
            case HomeSlideTargetType.Category:
            case HomeSlideTargetType.Manufacturer:
            case HomeSlideTargetType.Vendor:
                var entity = await _homeSlideService.GetLiveTargetEntityAsync(slide);
                if (entity == null)
                    return (null, null, null);

                //[Mahjar home-slides] the app opens product?id=…&mode=rent for a "rent" slide of a rentable piece
                if (entity is Product product)
                    return ("product", product.Id,
                        slide.TargetValue == NopHomeSlideDefaults.RentModeTargetValue && product.IsRental
                            ? NopHomeSlideDefaults.RentModeTargetValue
                            : null);

                return (slide.TargetType.ToString().ToLowerInvariant(), entity.Id, null);

            case HomeSlideTargetType.Search:
                var query = slide.TargetValue?.Trim();
                return string.IsNullOrEmpty(query) ? (null, null, null) : ("search", null, query);

            case HomeSlideTargetType.Url:
                var link = HomeSlideLinkHelper.NormalizeLink(slide.TargetValue, out var isRelative);
                if (link == null)
                    return (null, null, null);

                return ("url", null, isRelative ? _webHelper.GetStoreLocation().TrimEnd('/') + link : link);

            default:
                return (null, null, null);
        }
    }

    /// <summary>Gets a localized text, with an empty value normalized to null (the app hides the line).</summary>
    protected virtual async Task<string> GetLocalizedTextAsync(HomeSlide slide, Expression<Func<HomeSlide, string>> keySelector)
    {
        var text = await _localizationService.GetLocalizedAsync(slide, keySelector);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    #endregion

    #region Methods

    // GET /api/home/slides
    [HttpGet]
    [ProducesResponseType(typeof(ListResultDto<HomeSlideDto>), 200)]
    public virtual async Task<IActionResult> Slides()
    {
        var slides = await _homeSlideService.GetActiveHomeSlidesAsync(HomeSlidePlacement.Hero, HomeSlideChannel.App);

        var items = new List<HomeSlideDto>();
        foreach (var slide in slides)
        {
            var imageUrl = await GetSlideImageUrlAsync(slide);
            if (string.IsNullOrEmpty(imageUrl))
                continue;

            var (targetType, targetId, targetValue) = await PrepareTargetAsync(slide);

            items.Add(new HomeSlideDto
            {
                Id = slide.Id,
                ImageUrl = imageUrl,
                Kicker = await GetLocalizedTextAsync(slide, x => x.Kicker),
                Title = await _localizationService.GetLocalizedAsync(slide, x => x.Title),
                Subtitle = await GetLocalizedTextAsync(slide, x => x.Subtitle),
                ButtonText = await GetLocalizedTextAsync(slide, x => x.ButtonText),
                Layout = Enum.IsDefined(slide.Layout) ? slide.Layout.ToString().ToLowerInvariant() : "fullbleed",
                ContentPosition = Enum.IsDefined(slide.ContentPosition) ? slide.ContentPosition.ToString().ToLowerInvariant() : "start",
                OverlayOpacity = Math.Clamp(slide.OverlayOpacity, 0, 90),
                TargetType = targetType,
                TargetId = targetId,
                TargetValue = targetValue
            });
        }

        return Json(new ListResultDto<HomeSlideDto> { Items = items });
    }

    /// <summary>
    /// GET /api/home/categories — the categories the website home page shows (published, "show on home page", ACL +
    /// store mapping, the same display order and the same pictures, incl. the stand-in watch of a category without a
    /// picture of its own), as app cards: localized name (Accept-Language), ABSOLUTE 600 px picture (null when there
    /// is none) and the product count (the website's rule; 0-count categories are returned too — the app hides them).
    /// Anonymous. Cached per store + roles + language + host. [Mahjar app-home]
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ListResultDto<HomeCategoryDto>), 200)]
    public virtual async Task<IActionResult> Categories()
    {
        var items = await _homeCategoryModelFactory.PrepareAppHomeCategoriesAsync();
        return Json(new ListResultDto<HomeCategoryDto> { Items = items.ToList() });
    }

    #endregion
}
