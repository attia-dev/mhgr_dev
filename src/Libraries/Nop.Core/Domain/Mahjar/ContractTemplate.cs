// [Mahjar contracts]
using Nop.Core.Domain.Localization;

namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// An admin-editable, localized, tokenized contract template. One row per <see cref="ContractTemplateType"/>
/// (purchase / rental / mixed) is seeded by the migrations; the Body is localized via
/// <see cref="LocalizedProperty"/> and rendered per request with the %Contract.*% tokens, picking the row whose
/// <see cref="TemplateType"/> matches the request's frozen cart. [Mahjar contracts]
/// </summary>
public partial class ContractTemplate : BaseEntity, ILocalizedEntity
{
    /// <summary>The template name.</summary>
    public string Name { get; set; }

    /// <summary>The kind discriminator (backing value for <see cref="TemplateType"/>).</summary>
    public int TemplateTypeId { get; set; }

    /// <summary>The tokenized HTML body (localized).</summary>
    public string Body { get; set; }

    /// <summary>Last update date (UTC).</summary>
    public DateTime UpdatedOnUtc { get; set; }

    /// <summary>Typed wrapper over <see cref="TemplateTypeId"/>.</summary>
    public ContractTemplateType TemplateType
    {
        get => (ContractTemplateType)TemplateTypeId;
        set => TemplateTypeId = (int)value;
    }
}
