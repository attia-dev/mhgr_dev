// [Mahjar partner-request]
using Nop.Core;
using Nop.Core.Domain.Mahjar.Agreements;
using Nop.Core.Domain.Vendors;

namespace Nop.Services.Mahjar.Agreements;

/// <summary>
/// [Mahjar partner-request] The queue of partner applications waiting for the store owner, and the two
/// decisions they end in. A partner created directly in admin is not an application and never appears here.
/// </summary>
public partial interface IPartnerRequestService
{
    /// <summary>
    /// Searches the applications, newest first
    /// </summary>
    /// <param name="status">Only applications in this state; null for every state</param>
    /// <param name="keyword">Matches the partner name or e-mail; null or empty for all</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>A task that represents the asynchronous operation; the page of applications</returns>
    Task<IPagedList<Vendor>> SearchRequestsAsync(PartnerApplicationStatus? status = null, string keyword = null,
        int pageIndex = 0, int pageSize = int.MaxValue);

    /// <summary>
    /// Gets the most recent rejected application of a customer, so the application form can tell them why and
    /// let them correct it. It is found through <see cref="Vendor.ApplicationCustomerId"/>, which survives the
    /// rejection clearing <c>Customer.VendorId</c>.
    /// </summary>
    /// <param name="customerId">Customer identifier</param>
    /// <returns>A task that represents the asynchronous operation; the application, or null</returns>
    Task<Vendor> GetLatestRejectedRequestAsync(int customerId);

    /// <summary>
    /// Approves an application: the partner is activated, which grants the customer behind it the Vendors role
    /// and notifies them, through the same event the partner page publishes.
    /// </summary>
    /// <param name="vendor">The application</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task ApproveAsync(Vendor vendor);

    /// <summary>
    /// Rejects an application. The record is kept, with its reason and the agreement copy, and the customer
    /// behind it is released so they can apply again.
    /// </summary>
    /// <param name="vendor">The application</param>
    /// <param name="reason">Why it was rejected; shown to the applicant</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task RejectAsync(Vendor vendor, string reason);
}
