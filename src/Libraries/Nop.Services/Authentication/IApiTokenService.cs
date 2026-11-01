using System.Threading.Tasks;
using Nop.Core.Domain.Customers;

namespace Nop.Services.Authentication;

/// <summary>
/// Issues and validates bearer tokens (JWT HS256) for the mobile / headless API. Tokens carry a per-customer
/// "epoch" so they can be revoked server-side (logout) by bumping the epoch. [Mahjar mobile-api]
/// </summary>
public partial interface IApiTokenService
{
    /// <summary>
    /// Creates a signed bearer token for a customer, stamped with the given token epoch (version).
    /// </summary>
    string CreateToken(int customerId, string email, long tokenVersion = 0);

    /// <summary>
    /// Validates a bearer token (signature + expiry) and extracts the customer id. Does NOT check revocation.
    /// </summary>
    bool TryGetCustomerId(string token, out int customerId);

    /// <summary>
    /// Gets the customer's current token epoch (0 if none). Stamp new tokens with this value.
    /// </summary>
    Task<long> GetCurrentTokenEpochAsync(Customer customer);

    /// <summary>
    /// Resolves the customer from an "Authorization: Bearer ..." header: validates signature + expiry, checks the
    /// account is active, and verifies the token's epoch still matches (not revoked). Returns null otherwise.
    /// </summary>
    Task<Customer> GetAuthenticatedCustomerAsync(string authorizationHeader);

    /// <summary>
    /// Revokes every token previously issued to the customer by bumping their token epoch (server-side logout).
    /// </summary>
    Task RevokeTokensAsync(Customer customer);
}
