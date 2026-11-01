// [Mahjar notifications]
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Customers;
using Nop.Web.Factories;
using Nop.Web.Framework.Components;

namespace Nop.Web.Components;

/// <summary>
/// Storefront header notification bell + unread badge (logged-in buyers only). [Mahjar notifications]
/// </summary>
public partial class NotificationBellViewComponent : NopViewComponent
{
    protected readonly ICustomerService _customerService;
    protected readonly INotificationModelFactory _notificationModelFactory;
    protected readonly IWorkContext _workContext;

    public NotificationBellViewComponent(ICustomerService customerService,
        INotificationModelFactory notificationModelFactory,
        IWorkContext workContext)
    {
        _customerService = customerService;
        _notificationModelFactory = notificationModelFactory;
        _workContext = workContext;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        if (!await _customerService.IsRegisteredAsync(customer))
            return Content(string.Empty);

        var model = await _notificationModelFactory.PrepareBellModelAsync();
        return View(model);
    }
}
