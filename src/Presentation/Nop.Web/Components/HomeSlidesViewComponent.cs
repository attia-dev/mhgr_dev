// [Mahjar home-motion]
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Mahjar;
using Nop.Core.Domain.Vendors;
using Nop.Services.Localization;
using Nop.Services.Mahjar.HomeSlides;
using Nop.Services.Media;
using Nop.Services.Seo;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Mvc.Routing;
using Nop.Web.Models.Catalog;

namespace Nop.Web.Components;

/// <summary>
/// The storefront "Living Home" slides (Content management → Home slides): the Hero carousel right under the
/// menu bar (view Default) or the full-bleed parallax Feature bands (view Feature). Only active web slides
/// (published, ShowOnWeb, inside their schedule, with a picture that still exists) are shown. A Hero with no
/// slide falls back to the HomepageText topic block (the view does it); a Feature placement with no slide
/// renders nothing. Target links are resolved here and only ever point at a live entity, the product search,
/// an absolute http(s) url or a site-relative path — anything else renders as "no link". [Mahjar home-motion]
/// </summary>
public partial class HomeSlidesViewComponent : NopViewComponent
{
    #region Constants

    /// <summary>Longest side (px) of a FullBleed picture — a full-width photo (uploads are capped at 1980px anyway)</summary>
    protected const int FULLBLEED_PICTURE_SIZE = 1920;

    /// <summary>Longest side (px) of a Spotlight picture — a product render shown as an object on the display plate</summary>
    protected const int SPOTLIGHT_PICTURE_SIZE = 1000;

    /// <summary>Longest side (px) of the ≤640px picture</summary>
    protected const int MOBILE_PICTURE_SIZE = 900;

    /// <summary>Longest side (px) of the ≤640px copy of a Spotlight render (it sits on a ~300px plate)</summary>
    protected const int MOBILE_SPOTLIGHT_PICTURE_SIZE = 640;

    #endregion

    #region Fields

    protected readonly IHomeSlideService _homeSlideService;
    protected readonly ILocalizationService _localizationService;
    protected readonly INopUrlHelper _nopUrlHelper;
    protected readonly IPictureService _pictureService;
    protected readonly IUrlRecordService _urlRecordService;
    protected readonly IWebHelper _webHelper;

    #endregion

    #region Ctor

    public HomeSlidesViewComponent(IHomeSlideService homeSlideService,
        ILocalizationService localizationService,
        INopUrlHelper nopUrlHelper,
        IPictureService pictureService,
        IUrlRecordService urlRecordService,
        IWebHelper webHelper)
    {
        _homeSlideService = homeSlideService;
        _localizationService = localizationService;
        _nopUrlHelper = nopUrlHelper;
        _pictureService = pictureService;
        _urlRecordService = urlRecordService;
        _webHelper = webHelper;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Returns the value when it is a safe link for an href/src (the shared HomeSlideLinkHelper rule: a site-relative
    /// "/…" path or an absolute http(s) url — never a protocol-relative "//host", the "/\host" quirk, or a value with a
    /// control character or backslash); otherwise null
    /// </summary>
    protected virtual string GetSafeUrl(string value, out bool isExternal)
    {
        isExternal = false;
        var link = HomeSlideLinkHelper.NormalizeLink(value, out var isRelative);
        if (link == null || isRelative)
            return link;

        //another host → the view opens it in a new tab with rel=noopener
        var linkHost = Uri.TryCreate(link, UriKind.Absolute, out var uri) ? uri.Host : string.Empty;
        var storeHost = Uri.TryCreate(_webHelper.GetStoreLocation(), UriKind.Absolute, out var storeUri) ? storeUri.Host : string.Empty;
        isExternal = !string.Equals(linkHost, storeHost, StringComparison.OrdinalIgnoreCase);

        return link;
    }

    /// <summary>
    /// Resolves the slide target to a storefront url. An entity target must still be open to the customer
    /// (IHomeSlideService.GetLiveTargetEntityAsync, shared with the mobile API), otherwise the slide simply links
    /// nowhere. A product slide in rent mode (TargetValue "rent") opens a rentable product with Rent already chosen
    /// (?mode=rent).
    /// </summary>
    protected virtual async Task<(string Url, bool IsExternal)> PrepareTargetUrlAsync(HomeSlide slide)
    {
        switch (slide.TargetType)
        {
            case HomeSlideTargetType.Product:
            case HomeSlideTargetType.Category:
            case HomeSlideTargetType.Manufacturer:
            case HomeSlideTargetType.Vendor:
                switch (await _homeSlideService.GetLiveTargetEntityAsync(slide))
                {
                    case Product product:
                        var productSeName = await _urlRecordService.GetSeNameAsync(product);
                        //[Mahjar home-slides] rent mode: the product page pre-selects Rent (Views/Product/_RentalInfo.cshtml)
                        if (slide.TargetValue == NopHomeSlideDefaults.RentModeTargetValue && product.IsRental)
                            return (await _nopUrlHelper.RouteGenericUrlAsync<Product>(new RouteValueDictionary
                            {
                                ["SeName"] = productSeName,
                                [NopHomeSlideDefaults.RentModeQueryName] = NopHomeSlideDefaults.RentModeTargetValue
                            }), false);
                        return (await _nopUrlHelper.RouteGenericUrlAsync<Product>(new { SeName = productSeName }), false);

                    case Category category:
                        return (await _nopUrlHelper.RouteGenericUrlAsync<Category>(new { SeName = await _urlRecordService.GetSeNameAsync(category) }), false);

                    case Manufacturer manufacturer:
                        return (await _nopUrlHelper.RouteGenericUrlAsync<Manufacturer>(new { SeName = await _urlRecordService.GetSeNameAsync(manufacturer) }), false);

                    case Vendor vendor:
                        return (await _nopUrlHelper.RouteGenericUrlAsync<Vendor>(new { SeName = await _urlRecordService.GetSeNameAsync(vendor) }), false);

                    default:
                        return (null, false);
                }

            case HomeSlideTargetType.Search:
                var query = slide.TargetValue?.Trim();
                return string.IsNullOrEmpty(query) ? (null, false) : (Url.RouteUrl("ProductSearch", new { q = query }), false);

            case HomeSlideTargetType.Url:
                var url = GetSafeUrl(slide.TargetValue, out var isExternal);
                return (url, isExternal);

            default:
                return (null, false);
        }
    }

    /// <summary>Gets a localized text with an empty value normalized to null (the view then omits the line)</summary>
    protected virtual async Task<string> GetLocalizedTextAsync(HomeSlide slide, Expression<Func<HomeSlide, string>> keySelector)
    {
        var text = await _localizationService.GetLocalizedAsync(slide, keySelector);
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    #endregion

    #region Methods

    /// <summary>Renders the slides of one placement (Hero → Default view, Feature → Feature view)</summary>
    /// <param name="placement">Hero (top carousel) or Feature (parallax bands)</param>
    public async Task<IViewComponentResult> InvokeAsync(HomeSlidePlacement placement)
    {
        var slides = await _homeSlideService.GetActiveHomeSlidesAsync(placement, HomeSlideChannel.Web);
        var isHero = placement == HomeSlidePlacement.Hero;

        var model = new HomeSlidesModel { Placement = isHero ? "hero" : "feature" };
        foreach (var slide in slides)
        {
            var isSpotlight = slide.Layout == HomeSlideLayout.Spotlight;

            //no placeholder ever: a slide whose picture is gone is skipped
            var imageUrl = await _pictureService.GetPictureUrlAsync(slide.PictureId,
                isSpotlight ? SPOTLIGHT_PICTURE_SIZE : FULLBLEED_PICTURE_SIZE, showDefaultPicture: false);
            if (string.IsNullOrEmpty(imageUrl))
                continue;

            var mobileImageUrl = slide.MobilePictureId > 0
                ? await _pictureService.GetPictureUrlAsync(slide.MobilePictureId, MOBILE_PICTURE_SIZE, showDefaultPicture: false)
                : null;
            if (string.IsNullOrEmpty(mobileImageUrl))
                mobileImageUrl = await _pictureService.GetPictureUrlAsync(slide.PictureId,
                    isSpotlight ? MOBILE_SPOTLIGHT_PICTURE_SIZE : MOBILE_PICTURE_SIZE, showDefaultPicture: false);

            var (url, isExternal) = await PrepareTargetUrlAsync(slide);

            model.Slides.Add(new HomeSlidesModel.HomeSlideItemModel
            {
                Id = slide.Id,
                ImageUrl = imageUrl,
                MobileImageUrl = string.IsNullOrEmpty(mobileImageUrl) ? imageUrl : mobileImageUrl,
                //a video is a FullBleed hero feature only (a Spotlight shows its picture as an object)
                VideoUrl = isHero && !isSpotlight ? GetSafeUrl(slide.VideoUrl, out _) : null,
                Kicker = await GetLocalizedTextAsync(slide, x => x.Kicker),
                Title = await GetLocalizedTextAsync(slide, x => x.Title) ?? string.Empty,
                Subtitle = await GetLocalizedTextAsync(slide, x => x.Subtitle),
                ButtonText = await GetLocalizedTextAsync(slide, x => x.ButtonText),
                Url = url,
                IsExternal = isExternal,
                Layout = isSpotlight ? "spotlight" : "fullbleed",
                ContentPosition = slide.ContentPosition switch
                {
                    HomeSlideContentPosition.Center => "center",
                    HomeSlideContentPosition.End => "end",
                    _ => "start"
                },
                OverlayOpacity = Math.Clamp(slide.OverlayOpacity, 0, 90)
            });
        }

        if (!isHero)
            return model.Slides.Any() ? View("Feature", model) : Content(string.Empty);

        //the Hero view renders the HomepageText topic block itself when there is no slide
        return View(model);
    }

    #endregion
}
