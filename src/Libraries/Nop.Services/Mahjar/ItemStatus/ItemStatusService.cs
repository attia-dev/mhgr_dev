// [Mahjar item-status]
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Domain.Mahjar;
using Nop.Data;

namespace Nop.Services.Mahjar.ItemStatus;

/// <summary>
/// The per-product condition report service. All three tables key on ProductId (mirroring ProductPicture), so
/// every read is a single lookup by product identifier; the repositories carry the standard nopCommerce entity
/// cache, invalidated by the cache event consumers in the Caching folder. [Mahjar item-status]
/// </summary>
public partial class ItemStatusService : IItemStatusService
{
    #region Fields

    protected readonly IRepository<ProductItemStatus> _itemStatusRepository;
    protected readonly IRepository<ProductItemStatusPicture> _itemStatusPictureRepository;
    protected readonly IRepository<ProductItemStatusNote> _itemStatusNoteRepository;

    #endregion

    #region Ctor

    public ItemStatusService(IRepository<ProductItemStatus> itemStatusRepository,
        IRepository<ProductItemStatusPicture> itemStatusPictureRepository,
        IRepository<ProductItemStatusNote> itemStatusNoteRepository)
    {
        _itemStatusRepository = itemStatusRepository;
        _itemStatusPictureRepository = itemStatusPictureRepository;
        _itemStatusNoteRepository = itemStatusNoteRepository;
    }

    #endregion

    #region Methods

    #region Item status

    /// <summary>
    /// Gets the condition report header of a product
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the item status, or null when the product has no condition report
    /// </returns>
    public virtual async Task<ProductItemStatus> GetItemStatusByProductIdAsync(int productId)
    {
        if (productId <= 0)
            return null;

        //one row per product, but order by Id so a legacy duplicate can never make the result non-deterministic
        var query = from itemStatus in _itemStatusRepository.Table
            where itemStatus.ProductId == productId
            orderby itemStatus.Id
            select itemStatus;

        return await query.FirstOrDefaultAsync();
    }

    /// <summary>
    /// Inserts a condition report header
    /// </summary>
    /// <param name="itemStatus">Item status</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task InsertItemStatusAsync(ProductItemStatus itemStatus)
    {
        ArgumentNullException.ThrowIfNull(itemStatus);

        itemStatus.UpdatedOnUtc = DateTime.UtcNow;

        await _itemStatusRepository.InsertAsync(itemStatus);
    }

    /// <summary>
    /// Updates a condition report header
    /// </summary>
    /// <param name="itemStatus">Item status</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task UpdateItemStatusAsync(ProductItemStatus itemStatus)
    {
        ArgumentNullException.ThrowIfNull(itemStatus);

        itemStatus.UpdatedOnUtc = DateTime.UtcNow;

        await _itemStatusRepository.UpdateAsync(itemStatus);
    }

    /// <summary>
    /// Deletes a condition report header
    /// </summary>
    /// <param name="itemStatus">Item status</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task DeleteItemStatusAsync(ProductItemStatus itemStatus)
    {
        ArgumentNullException.ThrowIfNull(itemStatus);

        await _itemStatusRepository.DeleteAsync(itemStatus);
    }

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
    public virtual async Task<IList<ProductItemStatusPicture>> GetItemStatusPicturesByProductIdAsync(int productId)
    {
        if (productId <= 0)
            return new List<ProductItemStatusPicture>();

        var query = from picture in _itemStatusPictureRepository.Table
            where picture.ProductId == productId
            orderby picture.DisplayOrder, picture.Id
            select picture;

        return await query.ToListAsync();
    }

    /// <summary>
    /// Gets a condition photo
    /// </summary>
    /// <param name="id">Item status picture identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the item status picture
    /// </returns>
    public virtual async Task<ProductItemStatusPicture> GetItemStatusPictureByIdAsync(int id)
    {
        return await _itemStatusPictureRepository.GetByIdAsync(id, cache => default);
    }

    /// <summary>
    /// Inserts a condition photo
    /// </summary>
    /// <param name="picture">Item status picture</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task InsertItemStatusPictureAsync(ProductItemStatusPicture picture)
    {
        ArgumentNullException.ThrowIfNull(picture);

        await _itemStatusPictureRepository.InsertAsync(picture);
    }

    /// <summary>
    /// Updates a condition photo
    /// </summary>
    /// <param name="picture">Item status picture</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task UpdateItemStatusPictureAsync(ProductItemStatusPicture picture)
    {
        ArgumentNullException.ThrowIfNull(picture);

        await _itemStatusPictureRepository.UpdateAsync(picture);
    }

    /// <summary>
    /// Deletes a condition photo
    /// </summary>
    /// <param name="picture">Item status picture</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task DeleteItemStatusPictureAsync(ProductItemStatusPicture picture)
    {
        ArgumentNullException.ThrowIfNull(picture);

        await _itemStatusPictureRepository.DeleteAsync(picture);
    }

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
    public virtual async Task<IList<ProductItemStatusNote>> GetItemStatusNotesByProductIdAsync(int productId)
    {
        if (productId <= 0)
            return new List<ProductItemStatusNote>();

        var query = from note in _itemStatusNoteRepository.Table
            where note.ProductId == productId
            orderby note.DisplayOrder, note.Id
            select note;

        return await query.ToListAsync();
    }

    /// <summary>
    /// Gets a condition note
    /// </summary>
    /// <param name="id">Item status note identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the item status note
    /// </returns>
    public virtual async Task<ProductItemStatusNote> GetItemStatusNoteByIdAsync(int id)
    {
        return await _itemStatusNoteRepository.GetByIdAsync(id, cache => default);
    }

    /// <summary>
    /// Inserts a condition note
    /// </summary>
    /// <param name="note">Item status note</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task InsertItemStatusNoteAsync(ProductItemStatusNote note)
    {
        ArgumentNullException.ThrowIfNull(note);

        if (note.CreatedOnUtc == default)
            note.CreatedOnUtc = DateTime.UtcNow;

        await _itemStatusNoteRepository.InsertAsync(note);
    }

    /// <summary>
    /// Updates a condition note
    /// </summary>
    /// <param name="note">Item status note</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task UpdateItemStatusNoteAsync(ProductItemStatusNote note)
    {
        ArgumentNullException.ThrowIfNull(note);

        await _itemStatusNoteRepository.UpdateAsync(note);
    }

    /// <summary>
    /// Deletes a condition note
    /// </summary>
    /// <param name="note">Item status note</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task DeleteItemStatusNoteAsync(ProductItemStatusNote note)
    {
        ArgumentNullException.ThrowIfNull(note);

        await _itemStatusNoteRepository.DeleteAsync(note);
    }

    #endregion

    #endregion
}
