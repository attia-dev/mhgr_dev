// [Mahjar item-status]
namespace Nop.Services.Mahjar.ItemStatus;

/// <summary>
/// The single source of truth for what an EXTERNAL condition-video URL is allowed to be. [Mahjar item-status]
///
/// <para>
/// The value is typed by a partner into a free-text admin field and ends up in an <c>&lt;iframe src&gt;</c> on the
/// public product page and in <c>ItemStatusDto.VideoUrl</c> handed to the mobile app. Razor encodes the attribute
/// but NOT the scheme, so a <c>javascript:</c> URL would run in the store's own origin for every visitor. Only
/// absolute http/https URLs are therefore accepted, and the length is capped at the width of the column
/// (<c>VideoUrl nvarchar(1000)</c>) so an over-long value is refused with a message instead of blowing up as a
/// truncation error in the middle of the product Edit POST.
/// </para>
///
/// <para>
/// It is deliberately used in THREE places — the admin write path and BOTH read paths (storefront factory and
/// mobile API) — so a row poisoned by a direct database edit or by an older build can never render either.
/// </para>
/// </summary>
public static partial class ItemStatusVideoUrlHelper
{
    #region Constants

    /// <summary>
    /// Maximum length of an external video URL — the width of ProductItemStatus.VideoUrl. [Mahjar item-status]
    /// </summary>
    public const int MAX_VIDEO_URL_LENGTH = 1000;

    #endregion

    #region Methods

    /// <summary>
    /// Checks whether an external video URL may be rendered to a customer. [Mahjar item-status]
    /// </summary>
    /// <param name="videoUrl">External video URL as stored on the item status row</param>
    /// <returns>True when the URL is an absolute http(s) URL of an acceptable length; otherwise false</returns>
    public static bool IsAllowed(string videoUrl)
    {
        return TryNormalize(videoUrl, out var normalizedVideoUrl) && !string.IsNullOrEmpty(normalizedVideoUrl);
    }

    /// <summary>
    /// Trims and validates an external video URL. An empty value is valid and normalises to null — a partner is
    /// allowed to have no external URL at all. [Mahjar item-status]
    /// </summary>
    /// <param name="videoUrl">External video URL as typed by the partner</param>
    /// <param name="normalizedVideoUrl">The trimmed URL, or null when there was none</param>
    /// <returns>True when the value may be stored/rendered; false when it must be refused</returns>
    public static bool TryNormalize(string videoUrl, out string normalizedVideoUrl)
    {
        normalizedVideoUrl = null;

        //no external url at all is perfectly valid: the partner may have uploaded a file instead, or neither
        if (string.IsNullOrWhiteSpace(videoUrl))
            return true;

        var trimmedVideoUrl = videoUrl.Trim();

        if (trimmedVideoUrl.Length > MAX_VIDEO_URL_LENGTH)
            return false;

        if (!Uri.TryCreate(trimmedVideoUrl, UriKind.Absolute, out var uri))
            return false;

        //http(s) only: javascript:, data:, vbscript: and file: all execute or leak in an iframe
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        normalizedVideoUrl = trimmedVideoUrl;

        return true;
    }

    #endregion
}
