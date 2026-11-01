using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Services.Catalog;
using Nop.Services.Orders;

namespace Nop.Web.Controllers;

/// <summary>
/// Exposes rental availability to the storefront calendar. [Mahjar buy-or-rent]
/// </summary>
public partial class RentalController : BasePublicController
{
    #region Fields

    protected readonly IProductService _productService;
    protected readonly IRentalAvailabilityService _rentalAvailabilityService;

    #endregion

    #region Ctor

    public RentalController(IProductService productService,
        IRentalAvailabilityService rentalAvailabilityService)
    {
        _productService = productService;
        _rentalAvailabilityService = rentalAvailabilityService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Returns the fully-booked date ranges for a rental product so the date picker can disable them.
    /// </summary>
    public virtual async Task<IActionResult> BookedDates(int productId)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        if (product == null || product.Deleted || !product.IsRental)
            return Json(new { ranges = Array.Empty<object>() });

        var from = DateTime.UtcNow.Date;
        var to = from.AddMonths(12);
        var ranges = await _rentalAvailabilityService.GetUnavailableRangesAsync(product, from, to);

        return Json(new
        {
            ranges = ranges.Select(r => new { from = r.Start.ToString("yyyy-MM-dd"), to = r.End.ToString("yyyy-MM-dd") })
        });
    }

    #endregion
}
