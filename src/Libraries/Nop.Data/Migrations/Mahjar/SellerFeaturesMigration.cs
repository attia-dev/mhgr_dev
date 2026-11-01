using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Backs the two seller-facing storefront features:
///  1. the bilingual labels for the "Shop by Seller" home-page section and the seller name shown on product
///     boxes, plus the admin toggle (VendorSettings.ShowVendorOnCatalogPages, default ON), and
///  2. assigns every ORPHAN product (VendorId = 0) to one of the active sellers — distributed round-robin —
///     so the seller name appears across the catalog and the "Shop by Seller" grid is populated. Products
///     that already belong to a seller are left untouched, so real ownership is never reassigned.
///
/// Idempotent (resources/setting inserted only when missing; orphan-only assignment leaves nothing to do on
/// re-run), matched by stable flags so it is deploy-safe, and a schema migration so it auto-applies on startup.
/// SQL Server dialect. [Mahjar shop-by-seller] [Mahjar seller-on-product]
/// </summary>
[NopSchemaMigration("2026-06-14 15:00:00", "Mahjar seller features: shop-by-seller resources + distribute products across active vendors")]
public class SellerFeaturesMigration : ForwardOnlyMigration
{
    public override void Up()
    {
        //1) bilingual UI strings
        Execute.Sql(@"
INSERT INTO LocaleStringResource (LanguageId, ResourceName, ResourceValue)
SELECT l.Id, v.ResourceName,
       CASE WHEN (l.UniqueSeoCode = 'ar' OR l.LanguageCulture LIKE 'ar%') THEN v.ArValue ELSE v.EnValue END
FROM Language l
CROSS JOIN (VALUES
    ('mahjar.product.seller',          N'البائع',                  N'Seller'),
    ('mahjar.home.shopbyseller',       N'تسوّق حسب البائع',        N'Shop by Seller'),
    ('mahjar.home.shopbyseller.sub',   N'تصفّح منتجاتنا حسب البائع', N'Browse our trusted sellers')
) AS v (ResourceName, ArValue, EnValue)
WHERE NOT EXISTS (
    SELECT 1 FROM LocaleStringResource r
    WHERE r.LanguageId = l.Id AND r.ResourceName = v.ResourceName
);");

        //2) make the seller line on product boxes admin-toggleable, defaulting to ON
        Execute.Sql(@"
INSERT INTO [Setting] ([Name], [Value], [StoreId])
SELECT 'VendorSettings.ShowVendorOnCatalogPages', 'True', 0
WHERE NOT EXISTS (
    SELECT 1 FROM [Setting] WHERE [Name] = 'VendorSettings.ShowVendorOnCatalogPages'
);");

        //3) give every ORPHAN product (no seller yet) one of the active sellers, distributed round-robin.
        //   Products that already belong to a seller are left untouched — real ownership is never reassigned.
        Execute.Sql(@"
WITH av AS (
    SELECT Id,
           ROW_NUMBER() OVER (ORDER BY DisplayOrder, Id) - 1 AS rn,
           COUNT(*) OVER () AS cnt
    FROM Vendor
    WHERE Active = 1 AND Deleted = 0
),
pr AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) - 1 AS rn
    FROM Product
    WHERE Deleted = 0 AND VendorId = 0
)
UPDATE p
SET p.VendorId = av.Id
FROM Product p
JOIN pr ON pr.Id = p.Id
JOIN av ON av.rn = (pr.rn % av.cnt);");
    }
}
