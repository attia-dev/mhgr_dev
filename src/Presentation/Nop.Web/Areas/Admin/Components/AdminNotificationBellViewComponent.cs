// [Mahjar notifications]
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Framework.Components;

namespace Nop.Web.Areas.Admin.Components;

/// <summary>
/// Admin/vendor header notification bell + unread badge. Shows for any dashboard user (admins and vendors);
/// the feed is recipient-scoped, so vendors only ever see their own. [Mahjar notifications]
/// </summary>
public partial class AdminNotificationBellViewComponent : NopViewComponent
{
    protected readonly INotificationModelFactory _notificationModelFactory;
    protected readonly IPermissionService _permissionService;

    public AdminNotificationBellViewComponent(INotificationModelFactory notificationModelFactory,
        IPermissionService permissionService)
    {
        _notificationModelFactory = notificationModelFactory;
        _permissionService = permissionService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermission.Security.ACCESS_ADMIN_PANEL))
            return Content(string.Empty);

        var model = await _notificationModelFactory.PrepareBellModelAsync();
        return View(model);
    }
}
