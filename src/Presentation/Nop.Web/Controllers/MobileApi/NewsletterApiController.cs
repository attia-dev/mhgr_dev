using System;
using System.Threading.Tasks;
using MHGR.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Messages;
using Nop.Services.Localization;
using Nop.Services.Messages;

namespace Nop.Web.Controllers;

/// <summary>
/// Newsletter subscription API for the buyer app: subscribe an email to the store newsletter (idempotent —
/// re-subscribing an existing address just re-activates it). Returns shared MHGR.Api.Contracts DTOs. [Mahjar mobile-api]
/// </summary>
[IgnoreAntiforgeryToken]
public partial class NewsletterApiController : Controller
{
    #region Fields

    protected readonly ILocalizationService _localizationService; // [Mahjar i18n]
    protected readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
    protected readonly IStoreContext _storeContext;

    #endregion

    #region Ctor

    public NewsletterApiController(ILocalizationService localizationService,
        INewsLetterSubscriptionService newsLetterSubscriptionService,
        IStoreContext storeContext)
    {
        _localizationService = localizationService;
        _newsLetterSubscriptionService = newsLetterSubscriptionService;
        _storeContext = storeContext;
    }

    #endregion

    #region Methods

    // POST /api/newsletter/subscribe   { email }
    [HttpPost]
    [ProducesResponseType(typeof(MessageResponseDto), 200)]
    public virtual async Task<IActionResult> Subscribe([FromBody] NewsletterApiRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.emailrequired") });

        var email = request.Email.Trim();
        var store = await _storeContext.GetCurrentStoreAsync();

        var existing = await _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreIdAsync(email, store.Id);
        if (existing == null)
        {
            await _newsLetterSubscriptionService.InsertNewsLetterSubscriptionAsync(new NewsLetterSubscription
            {
                NewsLetterSubscriptionGuid = Guid.NewGuid(),
                Email = email,
                Active = true,
                StoreId = store.Id,
                CreatedOnUtc = DateTime.UtcNow
            });
        }

        return Json(new MessageResponseDto { Success = true, Message = await _localizationService.GetResourceAsync("mahjar.api.newsletter.subscribed") });
    }

    #endregion
}
