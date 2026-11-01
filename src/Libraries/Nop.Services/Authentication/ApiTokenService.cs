using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Security;
using Nop.Services.Common;
using Nop.Services.Customers;

namespace Nop.Services.Authentication;

/// <summary>
/// Standard JWT (HS256) bearer tokens signed with a key derived from the store's server key and validated
/// in-process. Tokens carry a per-customer epoch ("ver") so logout can revoke them server-side. [Mahjar mobile-api]
/// </summary>
public partial class ApiTokenService : IApiTokenService
{
    #region Constants

    //per-customer generic attribute holding the current token epoch; bumping it invalidates all prior tokens
    public const string TokenEpochAttribute = "MahjarApiTokenEpoch";

    #endregion

    #region Fields

    protected readonly byte[] _key;
    protected readonly ICustomerService _customerService;
    protected readonly IGenericAttributeService _genericAttributeService;

    #endregion

    #region Ctor

    public ApiTokenService(SecuritySettings securitySettings,
        ICustomerService customerService,
        IGenericAttributeService genericAttributeService)
    {
        _customerService = customerService;
        _genericAttributeService = genericAttributeService;

        //never sign with a hard-coded/known secret. Use the store's encryption key (always set on install); if it is
        //somehow empty, fall back to a per-process random key (tokens won't survive a restart, but are never forgeable).
        var secret = securitySettings.EncryptionKey;
        if (string.IsNullOrWhiteSpace(secret))
            secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

        //derive a dedicated 256-bit HS256 signing key (separate from the raw encryption key) [Mahjar mobile-api]
        _key = SHA256.HashData(Encoding.UTF8.GetBytes("MHGR.api.jwt.v1::" + secret));
    }

    #endregion

    #region Utilities

    protected static string Base64Url(byte[] data)
    {
        return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    protected static byte[] Base64UrlDecode(string value)
    {
        value = value.Replace('-', '+').Replace('_', '/');
        switch (value.Length % 4)
        {
            case 2: value += "=="; break;
            case 3: value += "="; break;
        }
        return Convert.FromBase64String(value);
    }

    protected byte[] Sign(string signingInput)
    {
        using var hmac = new HMACSHA256(_key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(signingInput));
    }

    //validate signature + expiry, then extract the subject (customer id) and epoch (ver, 0 if absent)
    protected virtual bool TryReadToken(string token, out int customerId, out long tokenVersion)
    {
        customerId = 0;
        tokenVersion = 0;
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var parts = token.Split('.');
        if (parts.Length != 3)
            return false;

        var signingInput = $"{parts[0]}.{parts[1]}";
        var expected = Sign(signingInput);

        byte[] provided;
        try { provided = Base64UrlDecode(parts[2]); }
        catch { return false; }

        if (!CryptographicOperations.FixedTimeEquals(expected, provided))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(Base64UrlDecode(parts[1])));
            var root = doc.RootElement;
            var exp = root.GetProperty("exp").GetInt64();
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp)
                return false;
            customerId = root.GetProperty("sub").GetInt32();
            if (root.TryGetProperty("ver", out var verElement))
                tokenVersion = verElement.GetInt64();
            return customerId > 0;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Methods

    public virtual string CreateToken(int customerId, string email, long tokenVersion = 0)
    {
        var header = Base64Url(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
        var exp = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds();
        var payload = Base64Url(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { sub = customerId, email, ver = tokenVersion, exp })));
        var signingInput = $"{header}.{payload}";
        var signature = Base64Url(Sign(signingInput));
        return $"{signingInput}.{signature}";
    }

    public virtual bool TryGetCustomerId(string token, out int customerId)
    {
        return TryReadToken(token, out customerId, out _);
    }

    public virtual async Task<long> GetCurrentTokenEpochAsync(Customer customer)
    {
        if (customer == null)
            return 0;
        return await _genericAttributeService.GetAttributeAsync<long>(customer, TokenEpochAttribute);
    }

    public virtual async Task<Customer> GetAuthenticatedCustomerAsync(string authorizationHeader)
    {
        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        var token = authorizationHeader["Bearer ".Length..].Trim();
        if (!TryReadToken(token, out var customerId, out var tokenVersion))
            return null;

        var customer = await _customerService.GetCustomerByIdAsync(customerId);
        if (customer == null || customer.Deleted || !customer.Active)
            return null;

        //revocation check: the token's epoch must still match the customer's current epoch
        var currentEpoch = await _genericAttributeService.GetAttributeAsync<long>(customer, TokenEpochAttribute);
        if (tokenVersion != currentEpoch)
            return null;

        return customer;
    }

    public virtual async Task RevokeTokensAsync(Customer customer)
    {
        if (customer == null)
            return;
        var currentEpoch = await _genericAttributeService.GetAttributeAsync<long>(customer, TokenEpochAttribute);
        await _genericAttributeService.SaveAttributeAsync(customer, TokenEpochAttribute, currentEpoch + 1);
    }

    #endregion
}
