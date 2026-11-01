// [Mahjar home-slides]
namespace Nop.Services.Mahjar.HomeSlides;

/// <summary>
/// The surface asking for the active home slides: it picks the <c>ShowOnWeb</c> or <c>ShowOnApp</c> flag.
/// [Mahjar home-slides]
/// </summary>
public enum HomeSlideChannel
{
    /// <summary>The web store (storefront home page).</summary>
    Web = 0,

    /// <summary>The mobile app (api/home/slides).</summary>
    App = 10
}
