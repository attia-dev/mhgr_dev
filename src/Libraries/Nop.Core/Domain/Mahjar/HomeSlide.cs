// [Mahjar home-slides]
using Nop.Core.Domain.Localization;

namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// An admin-managed home page slide: a Hero carousel slide (web + app) or a Feature band (web only). The four
/// texts (<see cref="Kicker"/>, <see cref="Title"/>, <see cref="Subtitle"/>, <see cref="ButtonText"/>) are
/// localized via <see cref="LocalizedProperty"/> (LocaleKeyGroup "HomeSlide"). The pictures are plain stock
/// <c>Picture</c> ids with no foreign key, so 0 (none) is storable and a seed slide can share a product's
/// picture. A fresh <c>new HomeSlide()</c> carries the contract defaults (OverlayOpacity 45, shown on web and app,
/// published); LinqToDB overwrites them when it materializes a stored row. [Mahjar home-slides]
/// </summary>
public partial class HomeSlide : BaseEntity, ILocalizedEntity
{
    /// <summary>The main (wide) picture identifier — web hero/band; recommended 2400×1050 JPG.</summary>
    public int PictureId { get; set; }

    /// <summary>The optional mobile picture identifier (0 = none) — used by the app and web ≤ 640px; recommended 1200×800.</summary>
    public int MobilePictureId { get; set; }

    /// <summary>An optional MP4 url, web hero only (muted, looped, the picture is the poster).</summary>
    public string VideoUrl { get; set; }

    /// <summary>The small eyebrow line above the title (localized).</summary>
    public string Kicker { get; set; }

    /// <summary>The slide title (localized).</summary>
    public string Title { get; set; }

    /// <summary>The line under the title (localized).</summary>
    public string Subtitle { get; set; }

    /// <summary>The call-to-action button text (localized); empty = no button (the slide is still clickable when it has a target).</summary>
    public string ButtonText { get; set; }

    /// <summary>The layout (backing value for <see cref="Layout"/>).</summary>
    public int LayoutId { get; set; }

    /// <summary>The text block position (backing value for <see cref="ContentPosition"/>).</summary>
    public int ContentPositionId { get; set; }

    /// <summary>The legibility gradient darkness, 0..90 (FullBleed layout); defaults to 45.</summary>
    public int OverlayOpacity { get; set; } = 45;

    /// <summary>The placement on the home page (backing value for <see cref="Placement"/>).</summary>
    public int PlacementId { get; set; }

    /// <summary>The link kind (backing value for <see cref="TargetType"/>).</summary>
    public int TargetTypeId { get; set; }

    /// <summary>The target entity id for Product / Category / Manufacturer / Vendor, else 0.</summary>
    public int TargetId { get; set; }

    /// <summary>The search query (Search), link (Url), "rent" for a product opened in rent mode (Product), else null.</summary>
    public string TargetValue { get; set; }

    /// <summary>Whether the slide is shown on the web store (default true).</summary>
    public bool ShowOnWeb { get; set; } = true;

    /// <summary>Whether the slide is shown in the mobile app (default true).</summary>
    public bool ShowOnApp { get; set; } = true;

    /// <summary>Whether the slide is published (default true).</summary>
    public bool Published { get; set; } = true;

    /// <summary>The display order (ascending).</summary>
    public int DisplayOrder { get; set; }

    /// <summary>The start of the schedule window (UTC); null = open.</summary>
    public DateTime? StartDateUtc { get; set; }

    /// <summary>The end of the schedule window (UTC); null = open.</summary>
    public DateTime? EndDateUtc { get; set; }

    /// <summary>Creation date (UTC).</summary>
    public DateTime CreatedOnUtc { get; set; }

    /// <summary>Last update date (UTC).</summary>
    public DateTime UpdatedOnUtc { get; set; }

    /// <summary>Typed wrapper over <see cref="LayoutId"/>.</summary>
    public HomeSlideLayout Layout
    {
        get => (HomeSlideLayout)LayoutId;
        set => LayoutId = (int)value;
    }

    /// <summary>Typed wrapper over <see cref="ContentPositionId"/>.</summary>
    public HomeSlideContentPosition ContentPosition
    {
        get => (HomeSlideContentPosition)ContentPositionId;
        set => ContentPositionId = (int)value;
    }

    /// <summary>Typed wrapper over <see cref="PlacementId"/>.</summary>
    public HomeSlidePlacement Placement
    {
        get => (HomeSlidePlacement)PlacementId;
        set => PlacementId = (int)value;
    }

    /// <summary>Typed wrapper over <see cref="TargetTypeId"/>.</summary>
    public HomeSlideTargetType TargetType
    {
        get => (HomeSlideTargetType)TargetTypeId;
        set => TargetTypeId = (int)value;
    }
}
