// [Mahjar notifications]
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Services.Mahjar.Notifications;

namespace Nop.Web.Infrastructure.Notifications;

/// <summary>
/// Registers SignalR and the real notification notifier. Order is 2100 — greater than the framework's
/// NopStartup (2000) — so this <see cref="SignalRNotificationNotifier"/> registration is the last one and
/// overrides the no-op default. The hub endpoint itself is mapped by
/// <see cref="NotificationHubRouteProvider"/>. [Mahjar notifications]
/// </summary>
public partial class NotificationsStartup : INopStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSignalR();
        services.AddScoped<INotificationRealtimeNotifier, SignalRNotificationNotifier>(); //overrides the Null default
    }

    public void Configure(IApplicationBuilder application)
    {
    }

    public int Order => 2100;
}
