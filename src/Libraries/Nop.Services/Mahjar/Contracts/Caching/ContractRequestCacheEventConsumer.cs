using Nop.Core.Domain.Mahjar;
using Nop.Services.Caching;

namespace Nop.Services.Mahjar.Contracts.Caching;

/// <summary>
/// Represents a contract request cache event consumer ([Mahjar contracts])
/// </summary>
public partial class ContractRequestCacheEventConsumer : CacheEventConsumer<ContractRequest>
{
}
