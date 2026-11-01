using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MHGR.Api.Contracts;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;

namespace Nop.Web.Controllers;

/// <summary>
/// Turns the app's flat list of selected attributes into nopCommerce AttributesXml so attributed products (size,
/// strap, color, …) can be added to the cart/wishlist through the mobile API exactly like the website. [Mahjar mobile-api]
/// </summary>
public static class ApiAttributeHelper
{
    public static async Task<string> BuildAttributesXmlAsync(
        IProductAttributeService attributeService,
        IProductAttributeParser attributeParser,
        Product product,
        List<SelectedAttributeApi> selected)
    {
        var attributesXml = string.Empty;
        var mappings = await attributeService.GetProductAttributeMappingsByProductIdAsync(product.Id);
        if (mappings == null || mappings.Count == 0)
            return attributesXml;

        foreach (var mapping in mappings)
        {
            // read-only checkboxes are server-controlled — always add their values, no user input
            if (mapping.AttributeControlType == AttributeControlType.ReadonlyCheckboxes)
            {
                foreach (var value in await attributeService.GetProductAttributeValuesAsync(mapping.Id))
                    attributesXml = attributeParser.AddProductAttribute(attributesXml, mapping, value.Id.ToString());
                continue;
            }

            var sel = selected?.FirstOrDefault(s => s.AttributeMappingId == mapping.Id);
            if (sel == null)
                continue;

            switch (mapping.AttributeControlType)
            {
                case AttributeControlType.DropdownList:
                case AttributeControlType.RadioList:
                case AttributeControlType.ColorSquares:
                case AttributeControlType.ImageSquares:
                    var one = sel.ValueIds?.FirstOrDefault(v => v > 0) ?? 0;
                    if (one > 0)
                        attributesXml = attributeParser.AddProductAttribute(attributesXml, mapping, one.ToString());
                    break;

                case AttributeControlType.Checkboxes:
                    foreach (var v in (sel.ValueIds ?? new List<int>()).Where(v => v > 0))
                        attributesXml = attributeParser.AddProductAttribute(attributesXml, mapping, v.ToString());
                    break;

                case AttributeControlType.TextBox:
                case AttributeControlType.MultilineTextbox:
                case AttributeControlType.Datepicker:
                    if (!string.IsNullOrWhiteSpace(sel.Text))
                        attributesXml = attributeParser.AddProductAttribute(attributesXml, mapping, sel.Text.Trim());
                    break;

                // FileUpload is not supported by the mobile app
            }
        }

        return attributesXml;
    }
}
