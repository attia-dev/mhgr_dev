// [Mahjar partner-request]
using Nop.Core;
using Nop.Core.Events;
using Nop.Core.Domain.Mahjar;
using Nop.Core.Domain.Mahjar.Agreements;
using Nop.Core.Domain.Vendors;
using Nop.Data;
using Nop.Services.Customers;
using Nop.Services.Vendors;

namespace Nop.Services.Mahjar.Agreements;

/// <summary>
/// [Mahjar partner-request] The partner application queue — see <see cref="IPartnerRequestService"/>.
/// </summary>
public partial class PartnerRequestService : IPartnerRequestService
{
    #region Fields

    protected readonly ICustomerService _customerService;
    protected readonly IEventPublisher _eventPublisher;
    protected readonly IRepository<Vendor> _vendorRepository;
    protected readonly IVendorService _vendorService;

    #endregion

    #region Ctor

    public PartnerRequestService(ICustomerService customerService,
        IEventPublisher eventPublisher,
        IRepository<Vendor> vendorRepository,
        IVendorService vendorService)
    {
        _customerService = customerService;
        _eventPublisher = eventPublisher;
        _vendorRepository = vendorRepository;
        _vendorService = vendorService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Searches the applications, newest first
    /// </summary>
    public virtual async Task<IPagedList<Vendor>> SearchRequestsAsync(PartnerApplicationStatus? status = null,
        string keyword = null, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        return await _vendorRepository.GetAllPagedAsync(query =>
        {
            //a partner created straight in admin is not an application
            query = query.Where(vendor => !vendor.Deleted &&
                vendor.ApplicationStatusId != (int)PartnerApplicationStatus.NotAnApplication);

            if (status.HasValue)
                query = query.Where(vendor => vendor.ApplicationStatusId == (int)status.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var term = keyword.Trim();
                query = query.Where(vendor => vendor.Name.Contains(term) || vendor.Email.Contains(term));
            }

            //newest first; the identity order is the tie-break for rows whose date was back-filled
            return query.OrderByDescending(vendor => vendor.ApplicationSubmittedOnUtc).ThenByDescending(vendor => vendor.Id);
        }, pageIndex, pageSize);
    }

    /// <summary>
    /// Gets the most recent rejected application of a customer
    /// </summary>
    public virtual async Task<Vendor> GetLatestRejectedRequestAsync(int customerId)
    {
        if (customerId <= 0)
            return null;

        var requests = await _vendorRepository.GetAllAsync(query => query
            .Where(vendor => !vendor.Deleted &&
                vendor.ApplicationCustomerId == customerId &&
                vendor.ApplicationStatusId == (int)PartnerApplicationStatus.Rejected)
            .OrderByDescending(vendor => vendor.ApplicationReviewedOnUtc)
            .ThenByDescending(vendor => vendor.Id));

        return requests.FirstOrDefault();
    }

    /// <summary>
    /// Approves an application
    /// </summary>
    public virtual async Task ApproveAsync(Vendor vendor)
    {
        ArgumentNullException.ThrowIfNull(vendor);

        //the role consumer reads Active off the instance it is handed and the notification consumer is not
        //idempotent, so: capture the previous state, save first, and publish only on a real transition —
        //exactly what the partner page does when an admin ticks "active" there
        var wasActive = vendor.Active;

        vendor.Active = true;
        vendor.ApplicationStatus = PartnerApplicationStatus.Approved;
        vendor.ApplicationReviewedOnUtc = DateTime.UtcNow;
        vendor.ApplicationRejectionReason = null;

        await _vendorService.UpdateVendorAsync(vendor);

        if (!wasActive)
            await _eventPublisher.PublishAsync(new VendorApprovedEvent(vendor));
    }

    /// <summary>
    /// Rejects an application
    /// </summary>
    public virtual async Task RejectAsync(Vendor vendor, string reason)
    {
        ArgumentNullException.ThrowIfNull(vendor);

        var wasActive = vendor.Active;

        vendor.Active = false;
        vendor.ApplicationStatus = PartnerApplicationStatus.Rejected;
        vendor.ApplicationReviewedOnUtc = DateTime.UtcNow;
        vendor.ApplicationRejectionReason = reason?.Trim();

        await _vendorService.UpdateVendorAsync(vendor);

        //an approved partner being rejected afterwards has to lose the role it was granted
        if (wasActive)
            await _eventPublisher.PublishAsync(new VendorDeactivatedEvent(vendor));

        //release the customer so they can apply again. The application keeps ApplicationCustomerId, so the
        //record still says whose it was and the form can still show them why it was refused
        foreach (var customer in await _customerService.GetAllCustomersAsync(vendorId: vendor.Id))
        {
            customer.VendorId = 0;
            await _customerService.UpdateCustomerAsync(customer);
        }

        //...and tell them, with the reason. Published after the release on purpose: the consumer resolves the
        //recipient through ApplicationCustomerId, which survives it, so the order cannot silently drop the
        //notification the way a VendorId lookup would
        await _eventPublisher.PublishAsync(new VendorApplicationRejectedEvent(vendor));
    }

    #endregion
}
