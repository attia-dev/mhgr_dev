// [Mahjar item-status]
using Nop.Core.Domain.Mahjar;
using Nop.Services.Caching;

namespace Nop.Services.Mahjar.ItemStatus.Caching;

/// <summary>
/// Represents a product item status note cache event consumer ([Mahjar item-status])
/// </summary>
public partial class ProductItemStatusNoteCacheEventConsumer : CacheEventConsumer<ProductItemStatusNote>
{
}
