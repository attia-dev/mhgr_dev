using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.CheckMoneyOrder.Models;

public record ConfigurationModel : BaseNopModel, ILocalizedModel<ConfigurationModel.ConfigurationLocalizedModel>
{
    public ConfigurationModel()
    {
        Locales = new List<ConfigurationLocalizedModel>();
    }

    public int ActiveStoreScopeConfiguration { get; set; }

    [NopResourceDisplayName("Plugins.Payment.CheckMoneyOrder.DescriptionText")]
    public string DescriptionText { get; set; }
    public bool DescriptionText_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payment.CheckMoneyOrder.AdditionalFee")]
    public decimal AdditionalFee { get; set; }
    public bool AdditionalFee_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payment.CheckMoneyOrder.AdditionalFeePercentage")]
    public bool AdditionalFeePercentage { get; set; }
    public bool AdditionalFeePercentage_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Payment.CheckMoneyOrder.ShippableProductRequired")]
    public bool ShippableProductRequired { get; set; }
    public bool ShippableProductRequired_OverrideForStore { get; set; }

    //[Mahjar payments] treat finishing checkout with this (simulated) method as a completed payment
    [NopResourceDisplayName("Plugins.Payment.CheckMoneyOrder.MarkOrdersAsPaidAutomatically")]
    public bool MarkOrdersAsPaidAutomatically { get; set; }
    public bool MarkOrdersAsPaidAutomatically_OverrideForStore { get; set; }

    public IList<ConfigurationLocalizedModel> Locales { get; set; }

    #region Nested class

    public class ConfigurationLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [NopResourceDisplayName("Plugins.Payment.CheckMoneyOrder.DescriptionText")]
        public string DescriptionText { get; set; }
    }

    #endregion

}