// [Mahjar app-home]
using MHGR.Api.Contracts;
using Nop.Core.Domain.Catalog;

namespace Nop.Web.Factories;

/// <summary>
/// The home category tiles shared by the website home page (HomepageCategoriesViewComponent) and the mobile app
/// (GET api/home/categories, api/categories): the stand-in watch picture of a category without a picture of its own,
/// and the app's category cards (same set, order and pictures as the website, plus the product count). All cached.
/// [Mahjar app-home]
/// </summary>
public partial interface IHomeCategoryModelFactory
{
    /// <summary>
    /// Prepare the tile picture of every home category WITHOUT a picture of its own: the first picture of its first
    /// watch a customer can actually open (published, visible individually, ACL, store mapping, availability dates),
    /// preferring a watch no other tile already shows (the renders are named by their SEO file name)
    /// </summary>
    /// <param name="categories">The home categories in display order, each with its own picture url (null/empty = none)</param>
    /// <param name="pictureSize">The picture size the caller shows (part of the cache key)</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the stand-in picture url by category id (only categories that got one)
    /// </returns>
    Task<IDictionary<int, string>> PrepareFallbackPictureUrlsAsync(IList<(int CategoryId, string OwnPictureUrl)> categories,
        int pictureSize);

    /// <summary>
    /// Prepare the app's home category cards: the categories the website home page shows, in the same order (a category
    /// with no picture at all goes last, as on the website), localized per the working language, with an ABSOLUTE
    /// 600 px picture (own, else the same stand-in watch as the website tile) and the product count (the website rule)
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the cards (0-count categories included; the app decides what to hide)
    /// </returns>
    Task<IList<HomeCategoryDto>> PrepareAppHomeCategoriesAsync();

    /// <summary>
    /// Prepare the picture and product count of any category for the app (api/categories): its own 600 px picture,
    /// else the picture of its home tile; the count follows the website rule
    /// </summary>
    /// <param name="category">Category</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the ABSOLUTE picture url (null when none) and the product count
    /// </returns>
    Task<(string ImageUrl, int ProductCount)> PrepareAppCategoryPictureAndCountAsync(Category category);
}
