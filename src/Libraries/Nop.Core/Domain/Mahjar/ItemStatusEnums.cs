// [Mahjar item-status]
namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// The overall condition grade a partner (or the admin) gives a listed piece. Values are explicit and gapped so
/// new grades can be slotted between the existing ones without renumbering stored rows. Labels come from the
/// standard nopCommerce enum resources (<c>enums.nop.core.domain.mahjar.itemconditiongrade.*</c>), so the admin
/// dropdown and the storefront/API read one source. [Mahjar item-status]
/// </summary>
public enum ItemConditionGrade
{
    /// <summary>No grade chosen yet — the condition report shows no grade chip.</summary>
    NotSpecified = 0,

    /// <summary>Brand new, unused, as it left the manufacturer.</summary>
    BrandNew = 10,

    /// <summary>Like new — barely used, no visible marks.</summary>
    LikeNew = 20,

    /// <summary>Excellent — light traces of use only.</summary>
    Excellent = 30,

    /// <summary>Good — visible but ordinary traces of use.</summary>
    Good = 40,

    /// <summary>Fair — clear wear or an old repair, fully disclosed in the notes.</summary>
    Fair = 50
}

/// <summary>
/// What a single condition note is about. Drives the note icon/label on the storefront and in the app.
/// Values are explicit and gapped. [Mahjar item-status]
/// </summary>
public enum ItemStatusNoteType
{
    /// <summary>A general remark that fits none of the other kinds.</summary>
    General = 10,

    /// <summary>Actual damage — a crack, a dent, a deep scratch.</summary>
    Damage = 20,

    /// <summary>Ordinary traces of use — light scratches, patina, a worn strap.</summary>
    Wear = 30,

    /// <summary>A previous repair or service performed on the piece.</summary>
    Repair = 40,

    /// <summary>A part that is missing from the piece.</summary>
    MissingPart = 50,

    /// <summary>What ships with the piece — box, papers, extra links, a spare strap.</summary>
    Accessory = 60
}

/// <summary>
/// How serious a condition note is. Drives the note colour on the storefront and in the app
/// (Info = muted, Minor = olive, Moderate = brass, Major = warm clay). Values are explicit and gapped.
/// [Mahjar item-status]
/// </summary>
public enum ItemStatusNoteSeverity
{
    /// <summary>Informational only — nothing wrong with the piece.</summary>
    Info = 10,

    /// <summary>Minor — barely noticeable, no effect on use.</summary>
    Minor = 20,

    /// <summary>Moderate — clearly visible, still no effect on use.</summary>
    Moderate = 30,

    /// <summary>Major — significant; the buyer must read this before ordering.</summary>
    Major = 40
}
