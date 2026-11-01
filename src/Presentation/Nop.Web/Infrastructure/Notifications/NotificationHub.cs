// [Mahjar notifications]
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Services.Authentication;
using Nop.Services.Customers;
using Nop.Services.Security;

namespace Nop.Web.Infrastructure.Notifications;

/// <summary>
/// Real-time notification hub. Dual-authenticates the connecting client — JWT mobile clients pass the token as
/// the <c>?access_token=</c> query (a WebSocket handshake can't carry headers); web clients fall back to the
/// auth cookie. The client may request a specific group via <c>?group=</c>; the hub only joins it after
/// verifying the caller is entitled to it, otherwise it joins the caller's own <c>customer-{id}</c> group.
/// Groups: <c>customer-{id}</c> (buyer), <c>vendor-{vendorId}-{id}</c> (a vendor manager), <c>admin-{id}</c> (a
/// non-vendor dashboard user: an administrator or a handover employee) — one group per person, so a row and its
/// unread count only ever reach their recipient. [Mahjar notifications]
/// </summary>
public partial class NotificationHub : Hub
{
    #region Fields

    protected readonly IApiTokenService _apiTokenService;
    protected readonly ICustomerService _customerService;
    protected readonly IPermissionService _permissionService;
    protected readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public NotificationHub(IApiTokenService apiTokenService,
        ICustomerService customerService,
        IPermissionService permissionService,
        IWorkContext workContext)
    {
        _apiTokenService = apiTokenService;
        _customerService = customerService;
        _permissionService = permissionService;
        _workContext = workContext;
    }

    #endregion

    #region Utilities

    /// <summary>Resolve the connecting customer from the bearer token (mobile) or the auth cookie (web).</summary>
    protected virtual async Task<Customer> ResolveCallerAsync()
    {
        var httpContext = Context.GetHttpContext();

        //mobile JWT: the WebSocket transport carries it as ?access_token=, while the SSE/long-polling
        //fallbacks carry it in the Authorization header — accept either so live delivery works on any transport.
        var queryToken = httpContext?.Request.Query["access_token"].ToString();
        var authHeader = !string.IsNullOrEmpty(queryToken)
            ? "Bearer " + queryToken
            : httpContext?.Request.Headers["Authorization"].ToString();

        if (!string.IsNullOrEmpty(authHeader))
        {
            var byToken = await _apiTokenService.GetAuthenticatedCustomerAsync(authHeader);
            if (byToken != null)
                return byToken;
        }

        //web: auth cookie principal already on the HttpContext during the handshake
        return await _workContext.GetCurrentCustomerAsync();
    }

    /// <summary>Pick the group to join: the requested one if the caller is entitled, else customer-{id}.</summary>
    protected virtual async Task<string> ResolveGroupAsync(Customer customer, string requestedGroup)
    {
        var ownGroup = $"customer-{customer.Id}";

        if (string.IsNullOrEmpty(requestedGroup) || requestedGroup == ownGroup)
            return ownGroup;

        //[Mahjar handover] a dashboard user's own admin-{id} group ("admin" is what a page rendered before per-user
        //groups asks for): anybody with admin-panel access, which takes in the handover staff
        if (requestedGroup == "admin" || requestedGroup == $"admin-{customer.Id}")
            return await _permissionService.AuthorizeAsync(StandardPermission.Security.ACCESS_ADMIN_PANEL, customer)
                ? $"admin-{customer.Id}"
                : ownGroup;

        //[Mahjar handover] a vendor manager's own vendor-{vendorId}-{id} group ("vendor-{vendorId}" is what a page rendered
        //before per-user groups asks for)
        if (customer.VendorId > 0 &&
            (requestedGroup == $"vendor-{customer.VendorId}" || requestedGroup == $"vendor-{customer.VendorId}-{customer.Id}"))
            return $"vendor-{customer.VendorId}-{customer.Id}";

        //requested a group the caller is not entitled to → fall back to their own
        return ownGroup;
    }

    #endregion

    #region Methods

    public override async Task OnConnectedAsync()
    {
        var customer = await ResolveCallerAsync();
        if (customer == null || await _customerService.IsGuestAsync(customer))
        {
            Context.Abort();
            return;
        }

        var requestedGroup = Context.GetHttpContext()?.Request.Query["group"].ToString();
        var group = await ResolveGroupAsync(customer, requestedGroup);
        await Groups.AddToGroupAsync(Context.ConnectionId, group);

        await base.OnConnectedAsync();
    }

    #endregion
}
