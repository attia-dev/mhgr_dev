using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.CheckMoneyOrder;

/// <summary>
/// Represents settings of "Check money order" payment plugin
/// </summary>
public class CheckMoneyOrderPaymentSettings : ISettings
{
    /// <summary>
    /// Gets or sets a description text
    /// </summary>
    public string DescriptionText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
    /// </summary>
    public bool AdditionalFeePercentage { get; set; }

    /// <summary>
    /// Gets or sets an additional fee
    /// </summary>
    public decimal AdditionalFee { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether shippable products are required in order to display this payment method during checkout
    /// </summary>
    public bool ShippableProductRequired { get; set; }

    /// <summary>
    /// [Mahjar payments] Gets or sets a value indicating whether an order paid with this method is marked as paid the
    /// moment it is placed. This method stands in for a real gateway while the store has none, so the customer
    /// finishing checkout is treated as a completed payment: the order becomes Paid, the customer is notified, and the
    /// handover desk gets its task without an admin pressing "Mark as paid". Turn it off when a real gateway is added.
    /// </summary>
    public bool MarkOrdersAsPaidAutomatically { get; set; } = true;
}