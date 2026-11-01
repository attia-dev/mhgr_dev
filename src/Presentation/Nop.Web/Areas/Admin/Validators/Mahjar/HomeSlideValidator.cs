// [Mahjar home-slides]
using FluentValidation;
using Nop.Core.Domain.Mahjar;
using Nop.Services.Localization;
using Nop.Services.Mahjar.HomeSlides;
using Nop.Web.Areas.Admin.Models.Mahjar;
using Nop.Web.Framework.Validators;

namespace Nop.Web.Areas.Admin.Validators.Mahjar;

/// <summary>
/// Validates the home slide form: a title (standard tab) and a main picture are required, the overlay darkness
/// stays within 0..90, the schedule ends after it starts, an entity link names its target, and every link the
/// slide will render (the target url, the video) is an http(s) url or a site-relative "/…" path — never
/// javascript:, data: or a protocol-relative "//host". Whether a typed product id exists is an async lookup, so
/// the controller checks it, as it checks that a new picture is a fresh upload. A button needs a link (the website
/// hides a button without one; the app would show a dead one). Auto-registered (AddValidatorsFromAssemblies).
/// [Mahjar home-slides]
/// </summary>
public partial class HomeSlideValidator : BaseNopValidator<HomeSlideModel>
{
    #region Constants

    /// <summary>The overlay darkness range (the storefront gradient is unreadable beyond 90).</summary>
    public const int OVERLAY_MIN = 0;
    public const int OVERLAY_MAX = 90;

    /// <summary>The TargetValue column length (a search query or a link).</summary>
    public const int TARGET_VALUE_MAX_LENGTH = 1000;

    #endregion

    #region Ctor

    public HomeSlideValidator(ILocalizationService localizationService)
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessageAwait(localizationService.GetResourceAsync("admin.mahjar.homeslides.fields.title.required"));

        RuleFor(x => x.PictureId)
            .GreaterThan(0)
            .WithMessageAwait(localizationService.GetResourceAsync("admin.mahjar.homeslides.fields.picture.required"));

        RuleFor(x => x.OverlayOpacity)
            .InclusiveBetween(OVERLAY_MIN, OVERLAY_MAX)
            .WithMessageAwait(localizationService.GetResourceAsync("admin.mahjar.homeslides.fields.overlayopacity.range"));

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessageAwait(localizationService.GetResourceAsync("admin.mahjar.homeslides.fields.enddate.beforestart"));

        RuleFor(x => x.VideoUrl)
            .Must(IsSafeLink)
            .When(x => !string.IsNullOrWhiteSpace(x.VideoUrl))
            .WithMessageAwait(localizationService.GetResourceAsync("admin.mahjar.homeslides.fields.videourl.invalid"));

        //the enum values themselves (a forged post must not store an unknown layout / position / placement / kind)
        RuleFor(x => x.LayoutId).Must(value => Enum.IsDefined(typeof(HomeSlideLayout), value));
        RuleFor(x => x.ContentPositionId).Must(value => Enum.IsDefined(typeof(HomeSlideContentPosition), value));
        RuleFor(x => x.PlacementId).Must(value => Enum.IsDefined(typeof(HomeSlidePlacement), value));
        RuleFor(x => x.TargetTypeId).Must(value => Enum.IsDefined(typeof(HomeSlideTargetType), value));

        //a button needs somewhere to go: the website hides a button without a link and the app would show a dead one
        RuleFor(x => x.TargetTypeId)
            .NotEqual((int)HomeSlideTargetType.None)
            .When(HasButtonText)
            .WithMessageAwait(localizationService.GetResourceAsync("admin.mahjar.homeslides.fields.buttontext.needslink"));

        //the link target of the chosen kind
        RuleFor(x => x.TargetProductId)
            .GreaterThan(0)
            .When(x => x.TargetTypeId == (int)HomeSlideTargetType.Product)
            .WithMessageAwait(localizationService.GetResourceAsync("admin.mahjar.homeslides.fields.targetproduct.required"));

        RuleFor(x => x.TargetCategoryId)
            .GreaterThan(0)
            .When(x => x.TargetTypeId == (int)HomeSlideTargetType.Category)
            .WithMessageAwait(localizationService.GetResourceAsync("admin.mahjar.homeslides.fields.targetcategory.required"));

        RuleFor(x => x.TargetManufacturerId)
            .GreaterThan(0)
            .When(x => x.TargetTypeId == (int)HomeSlideTargetType.Manufacturer)
            .WithMessageAwait(localizationService.GetResourceAsync("admin.mahjar.homeslides.fields.targetmanufacturer.required"));

        RuleFor(x => x.TargetVendorId)
            .GreaterThan(0)
            .When(x => x.TargetTypeId == (int)HomeSlideTargetType.Vendor)
            .WithMessageAwait(localizationService.GetResourceAsync("admin.mahjar.homeslides.fields.targetvendor.required"));

        RuleFor(x => x.TargetSearch)
            .NotEmpty()
            .When(x => x.TargetTypeId == (int)HomeSlideTargetType.Search)
            .WithMessageAwait(localizationService.GetResourceAsync("admin.mahjar.homeslides.fields.targetsearch.required"));

        RuleFor(x => x.TargetSearch)
            .MaximumLength(TARGET_VALUE_MAX_LENGTH)
            .When(x => x.TargetTypeId == (int)HomeSlideTargetType.Search);

        RuleFor(x => x.TargetUrl)
            .Must(IsSafeLink)
            .When(x => x.TargetTypeId == (int)HomeSlideTargetType.Url)
            .WithMessageAwait(localizationService.GetResourceAsync("admin.mahjar.homeslides.fields.targeturl.invalid"));

        RuleFor(x => x.TargetUrl)
            .MaximumLength(TARGET_VALUE_MAX_LENGTH)
            .When(x => x.TargetTypeId == (int)HomeSlideTargetType.Url);

        //the localized texts share the base column lengths
        RuleForEach(x => x.Locales).ChildRules(locale =>
        {
            locale.RuleFor(l => l.Kicker).MaximumLength(150);
            locale.RuleFor(l => l.Title).MaximumLength(250);
            locale.RuleFor(l => l.Subtitle).MaximumLength(600);
            locale.RuleFor(l => l.ButtonText).MaximumLength(100);
        });

        //string lengths of Kicker / Title / Subtitle / ButtonText / VideoUrl, from the table
        SetDatabaseValidationRules<HomeSlide>();
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Gets a value indicating whether the slide has a button in any language (the Standard tab or a language tab)
    /// </summary>
    /// <param name="model">The posted form</param>
    public static bool HasButtonText(HomeSlideModel model)
    {
        return !string.IsNullOrWhiteSpace(model.ButtonText)
            || (model.Locales?.Any(locale => !string.IsNullOrWhiteSpace(locale.ButtonText)) ?? false);
    }

    /// <summary>
    /// Gets a value indicating whether a link is safe to render: an absolute http(s) url, or a site-relative path
    /// ("/…", but never the protocol-relative "//host", the "/\host" browser quirk, nor a value with a control
    /// character or backslash — a pasted TAB would otherwise turn "/&lt;TAB&gt;/host" into "//host"). The very rule
    /// the storefront and the mobile API apply before they emit a link (HomeSlideLinkHelper).
    /// </summary>
    /// <param name="link">The link as typed</param>
    public static bool IsSafeLink(string link)
    {
        return HomeSlideLinkHelper.IsSafeLink(link);
    }

    #endregion
}
