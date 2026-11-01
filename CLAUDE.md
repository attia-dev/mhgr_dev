# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository layout (important)

The project lives directly at the root `D:\MHGR`:

- Visual Studio solution: `src\NopCommerce.sln`
- Web app: `src\Presentation\Nop.Web\Nop.Web.csproj`
- Documentation: `docs\`
- Deployment resources: `deploy\`

This is **not stock nopCommerce** — it is the **MHGR / محجر (Mahjar) marketplace**, a multi-vendor buy-or-rent store for KSA (Arabic-first/RTL, currency SAR), positioned as a luxury watch/jewelry store. Customizations were made by **editing core nopCommerce files directly** (not as a plugin); every such edit is tagged with a `[Mahjar ...]` comment. Grep `Mahjar` to find them all.

## Build / run / test

.NET 9 SDK (pinned via `global.json` to `9.0.100`, `latestFeature` roll-forward). Stop the running site before building — it locks the output DLLs.

```powershell
# Build (Release)
dotnet build "src\NopCommerce.sln" -c Release

# Run the web app (storefront, /admin, and /api/* mobile backend) on port 5000
dotnet run --project "src\Presentation\Nop.Web\Nop.Web.csproj" `
  -c Release --no-build --no-launch-profile --urls "http://localhost:5000"

# Tests (NUnit) — whole project or a single test
dotnet test "src\Tests\Nop.Tests\Nop.Tests.csproj" -c Debug
dotnet test "src\Tests\Nop.Tests\Nop.Tests.csproj" --filter "FullyQualifiedName~ShoppingCartServiceTests"
```

From **Visual Studio**, F5 launches `http://localhost:5000` (set in `Nop.Web/Properties/launchSettings.json`). Only one process can hold port 5000 — stop any CLI `dotnet run` before F5.

Front-end assets (Nop.Web) use **gulp** (`src/Presentation/Nop.Web/gulpfile.js`, `npm install` then `gulp`), but CSS/JS for most theme tweaks is served live via **WebOptimizer** bundles (see Gotchas).

### Local dev environment
- **DB:** SQL Server **LocalDB** `(localdb)\MSSQLLocalDB`, database `MhgrStore` (a stale, unused `nopCommerce` DB also exists on the instance — don't query it by mistake). Connection string in `src/Presentation/Nop.Web/App_Data/appsettings.json`.
- **Admin:** `admin@yourStore.com` / `Admin@2026`. **Demo seller:** `seller@mahjar.test` / `Seller@2026` (Vendors role, owns the buy/rent demo product). Password hash = `UPPERCASE_HEX(SHA512(UTF8(password+salt)))`.
- **SSL is disabled for dev** in the DB (`Store.SslEnabled=0`, `Store.Url='http://localhost:5000/'`). Re-enable with a real cert for prod.

## Architecture (big picture)

nopCommerce is a layered ASP.NET Core monolith with a custom IoC/engine and a plugin system. The four library/presentation layers (in `src/`):

- **`Libraries/Nop.Core`** — domain entities (`Domain/<area>/*.cs`, all derive from `BaseEntity`), configuration, caching abstractions, the `IEngine`/`NopEngine` + `Singleton<T>` + `ITypeFinder` infrastructure. No business logic.
- **`Libraries/Nop.Data`** — persistence via **LinqToDB** (not EF). `IRepository<T>`/`EntityRepository<T>`, mappings in `Mapping/`, and **FluentMigrator** schema migrations in `Migrations/`. Supports MS SQL / PostgreSQL / MySQL.
- **`Libraries/Nop.Services`** — all business logic, one folder per domain (`Catalog`, `Orders`, `Customers`, `Payments`, `Shipping`, `Tax`, …). Services are interface + impl pairs (`IFooService` / `FooService`), all methods `async`.
- **`Presentation/Nop.Web.Framework`** — shared MVC plumbing: `WebWorkContext`/`WebStoreContext`, theming, model factories base, security/anti-forgery, and the **startup pipeline** (`Infrastructure/Nop*Startup.cs`).
- **`Presentation/Nop.Web`** — the single web host: public storefront, the **Admin area** (`Areas/Admin/`), and the controllers/views. Uses a **Controller → ModelFactory → Service** pattern: controllers stay thin, `Factories/*ModelFactory.cs` build view models from services.

`Plugins/Nop.Plugin.*` are self-contained extensions (payment, shipping, tax, widgets, the Web API, etc.) discovered at runtime and copied into `Nop.Web/Plugins/`.

### Cross-cutting conventions — match these when adding code
- **Dependency injection.** Core services are registered in `src/Presentation/Nop.Web.Framework/Infrastructure/NopStartup.cs` (`ConfigureServices`, mostly `services.AddScoped<IFoo, Foo>()`). The two custom Mahjar services are registered here too (`IRentalAvailabilityService`, `IApiTokenService` — search `[Mahjar`). Startup classes implement `INopStartup` and run in `Order` sequence; plugins register their own via `IDependencyRegistrar`. Settings classes (`ISettings`) and event consumers (`IConsumer<>`) are auto-discovered by `ITypeFinder` — no manual registration.
- **Settings** are typed `ISettings` POCOs persisted as key/value rows (not appsettings.json), loaded per-store and **cached**.
- **Migrations:** add a FluentMigrator class under `Nop.Data/Migrations/`; it runs automatically on app start. Mahjar's live in `Nop.Data/Migrations/Mahjar/`.
- **Localization:** UI strings are DB locale-string resources (lowercase dotted keys, e.g. `products.purchasemode.buy`), not `.resx`. Add both `en` and `ar`.

## Mahjar / MHGR customizations (what makes this fork special)

All tagged `[Mahjar ...]` in code. Three feature areas:

1. **Buy OR rent the same item.** A product can be buy-only, rent-only, or both. New `Product` fields `RentalPrice`, `AvailableForPurchase`, `RentalSecurityDeposit` (`Nop.Core/Domain/Catalog/Product.cs`). A cart line is a rental **iff it carries rental dates** (no schema change). Pricing in `Nop.Services/Catalog/PriceCalculationService.cs`; cart rules in `Nop.Services/Orders/ShoppingCartService.cs`; product page toggle in `Nop.Web/Views/Product/_RentalInfo.cshtml`. Migrations: `BuyOrRentMigration.cs`, `RentalDepositMigration.cs`.
2. **Rental availability (no double-booking) + deposits.** `Nop.Services/Orders/IRentalAvailabilityService.cs` + impl: capacity = `StockQuantity` (if ManageStock) else 1; overlapping reserved units block a range at **add-to-cart and checkout**. Calendar greys booked days via `Nop.Web/Controllers/RentalController.cs` → route `rental/bookeddates/{productId}`. Refundable per-unit security deposit added after discounts (refund is manual, in admin).
3. **Mobile/headless REST API — full customer (buyer) app backend.** Bearer JWT (HS256, key derived in `Nop.Services/Authentication/ApiTokenService.cs`; per-customer epoch for logout/revocation). Controllers: `Nop.Web/Controllers/{Auth,Catalog,Cart,Checkout,Orders,Account,Wishlist,Reviews,Vendors,Reference}ApiController.cs`; routes in `Nop.Web/Infrastructure/RouteProvider.cs`. **Sellers have no API** — they manage listings via the web vendor panel by design. Routing trick: same URL + different HTTP verb = two `MapControllerRoute` with the same pattern disambiguated by `[HttpGet]/[HttpPut]/...`.

Branding/design (Arabic-first RTL, SAR, luxury dark theme) lives mostly in **DB-stored settings** (`seosettings.customheadtags` holds CSS/JS/fonts) and the `Themes/DefaultClean` theme, not in tracked source. The real logo artwork is installed at `src/Presentation/Nop.Web/Themes/DefaultClean/Content/images/logo.{png,svg}`.

## Gotchas

- **Settings & resources are cached.** After **direct DB edits** to settings/store/currency/language/resources/customer-roles/checkout-attributes, restart the app **or** `POST /Admin/Common/ClearCache` — otherwise changes won't show.
- **`customheadtags` gets clobbered.** Saving *any* settings in Admin flushes cached settings to the DB, overwriting direct DB edits to `customheadtags`. For durable CSS/logo overrides, append high-specificity `.header …` rules to the theme's `Content/css/styles.css` **and** `styles.rtl.css` (RTL is the active sheet) — WebOptimizer regenerates the bundle live on file save and cache-busts the `?v=` automatically (no restart). Static files like `logo.png` are served `max-age=1yr` → need a browser hard-refresh.
- **`styles.css` loads after `customheadtags`,** so color/background overrides placed in `customheadtags` need `!important`.
- **Plugins are not installed by the sample DB install** (`plugins.json` `InstalledPlugins` may be empty). Install needed ones (e.g. `Payments.CheckMoneyOrder`, `Shipping.FixedByWeightByTotal`, `Tax.FixedOrByCountryStateZip`) via Admin → Local plugins, then **restart** (they install on startup). Checkout hardcodes Check/Money-Order — no live payment gateway yet.
- **Arabic dates / culture:** the storefront posts rental dates as `dd‏/MM‏/yyyy` with a U+200F RTL mark (ar culture); the API accepts ISO dates.
- **Sandbox safety guard:** PowerShell scripts mixing `DELETE`/`Remove-Item`/`Get-Content` with the `\MSSQLLocalDB` or `d:\Work` paths are sometimes blocked (misread as file deletion). Prefer `SELECT`/`UPDATE`, split scripts, and clear the cart via the API rather than SQL `DELETE`.
