// [Mahjar home-slides]
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Mahjar;

namespace Nop.Services.Mahjar.HomeSlides;

/// <summary>
/// The admin-managed home slides: the Hero carousel (web + app) and the Feature bands (web). The admin reads the
/// whole table through <see cref="SearchHomeSlidesAsync"/>; the storefront and the mobile API read the active,
/// in-schedule slides of one placement and channel through <see cref="GetActiveHomeSlidesAsync"/>, which is
/// served from the static cache. [Mahjar home-slides]
/// </summary>
public partial interface IHomeSlideService
{
    /// <summary>
    /// Gets the home slides (admin list), ordered by placement, display order then identifier
    /// </summary>
    /// <param name="placement">Placement; null to load every placement</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the home slides
    /// </returns>
    Task<IPagedList<HomeSlide>> SearchHomeSlidesAsync(HomeSlidePlacement? placement = null, int pageIndex = 0, int pageSize = int.MaxValue);

    /// <summary>
    /// Gets a home slide
    /// </summary>
    /// <param name="homeSlideId">Home slide identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the home slide, or null when it does not exist
    /// </returns>
    Task<HomeSlide> GetHomeSlideByIdAsync(int homeSlideId);

    /// <summary>
    /// Inserts a home slide (stamps CreatedOnUtc and UpdatedOnUtc)
    /// </summary>
    /// <param name="homeSlide">Home slide</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task InsertHomeSlideAsync(HomeSlide homeSlide);

    /// <summary>
    /// Updates a home slide (stamps UpdatedOnUtc)
    /// </summary>
    /// <param name="homeSlide">Home slide</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task UpdateHomeSlideAsync(HomeSlide homeSlide);

    /// <summary>
    /// Deletes a home slide (hard delete), then its picture and mobile picture — each only when nothing else uses
    /// it (the seed slides share product pictures)
    /// </summary>
    /// <param name="homeSlide">Home slide</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task DeleteHomeSlideAsync(HomeSlide homeSlide);

    /// <summary>
    /// Gets the slides to show now: published, with a picture, enabled for the channel and inside their schedule
    /// window, ordered by display order then identifier
    /// </summary>
    /// <param name="placement">Placement</param>
    /// <param name="channel">The asking surface (web store or mobile app)</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the active home slides
    /// </returns>
    Task<IList<HomeSlide>> GetActiveHomeSlidesAsync(HomeSlidePlacement placement, HomeSlideChannel channel);

    /// <summary>
    /// Gets the entity a slide links to (its product, category, manufacturer or vendor), but only while a customer can
    /// still open it: it exists and is not deleted; a product is published, allowed by ACL and store mapping and inside
    /// its availability dates; a category / manufacturer is published and allowed by ACL and store mapping; a vendor
    /// is active. The storefront and the mobile API drop the link of a slide this returns null for.
    /// </summary>
    /// <param name="slide">Home slide</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the live target entity; null for a gone or hidden target and for the other link kinds
    /// (none, search, custom link)
    /// </returns>
    Task<BaseEntity> GetLiveTargetEntityAsync(HomeSlide slide);

    /// <summary>
    /// Gets a value indicating whether anything references a picture: a product gallery, an attribute value or
    /// combination picture, a category / manufacturer / vendor image, a condition photo, a customer avatar, the
    /// store or PDF logo, a Swiper widget slide, or any home slide (as its picture or mobile picture)
    /// </summary>
    /// <param name="pictureId">Picture identifier; 0 is never in use</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result is true when the picture is in use
    /// </returns>
    Task<bool> IsPictureInUseAsync(int pictureId);

    /// <summary>
    /// Deletes a picture a home slide no longer uses (a replaced or removed slide picture) — but only when nothing
    /// else still references it (see <see cref="IsPictureInUseAsync"/>)
    /// </summary>
    /// <param name="pictureId">Picture identifier; 0 is ignored</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result is true when the picture row was deleted
    /// </returns>
    Task<bool> DeletePictureIfUnusedAsync(int pictureId);
}
