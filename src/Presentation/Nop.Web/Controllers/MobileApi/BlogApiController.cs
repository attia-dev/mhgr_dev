using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MHGR.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Customers;
using Nop.Services.Authentication;
using Nop.Services.Blogs;
using Nop.Services.Customers;
using Nop.Services.Localization;

namespace Nop.Web.Controllers;

/// <summary>
/// Read + comment Blog API for the buyer app: list posts, open a post with its approved comments, and (when signed
/// in) add a comment. Returns shared MHGR.Api.Contracts DTOs. [Mahjar mobile-api]
/// </summary>
[IgnoreAntiforgeryToken]
public partial class BlogApiController : Controller
{
    #region Fields

    protected readonly BlogSettings _blogSettings;
    protected readonly IApiTokenService _apiTokenService;
    protected readonly IBlogService _blogService;
    protected readonly ICustomerService _customerService;
    protected readonly ILocalizationService _localizationService; // [Mahjar i18n]
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public BlogApiController(BlogSettings blogSettings,
        IApiTokenService apiTokenService,
        IBlogService blogService,
        ICustomerService customerService,
        ILocalizationService localizationService,
        IStoreContext storeContext,
        IWorkContext workContext)
    {
        _blogSettings = blogSettings;
        _apiTokenService = apiTokenService;
        _blogService = blogService;
        _customerService = customerService;
        _localizationService = localizationService;
        _storeContext = storeContext;
        _workContext = workContext;
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

    // GET /api/blog?page=&pageSize=
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<BlogPostSummaryDto>), 200)]
    public virtual async Task<IActionResult> List(int page = 0, int pageSize = 20)
    {
        if (!_blogSettings.Enabled)
            return Json(new PagedResultDto<BlogPostSummaryDto>());

        var store = await _storeContext.GetCurrentStoreAsync();
        var language = await _workContext.GetWorkingLanguageAsync();
        var posts = await _blogService.GetAllBlogPostsAsync(store.Id, language.Id,
            pageIndex: page, pageSize: Math.Clamp(pageSize, 1, 100));

        return Json(new PagedResultDto<BlogPostSummaryDto>
        {
            Page = posts.PageIndex,
            PageSize = posts.PageSize,
            TotalItems = posts.TotalCount,
            TotalPages = posts.TotalPages,
            Items = posts.Select(p => new BlogPostSummaryDto
            {
                Id = p.Id,
                Title = p.Title,
                BodyOverview = string.IsNullOrEmpty(p.BodyOverview) ? p.Body : p.BodyOverview,
                CreatedOn = p.CreatedOnUtc.ToString("yyyy-MM-dd")
            }).ToList()
        });
    }

    // GET /api/blog/{id}
    [HttpGet]
    [ProducesResponseType(typeof(BlogPostDto), 200)]
    public virtual async Task<IActionResult> Detail(int id)
    {
        var post = await _blogService.GetBlogPostByIdAsync(id);
        if (post == null)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.blog.postnotfound") });

        var comments = await _blogService.GetAllCommentsAsync(blogPostId: post.Id, approved: true);
        var commentDtos = new List<BlogCommentDto>();
        foreach (var c in comments.OrderBy(x => x.CreatedOnUtc))
            commentDtos.Add(new BlogCommentDto
            {
                Id = c.Id,
                CustomerName = await CustomerNameAsync(c.CustomerId),
                CommentText = c.CommentText,
                CreatedOn = c.CreatedOnUtc.ToString("yyyy-MM-dd")
            });

        return Json(new BlogPostDto
        {
            Id = post.Id,
            Title = post.Title,
            Body = post.Body,
            CreatedOn = post.CreatedOnUtc.ToString("yyyy-MM-dd"),
            AllowComments = post.AllowComments,
            Comments = commentDtos
        });
    }

    // POST /api/blog/{id}/comments   { commentText }
    [HttpPost]
    [ProducesResponseType(typeof(StatusResultDto), 200)]
    public virtual async Task<IActionResult> AddComment(int id, [FromBody] CommentApiRequest request)
    {
        var customer = await GetTokenCustomerAsync();
        if (customer == null)
            return Unauthorized(new { error = await _localizationService.GetResourceAsync("mahjar.api.common.invalidtoken") });
        if (request == null || string.IsNullOrWhiteSpace(request.CommentText))
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.content.commentrequired") });

        var post = await _blogService.GetBlogPostByIdAsync(id);
        if (post == null)
            return NotFound(new { error = await _localizationService.GetResourceAsync("mahjar.api.blog.postnotfound") });
        if (!_blogSettings.Enabled || !post.AllowComments)
            return BadRequest(new { error = await _localizationService.GetResourceAsync("mahjar.api.content.commentsnotallowed") });

        var store = await _storeContext.GetCurrentStoreAsync();
        var isApproved = !_blogSettings.BlogCommentsMustBeApproved;
        await _blogService.InsertBlogCommentAsync(new BlogComment
        {
            BlogPostId = post.Id,
            CustomerId = customer.Id,
            CommentText = request.CommentText,
            IsApproved = isApproved,
            StoreId = store.Id,
            CreatedOnUtc = DateTime.UtcNow
        });

        return Json(new StatusResultDto { Success = true, Status = isApproved ? "approved" : "pendingApproval" });
    }

    #endregion
}
