// [Mahjar handover]
using Nop.Core.Domain.Mahjar;
using Nop.Web.Areas.Admin.Models.Mahjar;

namespace Nop.Web.Areas.Admin.Factories;

/// <summary>
/// Represents the handover desk model factory (admin area) [Mahjar handover]
/// </summary>
public partial interface IHandoverModelFactory
{
    /// <summary>Prepare the handover desk search model (the stage dropdown)</summary>
    Task<HandoverSearchModel> PrepareHandoverSearchModelAsync(HandoverSearchModel searchModel);

    /// <summary>Prepare one page of a handover desk queue</summary>
    Task<HandoverTaskListModel> PrepareHandoverTaskListModelAsync(HandoverSearchModel searchModel);

    /// <summary>Prepare the condition check editor</summary>
    Task<ConditionCheckModel> PrepareConditionCheckModelAsync(ConditionCheck check);

    /// <summary>Prepare one page of the photos grid of a condition check</summary>
    Task<ConditionCheckPictureListModel> PrepareConditionCheckPictureListModelAsync(ConditionCheckPictureSearchModel searchModel,
        ConditionCheck check);

    /// <summary>
    /// Prepare the condition check rows of a contract request: one before-signing row per item (started or not)
    /// plus the delivery / return checks of the order placed against it
    /// </summary>
    Task<(IList<ContractConditionCheckRowModel> Rows, bool PreContractReady)> PrepareContractConditionChecksAsync(ContractRequest request);
}
