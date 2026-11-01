// [Mahjar item-status]
using Nop.Core.Configuration;

namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// Settings for the per-product condition report ("item status"). Auto-discovered/auto-registered as an
/// <see cref="ISettings"/> by ITypeFinder — never registered in DI. The C# defaults below apply until the rows
/// seeded by ItemStatusMigration (<c>itemstatussettings.*</c>) are edited in admin. [Mahjar item-status]
/// </summary>
public partial class ItemStatusSettings : ISettings
{
    /// <summary>Whether the condition report is available at all (admin editor + storefront/API section).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>The largest video file, in megabytes, a partner may upload for one product.</summary>
    public int MaxVideoSizeMb { get; set; } = 100;

    /// <summary>The largest number of condition photos allowed on one product.</summary>
    public int MaxPictures { get; set; } = 12;

    /// <summary>
    /// Comma-separated list of accepted video file extensions. Keep it to formats the static-file MIME provider
    /// actually serves — <c>.mkv</c> and <c>.m3u8</c> are NOT served and must stay out of this list.
    /// </summary>
    public string AllowedVideoExtensions { get; set; } = ".mp4,.webm,.mov,.m4v";
}
