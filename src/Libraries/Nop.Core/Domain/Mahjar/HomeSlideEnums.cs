// [Mahjar home-slides]
namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// How a <see cref="HomeSlide"/> is composed. Values are explicit and gapped so new layouts can be slotted in
/// without renumbering stored rows. Labels come from the standard nopCommerce enum resources
/// (<c>enums.nop.core.domain.mahjar.homeslidelayout.*</c>). [Mahjar home-slides]
/// </summary>
public enum HomeSlideLayout
{
    /// <summary>The photo covers the whole slide; the text sits over a legibility gradient.</summary>
    FullBleed = 0,

    /// <summary>A brand backdrop with the picture shown as an object (a product render) on a display plate.</summary>
    Spotlight = 10
}

/// <summary>
/// Where the slide text block sits. Logical, not physical: <see cref="Start"/> is the right side in RTL
/// (Arabic) and the left side in LTR. [Mahjar home-slides]
/// </summary>
public enum HomeSlideContentPosition
{
    /// <summary>The reading-start side (right in RTL, left in LTR).</summary>
    Start = 0,

    /// <summary>Centered.</summary>
    Center = 10,

    /// <summary>The reading-end side (left in RTL, right in LTR).</summary>
    End = 20
}

/// <summary>
/// Where on the home page a slide is shown. [Mahjar home-slides]
/// </summary>
public enum HomeSlidePlacement
{
    /// <summary>The top carousel (web home page and the app home).</summary>
    Hero = 0,

    /// <summary>A full-bleed parallax band in the middle of the web home page (web only).</summary>
    Feature = 10
}

/// <summary>
/// What tapping/clicking a slide opens. <see cref="HomeSlide.TargetId"/> carries the entity id for the entity
/// kinds, <see cref="HomeSlide.TargetValue"/> the query (Search), link (Url) or "rent" (Product opened in rent
/// mode). [Mahjar home-slides]
/// </summary>
public enum HomeSlideTargetType
{
    /// <summary>The slide links nowhere.</summary>
    None = 0,

    /// <summary>A product page (TargetId = product id; TargetValue "rent" = opened with Rent already chosen, else null).</summary>
    Product = 10,

    /// <summary>A category page (TargetId = category id).</summary>
    Category = 20,

    /// <summary>A brand page (TargetId = manufacturer id).</summary>
    Manufacturer = 30,

    /// <summary>A partner page (TargetId = vendor id).</summary>
    Vendor = 40,

    /// <summary>A search results page (TargetValue = the search query).</summary>
    Search = 50,

    /// <summary>A link (TargetValue = an absolute http(s) url or a site-relative "/…" path).</summary>
    Url = 60
}
