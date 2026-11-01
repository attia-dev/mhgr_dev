// [Mahjar notifications]
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MHGR.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Mahjar;
using Nop.Services.Authentication;
using Nop.Services.Localization;
using Nop.Services.Mahjar.Notifications;

namespace Nop.Web.Controllers;

/// <summary>
/// In-app notifications API (bearer-token authenticated, buyer-only): paged feed, unread count, mark read /
/// read-all, and per-type preferences. Returns shared MHGR.Api.Contracts DTOs. [Mahjar notifications]
/// </summary>
[IgnoreAntiforgeryToken]
public partial class NotificationsApiController : Controller
{
    #region Fields

    protected readonly IApiTokenService _apiTokenService;
    protected readonly ILocalizationService _localizationService;
    protected readonly INotificationFeedService _feedService;
    protected readonly INotificationPreferenceService _preferenceService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public NotificationsApiController(IApiTokenService apiTokenService,
        ILocalizationService localizationService,
        INotificationFeedService feedService,
        INotificationPreferenceService preferenceService,
        IStoreContext storeContext,
        IWorkContext workContext)
    {
        _apiTokenService = apiTokenService;
        _localizationService = localizationService;
        _feedService = feedService;
        _preferenceService = preferenceService;
        _storeContext = storeContext;
        _workContext = workContext;
    }

    #endregion

    #region Utilities

    protected virtual async Task<Customer> GetTokenCustomerAsync()
    {
        return await _apiTokenService.GetAuthenticatedCustomerAsync(Request.Headers["Authorization"].ToString());
    }

    protected static NotificationDto ToDto(Notification n)
    {
        return new NotificationDto
        {
            Id = n.Id,
            Type = n.NotificationTypeId,
            Title = n.Title,
            Body = n.Body,
            IsRead = n.IsRead,
            CreatedOn = n.CreatedOnUtc.ToString("o"),
            TargetEntity = string.IsNullOrEmpty(n.EntityName) ? null : n.EntityName.ToLowerInvariant(),
            TargetId = n.EntityId > 0 ? n.EntityId : (int?)null
        };
    }

    protected virtual async Task<List<NotificationPreferenceDto>> BuildPreferencesAsync(int customerId, int languageId)
    {
        var saved = (await _preferenceService.GetPreferencesAsync(customerId))
            .ToDictionary(p => p.NotificationTypeId, p => p);

        var result = new List<NotificationPreferenceDto>();
        foreach (var type in NotificationTypeCatalog.BuyerTypes)
        {
            saved.TryGetValue((int)type, out var row);
            result.Add(new NotificationPreferenceDto
            {
                Type = (int)type,
                Title = await _localizationService.GetResourceAsync(NotificationTypeCatalog.TitleResourceKey(type), languageId),
                InApp = row?.InAppEnabled ?? true,
                Email = row?.EmailEnabled ?? true
            });
        }

        return result;
    }

    #endregion

    #region Methods

    // GET /api/notifications?page=&pageSize=
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<NotificationDto>), 200)]
    public virtual async Task<IActionResult> List(int page = 0, int pageSize = 20)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var items = await _feedService.GetNotificationsAsync(customer.Id, NotificationRecipientType.Buyer,
            0, store.Id, null, page, System.Math.Clamp(pageSize, 1, 100));

        return Json(new PagedResultDto<NotificationDto>
        {
            Page = items.PageIndex,
            PageSize = items.PageSize,
            TotalItems = items.TotalCount,
            TotalPages = items.TotalPages,
            Items = items.Select(ToDto).ToList()
        });
    }

    // GET /api/notifications/unread-count
    [HttpGet]
    public virtual async Task<IActionResult> UnreadCount()
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var count = await _feedService.GetUnreadCountAsync(customer.Id, NotificationRecipientType.Buyer, 0, store.Id);
        return Json(new { count });
    }

    // POST /api/notifications/{id}/read
    [HttpPost]
    [ProducesResponseType(typeof(MessageResponseDto), 200)]
    public virtual async Task<IActionResult> MarkRead(int id)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        await _feedService.MarkAsReadAsync(id, customer.Id);
        return Json(new MessageResponseDto { Success = true, Message = await _localizationService.GetResourceAsync("mahjar.api.notifications.markedread") });
    }

    // POST /api/notifications/read-all
    [HttpPost]
    [ProducesResponseType(typeof(MessageResponseDto), 200)]
    public virtual async Task<IActionResult> MarkAllRead()
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var store = await _storeContext.GetCurrentStoreAsync();
        await _feedService.MarkAllAsReadAsync(customer.Id, NotificationRecipientType.Buyer, 0, store.Id);
        return Json(new MessageResponseDto { Success = true, Message = await _localizationService.GetResourceAsync("mahjar.api.notifications.allmarkedread") });
    }

    // GET /api/notifications/preferences
    [HttpGet]
    [ProducesResponseType(typeof(ListResultDto<NotificationPreferenceDto>), 200)]
    public virtual async Task<IActionResult> GetPreferences()
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var language = await _workContext.GetWorkingLanguageAsync();
        return Json(new ListResultDto<NotificationPreferenceDto>
        {
            Items = await BuildPreferencesAsync(customer.Id, language.Id)
        });
    }

    // PUT /api/notifications/preferences
    [HttpPut]
    [ProducesResponseType(typeof(ListResultDto<NotificationPreferenceDto>), 200)]
    public virtual async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesApiRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });

        var buyerTypeIds = NotificationTypeCatalog.BuyerTypes.Select(t => (int)t).ToHashSet();
        foreach (var pref in request?.Preferences ?? new List<NotificationPreferenceDto>())
        {
            if (!buyerTypeIds.Contains(pref.Type))
                continue; //ignore non-buyer types from a tampered request
            await _preferenceService.SetPreferenceAsync(customer.Id, (NotificationType)pref.Type, pref.InApp, pref.Email);
        }

        var language = await _workContext.GetWorkingLanguageAsync();
        return Json(new ListResultDto<NotificationPreferenceDto>
        {
            Items = await BuildPreferencesAsync(customer.Id, language.Id)
        });
    }

    #endregion
}
