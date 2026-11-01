// [Mahjar vendor-role]
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Mahjar;
using Nop.Core.Domain.Vendors;
using Nop.Services.Customers;
using Nop.Services.Events;
using Nop.Services.Logging;

namespace Nop.Services.Mahjar.Vendors;

/// <summary>
/// Keeps the customer accounts of a partner (vendor) in step with its status, so the admin never has to do it by
/// hand: approving (activating) a partner gives its linked accounts the built-in "Vendors" role, deactivating it
/// takes the role away. The removal matters: nopCommerce treats an inactive partner as no partner at all
/// (IWorkContext.GetCurrentVendorAsync returns null), so a "Vendors" member whose partner is inactive is no longer
/// scoped to their own products in the partner panel. Deleting a partner is handled in the admin VendorController.
/// Auto-discovered by ITypeFinder. [Mahjar vendor-role]
/// </summary>
public partial class VendorRoleEventConsumer :
    IConsumer<VendorApprovedEvent>,
    IConsumer<VendorDeactivatedEvent>
{
    #region Fields

    protected readonly ICustomerService _customerService;
    protected readonly ILogger _logger;

    #endregion

    #region Ctor

    public VendorRoleEventConsumer(ICustomerService customerService,
        ILogger logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    #endregion

    #region Utilities

    /// <summary>Gets the built-in "Vendors" role, logging when it is missing (it is a system role, so it never should be).</summary>
    protected virtual async Task<CustomerRole> GetVendorsRoleAsync(Vendor vendor)
    {
        var role = await _customerService.GetCustomerRoleBySystemNameAsync(NopCustomerDefaults.VendorsRoleName);
        if (role == null)
            await _logger.WarningAsync($"Partner #{vendor.Id} '{vendor.Name}': the \"{NopCustomerDefaults.VendorsRoleName}\" customer role was not found, so the partner's accounts were not updated.");

        return role;
    }

    #endregion

    #region Methods

    /// <summary>Admin approved (activated) a partner → its linked accounts join the "Vendors" role.</summary>
    public virtual async Task HandleEventAsync(VendorApprovedEvent eventMessage)
    {
        var vendor = eventMessage.Vendor;
        if (vendor == null || !vendor.Active || vendor.Deleted)
            return;

        var role = await GetVendorsRoleAsync(vendor);
        if (role == null)
            return;

        foreach (var customer in await _customerService.GetAllCustomersAsync(vendorId: vendor.Id))
        {
            //nopCommerce never lets an administrator be a partner (it unlinks them), so never touch an admin's roles
            if (await _customerService.IsAdminAsync(customer, false))
                continue;

            if (await _customerService.IsInCustomerRoleAsync(customer, NopCustomerDefaults.VendorsRoleName, false))
                continue;

            await _customerService.AddCustomerRoleMappingAsync(new CustomerCustomerRoleMapping
            {
                CustomerId = customer.Id,
                CustomerRoleId = role.Id
            });
        }
    }

    /// <summary>Admin deactivated (suspended) a partner → its linked accounts leave the "Vendors" role.</summary>
    public virtual async Task HandleEventAsync(VendorDeactivatedEvent eventMessage)
    {
        var vendor = eventMessage.Vendor;
        if (vendor == null)
            return;

        var role = await GetVendorsRoleAsync(vendor);
        if (role == null)
            return;

        //the accounts stay linked to the partner, so approving it again gives the role back
        foreach (var customer in await _customerService.GetAllCustomersAsync(vendorId: vendor.Id))
        {
            if (await _customerService.IsInCustomerRoleAsync(customer, NopCustomerDefaults.VendorsRoleName, false))
                await _customerService.RemoveCustomerRoleMappingAsync(customer, role);
        }
    }

    #endregion
}
