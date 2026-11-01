// [Mahjar handover]
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Mahjar;
using Nop.Core.Domain.Orders;

namespace Nop.Services.Mahjar.Handover;

/// <summary>
/// The handover desk: documents the condition of an item with photos, video and notes at every checkpoint of its
/// life (intake from the partner, before the customer signs, delivery, rental return), locks each check as evidence,
/// and applies what each checkpoint means — the order is delivered, the rental is returned with its deposit
/// assessed, and a receipt (intake / return) becomes the product's public condition report. [Mahjar handover]
/// </summary>
public partial interface IConditionCheckService
{
    #region Checks

    /// <summary>Gets a condition check by identifier (null if not found).</summary>
    Task<ConditionCheck> GetCheckByIdAsync(int id);

    /// <summary>Gets every check of a contract request (pre-contract checks), ordered by checkpoint.</summary>
    Task<IList<ConditionCheck>> GetChecksByContractRequestIdAsync(int contractRequestId);

    /// <summary>Gets every check of an order (delivery / return checks), ordered by checkpoint.</summary>
    Task<IList<ConditionCheck>> GetChecksByOrderIdAsync(int orderId);

    /// <summary>
    /// Gets the checks the customer of a contract may see: its pre-contract checks and, once the order exists, the
    /// delivery / return checks of that order — confirmed ones only.
    /// </summary>
    Task<IList<ConditionCheck>> GetCustomerVisibleChecksAsync(ContractRequest request);

    /// <summary>
    /// Gets the check of a checkpoint, starting a draft when there is none yet. Intake: by product (a new draft
    /// unless an intake of that product is in progress). Pre-contract: by contract request + product (only while
    /// the contract is Pending). Delivery / return: by order item.
    /// </summary>
    Task<ConditionCheck> GetOrCreateCheckAsync(ConditionCheckType type, int productId, int contractRequestId,
        int orderItemId, Customer employee);

    /// <summary>Saves the editable fields of a draft check (throws when the check is locked).</summary>
    Task UpdateCheckAsync(ConditionCheck check);

    /// <summary>Deletes a draft check with its photos and video (throws when the check is locked).</summary>
    Task DeleteDraftAsync(ConditionCheck check);

    /// <summary>Whether the check can still be edited.</summary>
    bool IsEditable(ConditionCheck check);

    /// <summary>Whether the check has at least one photo or a video.</summary>
    Task<bool> HasMediaAsync(ConditionCheck check);

    /// <summary>
    /// Confirms and locks an intake / delivery / return check (media required) and applies its checkpoint:
    /// intake → the product condition report is replaced; delivery → order note, and the item's stock shipment is
    /// shipped and delivered at once, so the order is PartiallyShipped while items remain and Delivered with the last
    /// one; return → the rental is marked returned with its deposit / deduction / refund, and the product condition
    /// report is replaced. Pre-contract checks are locked by sending the contract instead.
    /// </summary>
    Task<ConditionCheckConfirmResult> ConfirmAsync(ConditionCheck check, Customer employee);

    /// <summary>Whether the desk handed an order item over (it has a confirmed delivery check).</summary>
    Task<bool> IsHandedOverByDeskAsync(int orderItemId);

    /// <summary>Whether every item of a stock shipment was handed over by the desk.</summary>
    Task<bool> IsShipmentHandedOverByDeskAsync(int shipmentId);

    /// <summary>
    /// Whether an order item was handed over to the customer: a confirmed desk delivery, the whole order delivered, or
    /// the item in a delivered stock shipment.
    /// </summary>
    Task<bool> IsOrderItemDeliveredAsync(OrderItem orderItem, Order order);

    #endregion

    #region Media

    /// <summary>Gets the photos of a check in display order.</summary>
    Task<IList<ConditionCheckPicture>> GetPicturesAsync(int conditionCheckId);

    /// <summary>Gets a check photo by identifier (null if not found).</summary>
    Task<ConditionCheckPicture> GetPictureByIdAsync(int id);

    /// <summary>Adds uploaded photos to a draft check. Returns a localized error, or null on success.</summary>
    Task<string> AddPhotosAsync(ConditionCheck check, IList<IFormFile> files);

    /// <summary>Updates the caption / display order of a photo of a draft check.</summary>
    Task UpdatePictureAsync(ConditionCheckPicture picture);

    /// <summary>Removes a photo (and its picture) from a draft check.</summary>
    Task DeletePictureAsync(ConditionCheckPicture picture);

    /// <summary>
    /// Stores the uploaded video of a draft check (streamed to disk, replacing any previous one).
    /// Returns the new site-relative path, or a localized error.
    /// </summary>
    Task<(string VideoFile, string Error)> UploadVideoAsync(ConditionCheck check, IFormFile file);

    /// <summary>Removes the uploaded video of a draft check.</summary>
    Task RemoveVideoAsync(ConditionCheck check);

    /// <summary>
    /// Copies the photos, video, grade and summary of the product's condition report into a draft check (the latest
    /// receipt already put them there). Returns how many media items were copied.
    /// </summary>
    Task<int> ImportFromProductStatusAsync(ConditionCheck check);

    /// <summary>The accepted video extensions (lowercase, dot-prefixed), never wider than .mp4 .webm .mov .m4v.</summary>
    IList<string> GetAllowedVideoExtensions();

    #endregion

    #region Contracts

    /// <summary>The products of a contract request that still lack before-signing condition media.</summary>
    Task<IList<int>> GetProductIdsMissingPreContractMediaAsync(ContractRequest request);

    /// <summary>Locks the pre-contract checks of a contract request; called when the contract is sent.</summary>
    Task LockPreContractChecksAsync(ContractRequest request, Customer employee);

    #endregion

    #region Rentals

    /// <summary>The refundable security deposit of a rental order item (frozen contract amount when available).</summary>
    Task<decimal> GetRentalDepositAsync(OrderItem orderItem);

    #endregion

    #region Queues

    /// <summary>
    /// Gets one page of a handover-desk work queue, narrowed by what the desk searched for (null = the whole queue).
    /// </summary>
    Task<IPagedList<HandoverTask>> SearchTasksAsync(HandoverStage stage, HandoverTaskFilter filter = null,
        int pageIndex = 0, int pageSize = int.MaxValue);

    /// <summary>The items of an order that the "awaiting delivery" queue lists (paid, active, not handed over yet).</summary>
    Task<IList<OrderItem>> GetItemsAwaitingDeliveryAsync(Order order);

    /// <summary>
    /// [Mahjar handover] The rented items of an order that the "awaiting return" queue lists: out with the customer
    /// (handed over by the desk or delivered from the order screens) and not brought back yet.
    /// </summary>
    Task<IList<OrderItem>> GetItemsAwaitingReturnAsync(Order order);

    #endregion
}
