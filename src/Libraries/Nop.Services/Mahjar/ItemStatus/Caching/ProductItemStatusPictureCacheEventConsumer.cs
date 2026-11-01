// [Mahjar item-status]
using Nop.Core.Domain.Mahjar;
using Nop.Services.Caching;

namespace Nop.Services.Mahjar.ItemStatus.Caching;

/// <summary>
/// Represents a product item status picture cache event consumer ([Mahjar item-status])
/// </summary>
public partial class ProductItemStatusPictureCacheEventConsumer : CacheEventConsumer<ProductItemStatusPicture>
{
}
