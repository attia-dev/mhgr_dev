using FluentMigrator;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Adds the fields required for the "buy OR rent the same item" feature: a separate rental rate
/// per period (<see cref="Product.RentalPrice"/>) and a flag for whether the item can be bought
/// outright (<see cref="Product.AvailableForPurchase"/>). A product can therefore be available
/// for purchase, for rent (<see cref="Product.IsRental"/>), or both. [Mahjar buy-or-rent]
/// </summary>
[NopSchemaMigration("2026-06-05 12:00:00", "Mahjar buy-or-rent: add Product.RentalPrice and Product.AvailableForPurchase")]
public class BuyOrRentMigration : ForwardOnlyMigration
{
    public override void Up()
    {
        var productTable = nameof(Product);

        //rental rate per period (0 => fall back to the purchase Price, preserving existing rentals)
        if (!Schema.Table(productTable).Column(nameof(Product.RentalPrice)).Exists())
        {
            Alter.Table(productTable)
                .AddColumn(nameof(Product.RentalPrice))
                .AsDecimal(18, 4)
                .NotNullable()
                .WithDefaultValue(0);
        }

        //whether the item can be purchased outright (enables buy-only, rent-only, or both)
        if (!Schema.Table(productTable).Column(nameof(Product.AvailableForPurchase)).Exists())
        {
            Alter.Table(productTable)
                .AddColumn(nameof(Product.AvailableForPurchase))
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(true);

            //preserve current behavior: existing rental products were rent-only (not separately buyable)
            Update.Table(productTable)
                .Set(new { AvailableForPurchase = false })
                .Where(new { IsRental = true });
        }
    }
}
