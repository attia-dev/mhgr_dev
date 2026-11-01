// [Mahjar partner-request]
using Nop.Core.Domain.Vendors;
using Nop.Web.Areas.Admin.Models.Mahjar;

namespace Nop.Web.Areas.Admin.Factories;

/// <summary>
/// [Mahjar partner-request] Represents the partner request model factory
/// </summary>
public partial interface IPartnerRequestModelFactory
{
    /// <summary>
    /// Prepares the search model of the partner request queue
    /// </summary>
    /// <param name="searchModel">Search model</param>
    /// <returns>A task that represents the asynchronous operation; the search model</returns>
    Task<PartnerRequestSearchModel> PreparePartnerRequestSearchModelAsync(PartnerRequestSearchModel searchModel);

    /// <summary>
    /// Prepares the paged list of partner requests
    /// </summary>
    /// <param name="searchModel">Search model</param>
    /// <returns>A task that represents the asynchronous operation; the list model</returns>
    Task<PartnerRequestListModel> PreparePartnerRequestListModelAsync(PartnerRequestSearchModel searchModel);

    /// <summary>
    /// Prepares the review screen of one partner request
    /// </summary>
    /// <param name="model">Model, or null to build a new one</param>
    /// <param name="vendor">The application</param>
    /// <returns>A task that represents the asynchronous operation; the model</returns>
    Task<PartnerRequestModel> PreparePartnerRequestModelAsync(PartnerRequestModel model, Vendor vendor);
}
