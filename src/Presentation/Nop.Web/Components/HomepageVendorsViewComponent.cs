using Microsoft.AspNetCore.Mvc;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Seo;
using Nop.Services.Vendors;
using Nop.Web.Framework.Components;
using Nop.Web.Models.Catalog;

namespace Nop.Web.Components;

/// <summary>
/// "Shop by Seller" home-page section — a paginated grid of active seller (vendor) cards shown before the
/// categories. The current page comes from the "sellerpage" query-string value so the pager links can be
/// plain page reloads (?sellerpage=N), and is clamped to the valid range. [Mahjar shop-by-seller]
/// </summary>
public partial class HomepageVendorsViewComponent : NopViewComponent
{
    //sellers per page; the numbered pager only appears once there are more sellers than this.
    //the grid wraps (flex-wrap), so this fills several per row across multiple rows (e.g. ~6/row × 2 rows on desktop).
    protected const int SELLERS_PER_PAGE = 12;

    protected readonly ILocalizationService _localizationService;
    protected readonly IPictureService _pictureService;
    protected readonly IUrlRecordService _urlRecordService;
    protected readonly IVendorService _vendorService;

    public HomepageVendorsViewComponent(ILocalizationService localizationService,
        IPictureService pictureService,
        IUrlRecordService urlRecordService,
        IVendorService vendorService)
    {
        _localizationService = localizationService;
        _pictureService = pictureService;
        _urlRecordService = urlRecordService;
        _vendorService = vendorService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var page = 1;
        if (int.TryParse(Request.Query["sellerpage"].ToString(), out var requestedPage) && requestedPage > 0)
            page = requestedPage;

        //GetAllVendorsAsync with showHidden=false returns only active, non-deleted vendors (paged)
        var vendors = await _vendorService.GetAllVendorsAsync(pageIndex: page - 1, pageSize: SELLERS_PER_PAGE);

        //no sellers at all -> render nothing
        if (vendors.TotalCount == 0)
            return Content(string.Empty);

        //clamp an out-of-range page (e.g. a stale/over-incremented ?sellerpage=) to the last real page
        if (page > vendors.TotalPages)
        {
            page = vendors.TotalPages;
            vendors = await _vendorService.GetAllVendorsAsync(pageIndex: page - 1, pageSize: SELLERS_PER_PAGE);
        }

        var model = new HomepageVendorsModel
        {
            PageIndex = page,
            TotalPages = vendors.TotalPages
        };

        foreach (var vendor in vendors)
        {
            //seller picture/logo — empty when the seller has no picture (showDefaultPicture: false) → view falls back to the icon
            var pictureUrl = vendor.PictureId > 0
                ? await _pictureService.GetPictureUrlAsync(vendor.PictureId, targetSize: 240, showDefaultPicture: false)
                : null;

            model.Vendors.Add(new HomepageVendorsModel.VendorItemModel
            {
                Id = vendor.Id,
                Name = await _localizationService.GetLocalizedAsync(vendor, x => x.Name),
                SeName = await _urlRecordService.GetSeNameAsync(vendor),
                PictureUrl = pictureUrl
            });
        }

        return View(model);
    }
}
