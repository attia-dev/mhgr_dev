using Nop.Web.Framework.Models;

namespace Nop.Web.Models.Catalog;

/// <summary>
/// "Shop by Seller" home-page section: a paginated grid of seller (vendor) cards. [Mahjar shop-by-seller]
/// </summary>
public partial record HomepageVendorsModel : BaseNopModel
{
    public IList<VendorItemModel> Vendors { get; set; } = new List<VendorItemModel>();

    /// <summary>1-based index of the page currently shown.</summary>
    public int PageIndex { get; set; }

    /// <summary>Total number of pages (a pager is shown only when this is greater than 1).</summary>
    public int TotalPages { get; set; }

    public partial record VendorItemModel : BaseNopEntityModel
    {
        public string Name { get; set; }
        public string SeName { get; set; }

        /// <summary>The seller's picture/logo URL; null/empty when the seller has no picture (falls back to the icon). [Mahjar]</summary>
        public string PictureUrl { get; set; }
    }
}
