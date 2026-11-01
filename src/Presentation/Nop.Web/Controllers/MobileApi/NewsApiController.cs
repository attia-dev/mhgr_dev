using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MHGR.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.News;
using Nop.Services.Authentication;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.News;

namespace Nop.Web.Controllers;

/// <summary>
/// Read + comment News API for the buyer app: list news items, open an item with its approved comments, and (when
/// signed in) add a comment. Returns shared MHGR.Api.Contracts DTOs. [Mahjar mobile-api]
/// </summary>
[IgnoreAntiforgeryToken]
public partial class NewsApiController : Controller
{
    #region Fields

    protected readonly IApiTokenService _apiTokenService;
    protected readonly ICustomerService _customerService;
    protected readonly ILocalizationService _localizationService; // [Mahjar i18n]
    protected readonly INewsService _newsService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;
    protected readonly NewsSettings _newsSettings;

    #endregion

    #region Ctor

    public NewsApiController(IApiTokenService apiTokenService,
        ICustomerService customerService,
        ILocalizationService localizationService,
        INewsService newsService,
        IStoreContext storeContext,
        IWorkContext workContext,
        NewsSettings newsSettings)
    {
        _apiTokenService = apiTokenService;
        _customerService = customerService;
        _localizationService = localizationService;
        _newsService = newsService;
        _storeContext = storeContext;
        _workContext = workContext;
        _newsSettings = newsSettings;
    }

    #endregion

    #region Utilities

    protected virtual async Task<Customer> GetTokenCustomerAsync()
    {
        return await _apiTokenService.GetAuthenticatedCustomerAsync(Request.Headers["Authorization"].ToString());
    }

    protected virtual async Task<string> CustomerNameAsync(int customerId)
    {
        var c = await _customerService.GetCustomerByIdAsync(customerId);
        return c == null || string.IsNullOrWhiteSpace(c.FirstName)
            ? await _localizationService.GetResourceAsync("mahjar.api.common.customerfallback")
            : $"{c.FirstName} {c.LastName}".Trim();
    }

    #endregion

    #region Methods

    // GET /api/news?page=&pageSize=
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<NewsItemSummaryDto>), 200)]
    public virtual async Task<IActionResult> List(int page = 0, int pageSize = 20)
    {
        if (!_newsSettings.Enabled)
            return Json(new PagedResultDto<NewsItemSummaryDto>());

        var store = await _storeContext.GetCurrentStoreAsync();
        var language = await _workContext.GetWorkingLanguageAsync();
        var news = await _newsService.GetAllNewsAsync(language.Id, store.Id,
            pageIndex: page, pageSize: Math.Clamp(pageSize, 1, 100));

        return Json(new PagedResultDto<NewsItemSummaryDto>
        {
            Page = news.PageIndex,
            PageSize = news.PageSize,
            TotalItems = news.TotalCount,
            TotalPages = news.TotalPages,
            Items = news.Select(n => new NewsItemSummaryDto
            {
                Id = n.Id,
                Title = n.Title,
                Short = n.Short,
                CreatedOn = n.CreatedOnUtc.ToString("yyyy-MM-dd")
            }).ToList()
        });
    }

    // GET /api/news/{id}
    [HttpGet]
    [ProducesResponseType(typeof(NewsItemDto), 200)]
    public virtual async Task<IActionResult> Detail(int id)
    {
        var item = await _newsService.GetNewsByIdAsync(id);
        if (item == null || !item.Published)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.news.itemnotfound") });

        var comments = await _newsService.GetAllCommentsAsync(newsItemId: item.Id, approved: true);
        var commentDtos = new List<NewsCommentDto>();
        foreach (var c in comments.OrderBy(x => x.CreatedOnUtc))
            commentDtos.Add(new NewsCommentDto
            {
                Id = c.Id,
                CustomerName = await CustomerNameAsync(c.CustomerId),
                CommentTitle = c.CommentTitle,
                CommentText = c.CommentText,
                CreatedOn = c.CreatedOnUtc.ToString("yyyy-MM-dd")
            });

        return Json(new NewsItemDto
        {
            Id = item.Id,
            Title = item.Title,
            Full = item.Full,
            CreatedOn = item.CreatedOnUtc.ToString("yyyy-MM-dd"),
            AllowComments = item.AllowComments,
            Comments = commentDtos
        });
    }

    // POST /api/news/{id}/comments   { title, commentText }
    [HttpPost]
    [ProducesResponseType(typeof(StatusResultDto), 200)]
    public virtual async Task<IActionResult> AddComment(int id, [FromBody] CommentApiRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });
        if (request == null || string.IsNullOrWhiteSpace(request.CommentText))
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.content.commentrequired") });

        var item = await _newsService.GetNewsByIdAsync(id);
        if (item == null || !item.Published)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.news.itemnotfound") });
        if (!_newsSettings.Enabled || !item.AllowComments)
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.content.commentsnotallowed") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var isApproved = !_newsSettings.NewsCommentsMustBeApproved;
        await _newsService.InsertNewsCommentAsync(new NewsComment
        {
            NewsItemId = item.Id,
            CustomerId = customer.Id,
            CommentTitle = request.Title,
            CommentText = request.CommentText,
            IsApproved = isApproved,
            StoreId = store.Id,
            CreatedOnUtc = DateTime.UtcNow
        });

        return Json(new StatusResultDto { Success = true, Status = isApproved ? "approved" : "pendingApproval" });
    }

    #endregion
}
