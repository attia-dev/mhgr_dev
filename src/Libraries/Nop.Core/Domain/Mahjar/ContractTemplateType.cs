// [Mahjar contracts]
namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// The kind of contract a <see cref="ContractTemplate"/> covers. A contract request is rendered with the
/// template whose type matches the frozen cart: an all-purchase cart gets <see cref="Purchase"/>, an
/// all-rental cart gets <see cref="Rental"/>, and a cart mixing both gets <see cref="Mixed"/>.
/// [Mahjar contracts]
/// </summary>
public enum ContractTemplateType
{
    /// <summary>Every line in the cart is bought outright — no rental wording.</summary>
    Purchase = 10,

    /// <summary>Every line in the cart is rented — rental period, return and security-deposit wording.</summary>
    Rental = 20,

    /// <summary>The cart mixes bought and rented lines — combined sales/rental wording.</summary>
    Mixed = 30
}
