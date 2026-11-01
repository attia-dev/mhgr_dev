using LinqToDB;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Mahjar;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Data;
using Nop.Services.Helpers;

namespace Nop.Services.Orders;

/// <summary>
/// Rental availability service. A date range is unavailable when the number of overlapping reserved units
/// (from confirmed, non-cancelled orders) reaches the product's rentable-unit capacity. [Mahjar buy-or-rent]
///
/// [Mahjar rental-occupancy] A reservation occupies its unit for the booked days, corrected by what the handover desk
/// knows happened: from the day it was handed over when that came before the booked start; while it has not come back,
/// through today even when it is overdue; and a confirmed return frees the unit from the return day on. A rentable item
/// cannot be BOUGHT while such an occupancy lasts from today on: it is with a renter, or promised to one.
/// </summary>
public partial class RentalAvailabilityService : IRentalAvailabilityService
{
    #region Fields

    protected readonly IDateTimeHelper _dateTimeHelper;
    protected readonly IRepository<ConditionCheck> _conditionCheckRepository;
    protected readonly IRepository<Order> _orderRepository;
    protected readonly IRepository<OrderItem> _orderItemRepository;
    protected readonly IRepository<Shipment> _shipmentRepository;
    protected readonly IRepository<ShipmentItem> _shipmentItemRepository;

    #endregion

    #region Ctor

    public RentalAvailabilityService(IDateTimeHelper dateTimeHelper,
        IRepository<ConditionCheck> conditionCheckRepository,
        IRepository<Order> orderRepository,
        IRepository<OrderItem> orderItemRepository,
        IRepository<Shipment> shipmentRepository,
        IRepository<ShipmentItem> shipmentItemRepository)
    {
        _dateTimeHelper = dateTimeHelper;
        _conditionCheckRepository = conditionCheckRepository;
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _shipmentRepository = shipmentRepository;
        _shipmentItemRepository = shipmentItemRepository;
    }

    #endregion

    #region Utilities

    /// <summary>The days (inclusive, date-only) one live rental occupies its unit.</summary>
    protected record struct RentalOccupancy(int OrderItemId, DateTime Start, DateTime End, int Quantity);

    /// <summary>
    /// The store-local calendar day of a UTC moment. Rental dates are entered as store-local days (like a hotel), so
    /// "today", a handover and a return must be read on the same calendar — in KSA the UTC date lags three hours.
    /// </summary>
    protected virtual DateTime ToStoreDate(DateTime utc)
    {
        return _dateTimeHelper.ConvertToUserTime(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZoneInfo.Utc,
            _dateTimeHelper.DefaultStoreTimeZone).Date;
    }

    /// <summary>
    /// The occupancy of every live rental of a product (non-deleted, non-cancelled orders) — see the class summary
    /// for how the handover desk's records correct the booked days. [Mahjar rental-occupancy]
    /// </summary>
    protected virtual async Task<IList<RentalOccupancy>> GetOccupanciesAsync(int productId, int? excludeOrderItemId = null)
    {
        const int cancelledStatusId = (int)OrderStatus.Cancelled;
        const int deliveredShippingStatusId = (int)ShippingStatus.Delivered;
        const int deliveryTypeId = (int)ConditionCheckType.Delivery;
        const int returnTypeId = (int)ConditionCheckType.Return;
        const int confirmedStatusId = (int)ConditionCheckStatus.Confirmed;

        //scalars only: LinqToDB cannot filter a join that projects whole entities
        var query = from orderItem in _orderItemRepository.Table
                    join order in _orderRepository.Table on orderItem.OrderId equals order.Id
                    where orderItem.ProductId == productId
                          && orderItem.RentalStartDateUtc != null
                          && orderItem.RentalEndDateUtc != null
                          && !order.Deleted
                          && order.OrderStatusId != cancelledStatusId
                    select new
                    {
                        orderItem.Id,
                        Start = orderItem.RentalStartDateUtc.Value,
                        End = orderItem.RentalEndDateUtc.Value,
                        orderItem.Quantity,
                        order.ShippingStatusId
                    };

        if (excludeOrderItemId.HasValue)
        {
            var excludedId = excludeOrderItemId.Value;
            query = query.Where(row => row.Id != excludedId);
        }

        var reservations = await query.ToListAsync();
        if (reservations.Count == 0)
            return new List<RentalOccupancy>();

        //what the handover desk and the stock shipments recorded: when each item went out, and when it came back
        var itemIds = reservations.Select(reservation => reservation.Id).ToArray();
        var checks = await _conditionCheckRepository.Table
            .Where(check => itemIds.Contains(check.OrderItemId) &&
                            check.StatusId == confirmedStatusId &&
                            (check.CheckTypeId == deliveryTypeId || check.CheckTypeId == returnTypeId) &&
                            check.ConfirmedOnUtc != null)
            .Select(check => new { check.OrderItemId, check.CheckTypeId, ConfirmedOnUtc = check.ConfirmedOnUtc.Value })
            .ToListAsync();
        var deliveries = await (from shipmentItem in _shipmentItemRepository.Table
                                join shipment in _shipmentRepository.Table on shipmentItem.ShipmentId equals shipment.Id
                                where itemIds.Contains(shipmentItem.OrderItemId) && shipment.DeliveryDateUtc != null
                                select new { shipmentItem.OrderItemId, DeliveryDateUtc = shipment.DeliveryDateUtc.Value })
            .ToListAsync();

        var today = ToStoreDate(DateTime.UtcNow);
        var occupancies = new List<RentalOccupancy>();
        foreach (var reservation in reservations)
        {
            var start = reservation.Start.Date;
            var end = reservation.End.Date;

            var handedOverDates = checks
                .Where(check => check.OrderItemId == reservation.Id && check.CheckTypeId == deliveryTypeId)
                .Select(check => ToStoreDate(check.ConfirmedOnUtc))
                .Concat(deliveries.Where(delivery => delivery.OrderItemId == reservation.Id)
                    .Select(delivery => ToStoreDate(delivery.DeliveryDateUtc)))
                .ToList();
            var returnDates = checks
                .Where(check => check.OrderItemId == reservation.Id && check.CheckTypeId == returnTypeId)
                .Select(check => ToStoreDate(check.ConfirmedOnUtc))
                .ToList();
            var isOut = handedOverDates.Any() || reservation.ShippingStatusId == deliveredShippingStatusId;

            //out of the store from the day it was handed over, when that came before the booked start
            if (handedOverDates.Any() && handedOverDates.Min() < start)
                start = handedOverDates.Min();

            if (returnDates.Any())
                //back in the store: the unit is free again from the return day on (an early return frees the rest of the
                //booking, and a returned item can be bought or rented out the same day)
                end = returnDates.Min().AddDays(-1);
            else if (isOut && end < today)
                //overdue and still with the renter: it keeps the unit until it comes back
                end = today;

            if (end >= start)
                occupancies.Add(new RentalOccupancy(reservation.Id, start, end, reservation.Quantity));
        }

        return occupancies;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets the number of already-reserved units of a product that overlap the given date range.
    /// </summary>
    public virtual async Task<int> GetReservedUnitsAsync(int productId, DateTime startDate, DateTime endDate, int? excludeOrderItemId = null)
    {
        var from = startDate.Date;
        var to = endDate.Date;

        return (await GetOccupanciesAsync(productId, excludeOrderItemId))
            //inclusive overlap: the two ranges share at least one day
            .Where(occupancy => occupancy.Start <= to && from <= occupancy.End)
            .Sum(occupancy => occupancy.Quantity);
    }

    /// <summary>
    /// Gets the number of rentable units (capacity) of a product.
    /// </summary>
    public virtual int GetCapacity(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        return product.ManageInventoryMethod == ManageInventoryMethod.ManageStock
            ? Math.Max(product.StockQuantity, 0)
            : 1;
    }

    /// <summary>
    /// Indicates whether the requested quantity of a rental product is available for the given date range.
    /// </summary>
    public virtual async Task<bool> IsRangeAvailableAsync(Product product, DateTime startDate, DateTime endDate, int requestedQuantity, int? excludeOrderItemId = null)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (!product.IsRental)
            return true;

        var capacity = GetCapacity(product);
        var reserved = await GetReservedUnitsAsync(product.Id, startDate, endDate, excludeOrderItemId);

        return reserved + Math.Max(requestedQuantity, 1) <= capacity;
    }

    /// <summary>
    /// Gets the fully-booked (unavailable) date ranges of a rental product within the given window.
    /// </summary>
    public virtual async Task<IList<(DateTime Start, DateTime End)>> GetUnavailableRangesAsync(Product product, DateTime fromDate, DateTime toDate)
    {
        ArgumentNullException.ThrowIfNull(product);

        var result = new List<(DateTime, DateTime)>();

        if (!product.IsRental)
            return result;

        var capacity = GetCapacity(product);
        var windowStart = fromDate.Date;
        var windowEnd = toDate.Date;

        //no capacity at all => the whole window is unavailable
        if (capacity <= 0)
        {
            result.Add((windowStart, windowEnd));
            return result;
        }

        var occupancies = (await GetOccupanciesAsync(product.Id))
            .Where(occupancy => occupancy.Start <= windowEnd && windowStart <= occupancy.End)
            .ToList();

        if (occupancies.Count == 0)
            return result;

        //walk each day; a day is "booked" when overlapping reservations reach capacity. Merge runs into ranges.
        DateTime? rangeStart = null;
        var prevDay = windowStart;
        for (var day = windowStart; day <= windowEnd; day = day.AddDays(1))
        {
            var reservedOnDay = occupancies
                .Where(occupancy => occupancy.Start <= day && day <= occupancy.End)
                .Sum(occupancy => occupancy.Quantity);

            if (reservedOnDay >= capacity)
            {
                rangeStart ??= day;
                prevDay = day;
            }
            else if (rangeStart != null)
            {
                result.Add((rangeStart.Value, prevDay));
                rangeStart = null;
            }
        }

        if (rangeStart != null)
            result.Add((rangeStart.Value, prevDay));

        return result;
    }

    /// <summary>
    /// When the requested quantity of a rentable item cannot be bought now, because it is out with a renter or
    /// booked for a rental: the last occupied day. Null when it can be bought. [Mahjar rental-occupancy]
    /// </summary>
    public virtual async Task<DateTime?> GetPurchaseBlockedUntilAsync(Product product, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (!product.IsRental)
            return null;

        //a stock-managed item: every rental order already took its units out of stock when it was placed, so what is
        //left in stock is free to sell and the stock check alone decides (counting rentals again would refuse it)
        if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock)
            return null;

        var today = ToStoreDate(DateTime.UtcNow);
        var occupancies = (await GetOccupanciesAsync(product.Id))
            .Where(occupancy => occupancy.End >= today)
            .ToList();
        if (occupancies.Count == 0)
            return null;

        //a sold unit is gone for good, so it must be free on EVERY day from today on; overlapping rentals peak on the
        //first day of one of them (clamped to today)
        var peak = occupancies
            .Select(occupancy => occupancy.Start < today ? today : occupancy.Start)
            .Max(day => occupancies.Where(occupancy => occupancy.Start <= day && day <= occupancy.End).Sum(occupancy => occupancy.Quantity));
        if (peak + Math.Max(quantity, 1) <= GetCapacity(product))
            return null;

        return occupancies.Max(occupancy => occupancy.End);
    }

    #endregion
}
