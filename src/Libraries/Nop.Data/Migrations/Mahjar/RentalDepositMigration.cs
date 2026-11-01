using FluentMigrator;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Adds <see cref="Product.RentalSecurityDeposit"/> — a refundable security deposit charged per unit
/// when an item is rented. [Mahjar rental-deposit]
/// </summary>
[NopSchemaMigration("2026-06-05 13:00:00", "Mahjar rental deposit: add Product.RentalSecurityDeposit")]
public class RentalDepositMigration : ForwardOnlyMigration
{
    public override void Up()
    {
        var productTable = nameof(Product);

        if (!Schema.Table(productTable).Column(nameof(Product.RentalSecurityDeposit)).Exists())
        {
            Alter.Table(productTable)
                .AddColumn(nameof(Product.RentalSecurityDeposit))
                .AsDecimal(18, 4)
                .NotNullable()
                .WithDefaultValue(0);
        }
    }
}
