// [Mahjar app-home]
using MHGR.Api.Contracts;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Seo;
using Nop.Web.Infrastructure.Cache;

namespace Nop.Web.Factories;

/// <summary>
/// The home category tiles shared by the website home page and the mobile app. One implementation of the stand-in
/// picture rule (moved here from HomepageCategoriesViewComponent), so the website tiles and the app cards always show
/// the same watch for a category without a picture of its own. [Mahjar app-home]
/// </summary>
public partial class HomeCategoryModelFactory : IHomeCategoryModelFactory
{
    #region Constants

    /// <summary>The app's card picture size (px): sharp on a 3x phone at the 112 dp banner plate, light on the wire.</summary>
    protected const int APP_PICTURE_SIZE = 600;

    /// <summary>The app cards are cached briefly: the counts also move with product ACL/store-mapping edits, which clear nothing.</summary>
    protected const int APP_CACHE_MINUTES = 5;

    #endregion

    #region Fields

    protected readonly CatalogSettings _catalogSettings;
    protected readonly ICategoryService _categoryService;
    protected readonly ICustomerService _customerService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IPictureService _pictureService;
    protected readonly IProductService _productService;
    protected readonly IStaticCacheManager _staticCacheManager;
    protected readonly IStoreContext _storeContext;
    protected readonly IUrlRecordService _urlRecordService;
    protected readonly IWebHelper _webHelper;
    protected readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public HomeCategoryModelFactory(CatalogSettings catalogSettings,
        ICategoryService categoryService,
        ICustomerService customerService,
        ILocalizationService localizationService,
        IPictureService pictureService,
        IProductService productService,
        IStaticCacheManager staticCacheManager,
        IStoreContext storeContext,
        IUrlRecordService urlRecordService,
        IWebHelper webHelper,
        IWorkContext workContext)
    {
        _catalogSettings = catalogSettings;
        _categoryService = categoryService;
        _customerService = customerService;
        _localizationService = localizationService;
        _pictureService = pictureService;
        _productService = productService;
        _staticCacheManager = staticCacheManager;
        _storeContext = storeContext;
        _urlRecordService = urlRecordService;
        _webHelper = webHelper;
        _workContext = workContext;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Gets the category's own picture at the given size; empty when it has none (never the "no image" placeholder)
    /// </summary>
    protected virtual async Task<string> GetOwnPictureUrlAsync(Category category, int pictureSize)
    {
        if (category == null || category.PictureId <= 0)
            return string.Empty;

        return await _pictureService.GetPictureUrlAsync(category.PictureId, pictureSize, showDefaultPicture: false) ?? string.Empty;
    }

    /// <summary>
    /// Gets the number of products in a category the way the website counts them (CatalogModelFactory category
    /// navigation): published + visible individually + ACL + store mapping, subcategories included when
    /// CatalogSettings.ShowCategoryProductNumberIncludingSubcategories is on
    /// </summary>
    protected virtual async Task<int> GetProductCountAsync(int categoryId, int storeId)
    {
        var categoryIds = new List<int> { categoryId };
        if (_catalogSettings.ShowCategoryProductNumberIncludingSubcategories)
            categoryIds.AddRange(await _categoryService.GetChildCategoryIdsAsync(categoryId, storeId));

        return await _productService.GetNumberOfProductsInCategoryAsync(categoryIds, storeId);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Prepare the tile picture of every home category WITHOUT a picture of its own: the first picture of its first
    /// watch a customer can actually open (the product search applies published, visible individually, ACL, store
    /// mapping and the availability dates), preferring a watch no other tile already shows (the renders are named by
    /// their SEO file name). Cached like the home category models themselves (store, roles, picture size, language,
    /// and the store location — the urls are absolute off the request host, so an app on 10.0.2.2 never gets a
    /// "localhost" url cached by a browser) and cleared by any category, product, product-category, product-picture
    /// or picture change.
    /// </summary>
    /// <param name="categories">The home categories in display order, each with its own picture url (null/empty = none)</param>
    /// <param name="pictureSize">The picture size the caller shows (part of the cache key)</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the stand-in picture url by category id (only categories that got one)
    /// </returns>
    public virtual async Task<IDictionary<int, string>> PrepareFallbackPictureUrlsAsync(IList<(int CategoryId, string OwnPictureUrl)> categories,
        int pictureSize)
    {
        ArgumentNullException.ThrowIfNull(categories);

        if (categories.All(category => !string.IsNullOrEmpty(category.OwnPictureUrl)))
            return new Dictionary<int, string>();

        var customer = await _workContext.GetCurrentCustomerAsync();
        var customerRoleIds = await _customerService.GetCustomerRoleIdsAsync(customer);
        var store = await _storeContext.GetCurrentStoreAsync();
        var language = await _workContext.GetWorkingLanguageAsync();
        var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(NopModelCacheDefaults.MahjarHomeCategoryFallbackPicturesKey,
            store, customerRoleIds, pictureSize, language, _webHelper.GetStoreLocation());

        return await _staticCacheManager.GetAsync(cacheKey, async () =>
        {
            var images = new Dictionary<int, string>();
            var usedUrls = categories
                .Where(category => !string.IsNullOrEmpty(category.OwnPictureUrl))
                .Select(category => category.OwnPictureUrl)
                .ToList();

            foreach (var (categoryId, _) in categories.Where(category => string.IsNullOrEmpty(category.OwnPictureUrl)))
            {
                string chosen = null;
                string first = null;
                var products = await _productService.SearchProductsAsync(pageSize: 8,
                    categoryIds: new List<int> { categoryId }, storeId: store.Id, visibleIndividuallyOnly: true);
                foreach (var product in products)
                {
                    var picture = (await _pictureService.GetPicturesByProductIdAsync(product.Id, 1)).FirstOrDefault();
                    if (picture == null)
                        continue;

                    var (url, _) = await _pictureService.GetPictureUrlAsync(picture, pictureSize, showDefaultPicture: false);
                    if (string.IsNullOrEmpty(url))
                        continue;

                    var seo = picture.SeoFilename;
                    if (string.IsNullOrEmpty(seo) || !usedUrls.Any(used => used.Contains(seo, StringComparison.OrdinalIgnoreCase)))
                    {
                        chosen = url;
                        break;
                    }

                    first ??= url;
                }

                chosen ??= first;
                if (string.IsNullOrEmpty(chosen))
                    continue;

                images[categoryId] = chosen;
                usedUrls.Add(chosen);
            }

            return (IDictionary<int, string>)images;
        });
    }

    /// <summary>
    /// Prepare the app's home category cards: the categories the website home page shows, in the same order (a
    /// category with no picture at all goes last, as on the website), localized per the working language, with an
    /// ABSOLUTE 600 px picture (own, else the same stand-in watch as the website tile) and the product count (the
    /// website rule). Cached per store, roles, language and store location for a few minutes; the key shares the
    /// home category and stand-in prefixes, so a category, product or picture edit clears it at once (the Mahjar
    /// consumer also clears the stock per-category product-number cache on product edits, so the counts follow).
    /// Built straight from the category service, NOT from the stock CatalogModelFactory: its home category and
    /// category picture caches are not keyed by host, so an /api request (the app on another host, e.g. 10.0.2.2)
    /// would write app-host picture urls into the models every website visitor gets.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the cards (0-count categories included; the app decides what to hide)
    /// </returns>
    public virtual async Task<IList<HomeCategoryDto>> PrepareAppHomeCategoriesAsync()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var customerRoleIds = await _customerService.GetCustomerRoleIdsAsync(customer);
        var store = await _storeContext.GetCurrentStoreAsync();
        var language = await _workContext.GetWorkingLanguageAsync();
        var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(NopModelCacheDefaults.MahjarAppHomeCategoriesKey,
            store, customerRoleIds, language, _webHelper.GetStoreLocation());
        cacheKey.CacheTime = APP_CACHE_MINUTES;

        return await _staticCacheManager.GetAsync(cacheKey, async () =>
        {
            //the website's own home category set and order: the query the stock homepage factory runs (published,
            //ShowOnHomepage, ACL + store mapping, display order), but not the stock factory itself (host-blind caches)
            var categories = await _categoryService.GetAllCategoriesDisplayedOnHomepageAsync();

            var ownPictures = new List<(int CategoryId, string OwnPictureUrl)>();
            foreach (var category in categories)
                ownPictures.Add((category.Id, await GetOwnPictureUrlAsync(category, APP_PICTURE_SIZE)));

            var fallbackPictures = await PrepareFallbackPictureUrlsAsync(ownPictures, APP_PICTURE_SIZE);

            var items = new List<HomeCategoryDto>();
            for (var i = 0; i < categories.Count; i++)
            {
                var category = categories[i];
                var imageUrl = ownPictures[i].OwnPictureUrl;
                if (string.IsNullOrEmpty(imageUrl))
                    imageUrl = fallbackPictures.TryGetValue(category.Id, out var fallback) ? fallback : null;

                items.Add(new HomeCategoryDto
                {
                    Id = category.Id,
                    Name = await _localizationService.GetLocalizedAsync(category, x => x.Name),
                    SeName = await _urlRecordService.GetSeNameAsync(category),
                    ImageUrl = string.IsNullOrEmpty(imageUrl) ? null : imageUrl,
                    ProductCount = await GetProductCountAsync(category.Id, store.Id)
                });
            }

            //the website places a category with no watch to show after the others (a stable sort keeps the order)
            return (IList<HomeCategoryDto>)items.OrderBy(item => string.IsNullOrEmpty(item.ImageUrl) ? 1 : 0).ToList();
        });
    }

    /// <summary>
    /// Prepare the picture and product count of any category for the app (api/categories): its own 600 px picture,
    /// else the picture of its home tile (so a Browse banner shows the same watch as the home card); the count
    /// follows the website rule
    /// </summary>
    /// <param name="category">Category</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the ABSOLUTE picture url (null when none) and the product count
    /// </returns>
    public virtual async Task<(string ImageUrl, int ProductCount)> PrepareAppCategoryPictureAndCountAsync(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);

        var store = await _storeContext.GetCurrentStoreAsync();
        var imageUrl = await GetOwnPictureUrlAsync(category, APP_PICTURE_SIZE);
        if (string.IsNullOrEmpty(imageUrl))
            imageUrl = (await PrepareAppHomeCategoriesAsync()).FirstOrDefault(item => item.Id == category.Id)?.ImageUrl;

        return (string.IsNullOrEmpty(imageUrl) ? null : imageUrl, await GetProductCountAsync(category.Id, store.Id));
    }

    #endregion
}
