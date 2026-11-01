using Nop.Core.Domain.Catalog;

namespace Nop.Services.Orders;

/// <summary>
/// Rental availability service — prevents double-booking of rental products. [Mahjar buy-or-rent]
/// </summary>
public partial interface IRentalAvailabilityService
{
    /// <summary>
    /// Gets the number of already-reserved units of a product that overlap the given date range
    /// (counting confirmed, non-cancelled orders). [Mahjar rental-occupancy] A rental keeps its unit from the day it
    /// was handed over and, while it has not come back, through today even when overdue; a confirmed return frees it.
    /// </summary>
    Task<int> GetReservedUnitsAsync(int productId, DateTime startDate, DateTime endDate, int? excludeOrderItemId = null);

    /// <summary>
    /// Gets the number of rentable units (capacity) of a product: its stock quantity when it manages
    /// stock, otherwise a single unit.
    /// </summary>
    int GetCapacity(Product product);

    /// <summary>
    /// Indicates whether the requested quantity of a rental product is available for the given date range.
    /// </summary>
    Task<bool> IsRangeAvailableAsync(Product product, DateTime startDate, DateTime endDate, int requestedQuantity, int? excludeOrderItemId = null);

    /// <summary>
    /// Gets the fully-booked (unavailable) date ranges of a rental product within the given window,
    /// so the storefront calendar can disable those days.
    /// </summary>
    Task<IList<(DateTime Start, DateTime End)>> GetUnavailableRangesAsync(Product product, DateTime fromDate, DateTime toDate);

    /// <summary>
    /// When the requested quantity of a rentable item cannot be bought now, because it is out with a renter or
    /// booked for a rental: the last occupied day. Null when it can be bought. [Mahjar rental-occupancy]
    /// </summary>
    Task<DateTime?> GetPurchaseBlockedUntilAsync(Product product, int quantity = 1);
}
