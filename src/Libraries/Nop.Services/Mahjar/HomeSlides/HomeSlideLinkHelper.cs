// [Mahjar home-slides]
using System;
using System.Linq;

namespace Nop.Services.Mahjar.HomeSlides;

/// <summary>
/// The ONE link rule of the home slides, shared by the admin validator, the storefront view component and the mobile
/// API, so the three can never disagree: a slide link (a Custom-link target or a video) is either a site-relative
/// path ("/…") or an absolute http(s) url. [Mahjar home-slides]
/// </summary>
public static partial class HomeSlideLinkHelper
{
    /// <summary>A host that only serves to prove a relative path stays on the store's own host.</summary>
    private static readonly Uri _probeBase = new("http://store.invalid/");

    /// <summary>
    /// Normalizes a slide link. Returns the trimmed site-relative path ("/…") or the absolute http(s) url (re-emitted
    /// as <see cref="Uri.AbsoluteUri"/>); null for anything else: empty, javascript: / data: / vbscript:, the
    /// protocol-relative "//host", the "/\host" browser quirk, and any value holding a control character or a
    /// backslash. Browsers silently drop a TAB / CR / LF inside a url, so "/&lt;TAB&gt;/evil.example" would otherwise
    /// pass a prefix check and still open "//evil.example" — an off-site link dressed up as a store path.
    /// </summary>
    /// <param name="value">The link as stored or typed</param>
    /// <param name="isRelative">True when the result is a site-relative path</param>
    /// <returns>The safe link, or null</returns>
    public static string NormalizeLink(string value, out bool isRelative)
    {
        isRelative = false;
        var link = value?.Trim();
        if (string.IsNullOrEmpty(link) || link.Any(char.IsControl) || link.Contains('\\'))
            return null;

        if (link.StartsWith('/'))
        {
            if (link.StartsWith("//"))
                return null;

            //belt and braces: resolved against a probe host, the path must still point at that very host
            if (!Uri.TryCreate(_probeBase, link, out var resolved)
                || !string.Equals(resolved.Host, _probeBase.Host, StringComparison.OrdinalIgnoreCase))
                return null;

            isRelative = true;
            return link;
        }

        return Uri.TryCreate(link, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri.AbsoluteUri
            : null;
    }

    /// <summary>
    /// Gets a value indicating whether a link is safe to store and render (see <see cref="NormalizeLink"/>)
    /// </summary>
    /// <param name="value">The link as typed</param>
    public static bool IsSafeLink(string value)
    {
        return NormalizeLink(value, out _) != null;
    }
}
