using Nop.Core.Configuration;

namespace Nop.Core.Domain.Vendors;

/// <summary>
/// Vendor settings
/// </summary>
public partial class VendorSettings : ISettings
{
    /// <summary>
    /// Gets or sets the default value to use for Vendor page size options (for new vendors)
    /// </summary>
    public string DefaultVendorPageSizeOptions { get; set; }

    /// <summary>
    /// Gets or sets the value indicating how many vendors to display in vendors block
    /// </summary>
    public int VendorsBlockItemsToDisplay { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to display vendor name on the product details page
    /// </summary>
    public bool ShowVendorOnProductDetailsPage { get; set; }

    /// <summary>
    /// [Mahjar seller-on-product] Gets or sets a value indicating whether to display the seller/vendor name
    /// on product boxes in catalog listings (category, search, home page, etc.)
    /// </summary>
    public bool ShowVendorOnCatalogPages { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to display vendor name on the order details page
    /// </summary>
    public bool ShowVendorOnOrderDetailsPage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether customers can contact vendors
    /// </summary>
    public bool AllowCustomersToContactVendors { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether users can fill a form to become a new vendor
    /// </summary>
    public bool AllowCustomersToApplyForVendorAccount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether vendors have to accept terms of service during registration
    /// </summary>
    public bool TermsOfServiceEnabled { get; set; }

    /// <summary>
    /// [Mahjar vendor-application] Gets or sets the partner terms and conditions shown on the application form
    /// (HTML). Edited from Admin -> Settings -> Vendor settings, per language (localized setting), and served
    /// to the mobile app through GET /api/customer/apply-vendor. Only used when
    /// <see cref="TermsOfServiceEnabled"/> is on.
    /// </summary>
    public string TermsOfService { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether it is possible to carry out advanced search in the store by vendor
    /// </summary>
    public bool AllowSearchByVendor { get; set; }

    /// <summary>
    /// Get or sets a value indicating whether vendor can edit information about itself (public store)
    /// </summary>
    public bool AllowVendorsToEditInfo { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the store owner is notified that the vendor information has been changed
    /// </summary>
    public bool NotifyStoreOwnerAboutVendorInformationChange { get; set; }

    /// <summary>
    /// Gets or sets a maximum number of products per vendor
    /// </summary>
    public int MaximumProductNumber { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether vendors are allowed to import products
    /// </summary>
    public bool AllowVendorsToImportProducts { get; set; }
}