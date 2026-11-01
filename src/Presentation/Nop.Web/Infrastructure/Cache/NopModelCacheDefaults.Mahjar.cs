// [Mahjar home-motion]
using Nop.Core.Caching;

namespace Nop.Web.Infrastructure.Cache;

public static partial class NopModelCacheDefaults
{
    /// <summary>
    /// Key for the stand-in pictures of the home category tiles (category id → picture url of its first visible watch,
    /// for every home category without a picture of its own). It starts with <see cref="CategoryHomepagePrefixCacheKey"/>,
    /// so the stock consumer clears it with the home category models on any category change; the Mahjar consumer
    /// (MahjarHomeCategoryCacheEventConsumer) clears <see cref="MahjarHomeCategoryFallbackPrefixCacheKey"/> on product,
    /// product-category, product-picture and picture changes. [Mahjar home-motion]
    /// </summary>
    /// <remarks>
    /// {0} : current store ID
    /// {1} : roles of the current user
    /// {2} : picture size
    /// {3} : language ID
    /// {4} : store location, i.e. scheme + request host [Mahjar app-home] (the picture urls are absolute off the
    ///       request host: the app on the emulator's 10.0.2.2 must never get a url cached for a "localhost" browser)
    /// </remarks>
    public static CacheKey MahjarHomeCategoryFallbackPicturesKey => new("Nop.pres.category.homepage.mahjar-fallback-{0}-{1}-{2}-{3}-{4}",
        CategoryHomepagePrefixCacheKey, MahjarHomeCategoryFallbackPrefixCacheKey);
    public static string MahjarHomeCategoryFallbackPrefixCacheKey => "Nop.pres.category.homepage.mahjar-fallback";

    /// <summary>
    /// Key for the app's home category cards (GET api/home/categories): names, 600 px pictures and product counts.
    /// It starts with both <see cref="CategoryHomepagePrefixCacheKey"/> and
    /// <see cref="MahjarHomeCategoryFallbackPrefixCacheKey"/>, so a category change (stock consumer) or a product,
    /// product-category, product-picture or picture change (MahjarHomeCategoryCacheEventConsumer) clears it at once.
    /// [Mahjar app-home]
    /// </summary>
    /// <remarks>
    /// {0} : current store ID
    /// {1} : roles of the current user
    /// {2} : language ID
    /// {3} : store location (the picture urls are absolute off the request host)
    /// </remarks>
    public static CacheKey MahjarAppHomeCategoriesKey => new("Nop.pres.category.homepage.mahjar-fallback.app-{0}-{1}-{2}-{3}",
        CategoryHomepagePrefixCacheKey, MahjarHomeCategoryFallbackPrefixCacheKey);
}
