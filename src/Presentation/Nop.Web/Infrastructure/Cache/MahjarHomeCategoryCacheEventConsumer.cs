// [Mahjar home-motion]
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Core.Events;
using Nop.Services.Catalog;
using Nop.Services.Events;

namespace Nop.Web.Infrastructure.Cache;

/// <summary>
/// Clears the cached stand-in pictures of the home category tiles
/// (<see cref="NopModelCacheDefaults.MahjarHomeCategoryFallbackPicturesKey"/>) whenever the watch they come from can
/// change: a product (published, visibility, deleted), its category mapping, its picture list or a picture itself.
/// Category changes are already covered by the stock ModelCacheEventConsumer (the key shares the home category
/// prefix). Auto-discovered like every IConsumer. [Mahjar home-motion]
/// </summary>
public partial class MahjarHomeCategoryCacheEventConsumer :
    IConsumer<EntityInsertedEvent<Product>>,
    IConsumer<EntityUpdatedEvent<Product>>,
    IConsumer<EntityDeletedEvent<Product>>,
    IConsumer<EntityInsertedEvent<ProductCategory>>,
    IConsumer<EntityUpdatedEvent<ProductCategory>>,
    IConsumer<EntityDeletedEvent<ProductCategory>>,
    IConsumer<EntityInsertedEvent<ProductPicture>>,
    IConsumer<EntityUpdatedEvent<ProductPicture>>,
    IConsumer<EntityDeletedEvent<ProductPicture>>,
    IConsumer<EntityUpdatedEvent<Picture>>,
    IConsumer<EntityDeletedEvent<Picture>>
{
    #region Fields

    protected readonly IStaticCacheManager _staticCacheManager;

    #endregion

    #region Ctor

    public MahjarHomeCategoryCacheEventConsumer(IStaticCacheManager staticCacheManager)
    {
        _staticCacheManager = staticCacheManager;
    }

    #endregion

    #region Utilities

    /// <returns>A task that represents the asynchronous operation</returns>
    protected virtual async Task ClearAsync()
    {
        await _staticCacheManager.RemoveByPrefixAsync(NopModelCacheDefaults.MahjarHomeCategoryFallbackPrefixCacheKey);
    }

    /// <summary>
    /// A product change (published, deleted, ACL/visibility) also moves the per-category product numbers: the stock
    /// consumers clear that cache only on category and product-category changes, so the app's category cards (and the
    /// website's category navigation counts) would keep a stale count until it expires. [Mahjar app-home]
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    protected virtual async Task ClearWithProductNumbersAsync()
    {
        await ClearAsync();
        await _staticCacheManager.RemoveByPrefixAsync(NopCatalogDefaults.CategoryProductsNumberPrefix);
    }

    #endregion

    #region Methods

    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual Task HandleEventAsync(EntityInsertedEvent<Product> eventMessage) => ClearWithProductNumbersAsync(); //[Mahjar app-home]

    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual Task HandleEventAsync(EntityUpdatedEvent<Product> eventMessage) => ClearWithProductNumbersAsync(); //[Mahjar app-home]

    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual Task HandleEventAsync(EntityDeletedEvent<Product> eventMessage) => ClearWithProductNumbersAsync(); //[Mahjar app-home]

    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual Task HandleEventAsync(EntityInsertedEvent<ProductCategory> eventMessage) => ClearAsync();

    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual Task HandleEventAsync(EntityUpdatedEvent<ProductCategory> eventMessage) => ClearAsync();

    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual Task HandleEventAsync(EntityDeletedEvent<ProductCategory> eventMessage) => ClearAsync();

    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual Task HandleEventAsync(EntityInsertedEvent<ProductPicture> eventMessage) => ClearAsync();

    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual Task HandleEventAsync(EntityUpdatedEvent<ProductPicture> eventMessage) => ClearAsync();

    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual Task HandleEventAsync(EntityDeletedEvent<ProductPicture> eventMessage) => ClearAsync();

    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual Task HandleEventAsync(EntityUpdatedEvent<Picture> eventMessage) => ClearAsync();

    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual Task HandleEventAsync(EntityDeletedEvent<Picture> eventMessage) => ClearAsync();

    #endregion
}
