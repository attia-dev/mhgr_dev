// [Mahjar home-slides]
using Nop.Core.Domain.Mahjar;
using Nop.Web.Areas.Admin.Models.Mahjar;

namespace Nop.Web.Areas.Admin.Factories;

/// <summary>
/// Represents the home slide model factory (admin area) [Mahjar home-slides]
/// </summary>
public partial interface IHomeSlideModelFactory
{
    /// <summary>
    /// Prepare home slide search model
    /// </summary>
    /// <param name="searchModel">Home slide search model</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the home slide search model
    /// </returns>
    Task<HomeSlideSearchModel> PrepareHomeSlideSearchModelAsync(HomeSlideSearchModel searchModel);

    /// <summary>
    /// Prepare paged home slide list model
    /// </summary>
    /// <param name="searchModel">Home slide search model</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the home slide list model
    /// </returns>
    Task<HomeSlideListModel> PrepareHomeSlideListModelAsync(HomeSlideSearchModel searchModel);

    /// <summary>
    /// Prepare home slide model
    /// </summary>
    /// <param name="model">Home slide model; null to build it from the entity</param>
    /// <param name="homeSlide">Home slide; null for a new slide</param>
    /// <param name="excludeProperties">Whether to keep the posted values (a redisplayed form) instead of the entity's</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the home slide model
    /// </returns>
    Task<HomeSlideModel> PrepareHomeSlideModelAsync(HomeSlideModel model, HomeSlide homeSlide, bool excludeProperties = false);
}
