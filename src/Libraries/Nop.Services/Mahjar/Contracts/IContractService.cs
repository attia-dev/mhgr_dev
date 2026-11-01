// [Mahjar contracts]
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Mahjar;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Stores;

namespace Nop.Services.Mahjar.Contracts;

/// <summary>
/// The contract-approval stage service: customers freeze their cart into a <see cref="ContractRequest"/>,
/// the admin renders and sends the tokenized contract, the customer signs it, and only an Approved request
/// whose <see cref="ContractRequest.CartHash"/> still matches the current cart unlocks checkout.
/// [Mahjar contracts]
/// </summary>
public partial interface IContractService
{
    /// <summary>Gets a contract request by identifier.</summary>
    /// <param name="id">Contract request identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the contract request (null if not found)
    /// </returns>
    Task<ContractRequest> GetRequestByIdAsync(int id);

    /// <summary>Gets a contract request by its opaque sign-URL token.</summary>
    /// <param name="token">Token</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the contract request (null if not found)
    /// </returns>
    Task<ContractRequest> GetRequestByTokenAsync(Guid token);

    /// <summary>[Mahjar handover] The contract request an order was placed against (the newest one), or null.</summary>
    Task<ContractRequest> GetRequestByOrderIdAsync(int orderId);

    /// <summary>
    /// Gets the customer's newest active request (status Pending, ContractSent, Signed or Approved).
    /// </summary>
    /// <param name="customerId">Customer identifier</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the active contract request (null if none)
    /// </returns>
    Task<ContractRequest> GetActiveRequestAsync(int customerId, int storeId);

    /// <summary>Searches contract requests.</summary>
    /// <param name="customerId">Customer identifier; 0 for all</param>
    /// <param name="storeId">Store identifier; 0 for all</param>
    /// <param name="status">Status filter; null for all</param>
    /// <param name="createdFromUtc">Created from (UTC); null for no lower bound</param>
    /// <param name="createdToUtc">Created to (UTC); null for no upper bound</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the paged list of contract requests (newest first)
    /// </returns>
    Task<IPagedList<ContractRequest>> SearchRequestsAsync(int customerId = 0, int storeId = 0,
        ContractRequestStatus? status = null, DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        int pageIndex = 0, int pageSize = int.MaxValue);

    /// <summary>Counts the contract requests in a given status.</summary>
    /// <param name="status">Status</param>
    /// <param name="storeId">Store identifier; 0 for all</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the number of contract requests
    /// </returns>
    Task<int> GetCountByStatusAsync(ContractRequestStatus status, int storeId = 0);

    /// <summary>Whether the given cart requires a contract before payment.</summary>
    /// <param name="cart">Shopping cart</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains true when a contract is required
    /// </returns>
    Task<bool> IsContractRequiredAsync(IList<ShoppingCartItem> cart);

    /// <summary>Computes the deterministic hash of the cart lines used to detect cart drift.</summary>
    /// <param name="cart">Shopping cart</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the SHA256 hex hash
    /// </returns>
    Task<string> ComputeCartHashAsync(IList<ShoppingCartItem> cart);

    /// <summary>
    /// Computes the deterministic hash of the order items in the identical per-line format as
    /// <see cref="ComputeCartHashAsync"/>, so a placed order can be matched back to the contract request
    /// whose frozen cart it was gated on.
    /// </summary>
    /// <param name="orderItems">Order items</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the SHA256 hex hash
    /// </returns>
    Task<string> ComputeOrderHashAsync(IList<OrderItem> orderItems);

    /// <summary>
    /// Submits a contract request from the current cart. An existing active request with the same cart hash
    /// is returned unchanged; one with a different hash is cancelled and superseded by a new request.
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="cart">Shopping cart</param>
    /// <param name="store">Store</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the (existing or newly created) contract request
    /// </returns>
    Task<ContractRequest> SubmitRequestAsync(Customer customer, IList<ShoppingCartItem> cart, Store store);

    /// <summary>
    /// Whether the customer may proceed to payment: either no contract is required, or an Approved request
    /// exists whose stored cart hash matches the current cart.
    /// </summary>
    /// <param name="customer">Customer</param>
    /// <param name="cart">Shopping cart</param>
    /// <param name="storeId">Store identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains true when checkout is allowed
    /// </returns>
    Task<bool> IsCheckoutAllowedAsync(Customer customer, IList<ShoppingCartItem> cart, int storeId);

    /// <summary>Deserializes the frozen cart snapshot of a request.</summary>
    /// <param name="request">Contract request</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the cart snapshot (never null)
    /// </returns>
    Task<ContractCartSnapshot> DeserializeSnapshotAsync(ContractRequest request);

    /// <summary>Renders the localized contract template with the request's tokens.</summary>
    /// <param name="request">Contract request</param>
    /// <param name="languageId">Language identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the rendered contract HTML
    /// </returns>
    Task<string> RenderContractBodyAsync(ContractRequest request, int languageId);

    /// <summary>
    /// Renders and freezes the contract into a Pending request and sends it to the customer for signing
    /// (status ContractSent).
    /// </summary>
    /// <param name="request">Contract request (must be Pending)</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task SendContractAsync(ContractRequest request);

    /// <summary>Records the customer's signature on a sent contract (status Signed).</summary>
    /// <param name="request">Contract request (must be ContractSent)</param>
    /// <param name="signatureDataUrl">The drawn signature as a PNG data URL</param>
    /// <param name="signedName">The customer's typed full name</param>
    /// <param name="ip">The IP address the signature was submitted from</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task SignContractAsync(ContractRequest request, string signatureDataUrl, string signedName, string ip);

    /// <summary>Approves a signed contract (status Approved), unlocking checkout.</summary>
    /// <param name="request">Contract request (must be Signed)</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task ApproveAsync(ContractRequest request);

    /// <summary>Rejects a contract request with a reason (status Rejected).</summary>
    /// <param name="request">Contract request (must be Pending, ContractSent or Signed)</param>
    /// <param name="reason">Rejection reason</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task RejectAsync(ContractRequest request, string reason);

    /// <summary>Cancels a non-terminal contract request (status Cancelled).</summary>
    /// <param name="request">Contract request</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task CancelAsync(ContractRequest request);

    /// <summary>Marks an approved request as Completed once an order was placed against it.</summary>
    /// <param name="request">Contract request</param>
    /// <param name="orderId">The placed order identifier</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task MarkCompletedAsync(ContractRequest request, int orderId);

    /// <summary>Updates a contract request (stamps <see cref="ContractRequest.UpdatedOnUtc"/>).</summary>
    /// <param name="request">Contract request</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task UpdateRequestAsync(ContractRequest request);

    /// <summary>Gets every contract template, ordered by kind (purchase, rental, mixed).</summary>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the contract templates
    /// </returns>
    Task<IList<ContractTemplate>> GetAllTemplatesAsync();

    /// <summary>Gets a contract template by identifier.</summary>
    /// <param name="id">Contract template identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the contract template (null if not found)
    /// </returns>
    Task<ContractTemplate> GetTemplateByIdAsync(int id);

    /// <summary>
    /// Gets the contract template for a kind, falling back to the mixed template and then to any template
    /// so a missing row can never break contract rendering.
    /// </summary>
    /// <param name="templateType">Contract template kind</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the contract template (null only when none exist at all)
    /// </returns>
    Task<ContractTemplate> GetTemplateByTypeAsync(ContractTemplateType templateType);

    /// <summary>
    /// Determines which contract kind a frozen cart calls for: all-purchase lines give
    /// <see cref="ContractTemplateType.Purchase"/>, all-rental lines give
    /// <see cref="ContractTemplateType.Rental"/>, and any mix gives <see cref="ContractTemplateType.Mixed"/>.
    /// </summary>
    /// <param name="snapshot">The frozen cart snapshot</param>
    /// <returns>The contract template kind</returns>
    ContractTemplateType GetTemplateType(ContractCartSnapshot snapshot);

    /// <summary>Determines which contract kind a request calls for, from its frozen cart snapshot.</summary>
    /// <param name="request">Contract request</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the contract template kind
    /// </returns>
    Task<ContractTemplateType> GetTemplateTypeAsync(ContractRequest request);

    /// <summary>Updates the contract template.</summary>
    /// <param name="template">Contract template</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task UpdateTemplateAsync(ContractTemplate template);
}
