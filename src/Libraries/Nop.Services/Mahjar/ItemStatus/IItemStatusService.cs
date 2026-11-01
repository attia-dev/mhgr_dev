// [Mahjar item-status]
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Mahjar;

namespace Nop.Services.Mahjar.ItemStatus;

/// <summary>
/// The per-product condition report ("item status"): the single header row, the condition photos and the
/// condition notes. Every collection is addressed by PRODUCT identifier (the three tables key on ProductId,
/// mirroring ProductPicture), which is what the admin grids, the storefront product page and the mobile API
/// all have in hand. [Mahjar item-status]
/// </summary>
public partial interface IItemStatusService
{
    #region Item status

    /// <summary>
    /// Gets the condition report header of a product
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the item status, or null when the product has no condition report
    /// </returns>
    Task<ProductItemStatus> GetItemStatusByProductIdAsync(int productId);

    /// <summary>
    /// Inserts a condition report header
    /// </summary>
    /// <param name="itemStatus">Item status</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task InsertItemStatusAsync(ProductItemStatus itemStatus);

    /// <summary>
    /// Updates a condition report header
    /// </summary>
    /// <param name="itemStatus">Item status</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task UpdateItemStatusAsync(ProductItemStatus itemStatus);

    /// <summary>
    /// Deletes a condition report header
    /// </summary>
    /// <param name="itemStatus">Item status</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task DeleteItemStatusAsync(ProductItemStatus itemStatus);

    #endregion

    #region Pictures

    /// <summary>
    /// Gets the condition photos of a product, ordered by display order then identifier
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the item status pictures
    /// </returns>
    Task<IList<ProductItemStatusPicture>> GetItemStatusPicturesByProductIdAsync(int productId);

    /// <summary>
    /// Gets a condition photo
    /// </summary>
    /// <param name="id">Item status picture identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the item status picture
    /// </returns>
    Task<ProductItemStatusPicture> GetItemStatusPictureByIdAsync(int id);

    /// <summary>
    /// Inserts a condition photo
    /// </summary>
    /// <param name="picture">Item status picture</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task InsertItemStatusPictureAsync(ProductItemStatusPicture picture);

    /// <summary>
    /// Updates a condition photo
    /// </summary>
    /// <param name="picture">Item status picture</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task UpdateItemStatusPictureAsync(ProductItemStatusPicture picture);

    /// <summary>
    /// Deletes a condition photo
    /// </summary>
    /// <param name="picture">Item status picture</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task DeleteItemStatusPictureAsync(ProductItemStatusPicture picture);

    #endregion

    #region Notes

    /// <summary>
    /// Gets the condition notes of a product, ordered by display order then identifier
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the item status notes
    /// </returns>
    Task<IList<ProductItemStatusNote>> GetItemStatusNotesByProductIdAsync(int productId);

    /// <summary>
    /// Gets a condition note
    /// </summary>
    /// <param name="id">Item status note identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the item status note
    /// </returns>
    Task<ProductItemStatusNote> GetItemStatusNoteByIdAsync(int id);

    /// <summary>
    /// Inserts a condition note
    /// </summary>
    /// <param name="note">Item status note</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task InsertItemStatusNoteAsync(ProductItemStatusNote note);

    /// <summary>
    /// Updates a condition note
    /// </summary>
    /// <param name="note">Item status note</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task UpdateItemStatusNoteAsync(ProductItemStatusNote note);

    /// <summary>
    /// Deletes a condition note
    /// </summary>
    /// <param name="note">Item status note</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task DeleteItemStatusNoteAsync(ProductItemStatusNote note);

    #endregion
}
