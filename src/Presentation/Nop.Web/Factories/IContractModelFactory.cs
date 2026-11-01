// [Mahjar contracts]
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Mahjar;
using Nop.Web.Models.Contract;

namespace Nop.Web.Factories;

/// <summary>Builds the storefront (customer) contract-request view models. [Mahjar contracts]</summary>
public partial interface IContractModelFactory
{
    Task<ContractListModel> PrepareContractListModelAsync();

    Task<ContractSignModel> PrepareContractSignModelAsync(ContractRequest request);

    /// <summary>[Mahjar handover] The read-only contract page (document, signature, condition checks).</summary>
    Task<ContractViewModel> PrepareContractViewModelAsync(ContractRequest request);

    /// <summary>[Mahjar handover] The condition checks of a contract the customer may see (confirmed ones only).</summary>
    Task<IList<ContractConditionCheckModel>> PrepareConditionChecksAsync(ContractRequest request);
}
