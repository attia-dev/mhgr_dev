// [Mahjar contracts]
using Nop.Core.Domain.Mahjar;
using Nop.Web.Areas.Admin.Models.Mahjar;

namespace Nop.Web.Areas.Admin.Factories;

/// <summary>
/// Represents the contract request model factory (admin area) [Mahjar contracts]
/// </summary>
public partial interface IContractRequestModelFactory
{
    /// <summary>
    /// Prepare contract request search model
    /// </summary>
    /// <param name="searchModel">Contract request search model</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the contract request search model
    /// </returns>
    Task<ContractRequestSearchModel> PrepareContractRequestSearchModelAsync(ContractRequestSearchModel searchModel);

    /// <summary>
    /// Prepare paged contract request list model
    /// </summary>
    /// <param name="searchModel">Contract request search model</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the contract request list model
    /// </returns>
    Task<ContractRequestListModel> PrepareContractRequestListModelAsync(ContractRequestSearchModel searchModel);

    /// <summary>
    /// Prepare contract request model
    /// </summary>
    /// <param name="request">Contract request</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the contract request model
    /// </returns>
    Task<ContractRequestModel> PrepareContractRequestModelAsync(ContractRequest request);

    /// <summary>
    /// [Mahjar handover] Prepare the condition checks card of the Edit page (one before-signing row per item + the
    /// delivery / return checks of the order) and what the current viewer may do: handover staff hold only the
    /// contracts VIEW permission, so every transition is hidden for them
    /// </summary>
    /// <param name="model">Contract request model</param>
    /// <param name="request">Contract request</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task PrepareConditionChecksAsync(ContractRequestModel model, ContractRequest request);

    /// <summary>
    /// Prepare contract template search model
    /// </summary>
    /// <param name="searchModel">Contract template search model</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the contract template search model
    /// </returns>
    Task<ContractTemplateSearchModel> PrepareTemplateSearchModelAsync(ContractTemplateSearchModel searchModel);

    /// <summary>
    /// Prepare paged contract template list model
    /// </summary>
    /// <param name="searchModel">Contract template search model</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the contract template list model
    /// </returns>
    Task<ContractTemplateListModel> PrepareTemplateListModelAsync(ContractTemplateSearchModel searchModel);

    /// <summary>
    /// Prepare contract template content model
    /// </summary>
    /// <param name="template">Contract template</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the contract template content model
    /// </returns>
    Task<ContractTemplateContentModel> PrepareTemplateModelAsync(ContractTemplate template);

    /// <summary>
    /// Prepare contract settings model
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the contract settings model
    /// </returns>
    Task<ContractSettingsModel> PrepareSettingsModelAsync();
}
