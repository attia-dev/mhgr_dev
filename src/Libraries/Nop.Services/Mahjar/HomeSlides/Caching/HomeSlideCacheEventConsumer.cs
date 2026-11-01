// [Mahjar home-slides]
using Nop.Core.Domain.Mahjar;
using Nop.Services.Caching;

namespace Nop.Services.Mahjar.HomeSlides.Caching;

/// <summary>
/// Represents a home slide cache event consumer: the base clears the HomeSlide entity caches, including
/// <see cref="NopHomeSlideDefaults.HomeSlidesAllCacheKey"/> (it lives under the AllPrefix). [Mahjar home-slides]
/// </summary>
public partial class HomeSlideCacheEventConsumer : CacheEventConsumer<HomeSlide>
{
}
