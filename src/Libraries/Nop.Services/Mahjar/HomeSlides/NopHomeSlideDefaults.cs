// [Mahjar home-slides]
using Nop.Core.Caching;
using Nop.Core.Domain.Mahjar;

namespace Nop.Services.Mahjar.HomeSlides;

/// <summary>
/// Represents default values related to the home slides service. [Mahjar home-slides]
/// </summary>
public static partial class NopHomeSlideDefaults
{
    #region Link defaults

    /// <summary>
    /// The TargetValue of a Product slide that opens the product in rent mode (Rent already chosen): the web product
    /// page gets <c>?mode=rent</c>, the app <c>product?id=…&amp;mode=rent</c>. Any other value on a product slide means
    /// the default (buy) mode. The exact value; nothing else is ever stored or emitted for a product target.
    /// </summary>
    public const string RentModeTargetValue = "rent";

    /// <summary>The product page query flag that pre-selects Rent (web: <c>?mode=rent</c>).</summary>
    public const string RentModeQueryName = "mode";

    #endregion

    #region Caching defaults

    /// <summary>
    /// Gets a key for caching the whole slide table (every placement, channel and schedule — the per-request
    /// filters, including the time window, are applied in memory, so the entry never goes stale with the clock).
    /// Built on <see cref="NopEntityCacheDefaults{TEntity}.AllPrefix"/>, so the HomeSlide cache event consumer
    /// clears it on every insert / update / delete.
    /// </summary>
    public static CacheKey HomeSlidesAllCacheKey => new("Nop.homeslide.all.list", NopEntityCacheDefaults<HomeSlide>.AllPrefix);

    #endregion
}
