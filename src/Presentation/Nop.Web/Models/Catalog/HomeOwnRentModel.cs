// [Mahjar home-motion]
using Nop.Web.Framework.Models;

namespace Nop.Web.Models.Catalog;

/// <summary>
/// The home "Own or Rent" editorial band: one real watch that can be bought and one that can be rented, each
/// with the category listing it belongs to. A tile is null when no home-page product qualifies. [Mahjar home-motion]
/// </summary>
public partial record HomeOwnRentModel : BaseNopModel
{
    public TileModel Own { get; set; }

    public TileModel Rent { get; set; }

    /// <summary>One tile of the band. [Mahjar home-motion]</summary>
    public partial record TileModel : BaseNopModel
    {
        public string ProductName { get; set; }

        public string ProductUrl { get; set; }

        public string PictureUrl { get; set; }

        /// <summary>The purchase price (Own) or the rental rate (Rent: "120 SAR / day" for a by-the-day rental)</summary>
        public string PriceText { get; set; }

        /// <summary>True when the rented product carries a refundable security deposit</summary>
        public bool HasDeposit { get; set; }

        /// <summary>The product's category — a small secondary "Browse …" link (the tile itself leads to the watch); null = none</summary>
        public string CategoryName { get; set; }

        public string CategoryUrl { get; set; }
    }
}
