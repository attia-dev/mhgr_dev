// [Mahjar notifications]
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Web.Infrastructure.Notifications;

/// <summary>
/// Maps the SignalR notification hub. Runs inside UseNopEndpoints (after UseAuthentication/UseRouting) so the
/// cookie principal is available to <see cref="NotificationHub"/> for web clients. [Mahjar notifications]
/// </summary>
public partial class NotificationHubRouteProvider : IRouteProvider
{
    /// <summary>
    /// Register routes
    /// </summary>
    /// <param name="endpointRouteBuilder">Route builder</param>
    public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapHub<NotificationHub>("/hubs/notifications");
    }

    /// <summary>
    /// Gets a priority of route provider
    /// </summary>
    public int Priority => 0;
}
