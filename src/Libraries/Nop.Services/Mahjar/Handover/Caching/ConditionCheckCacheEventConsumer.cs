// [Mahjar handover]
using Nop.Core.Domain.Mahjar;
using Nop.Services.Caching;

namespace Nop.Services.Mahjar.Handover.Caching;

/// <summary>
/// Represents a condition check cache event consumer ([Mahjar handover])
/// </summary>
public partial class ConditionCheckCacheEventConsumer : CacheEventConsumer<ConditionCheck>
{
}
