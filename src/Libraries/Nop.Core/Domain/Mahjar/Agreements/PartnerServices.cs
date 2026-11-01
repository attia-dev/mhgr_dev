// [Mahjar partner-agreement]
namespace Nop.Core.Domain.Mahjar.Agreements;

/// <summary>
/// [Mahjar partner-agreement] The services a partner activated on the platform — the "الخدمات المفعلة
/// ☐ التأجير ☐ البيع ☐ كلاهما" row that both partner agreements carry in their header and in Annex A.
/// A service annex of the agreement binds the partner only while the matching service is activated, which is
/// why this is a flags enum and not a single choice: <see cref="Rental"/> | <see cref="Sale"/> is "كلاهما".
/// </summary>
[Flags]
public enum PartnerServices
{
    /// <summary>
    /// Nothing activated — the state of every partner who applied before the agreement gate existed
    /// </summary>
    None = 0,

    /// <summary>
    /// Renting pieces out (Annex B of the agreement applies)
    /// </summary>
    Rental = 1,

    /// <summary>
    /// Selling pieces (Annex C of the agreement applies)
    /// </summary>
    Sale = 2
}
