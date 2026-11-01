using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Media;
using Nop.Web.Factories;
using Nop.Web.Framework.Components;
using Nop.Web.Models.Catalog;

namespace Nop.Web.Components;

public partial class HomepageCategoriesViewComponent : NopViewComponent
{
    //[Mahjar home-motion] the ViewData entry holding the tile stand-in pictures (category id -> picture url)
    public const string MAHJAR_FALLBACK_IMAGES_KEY = "Mahjar.HomeCategories.FallbackImages";

    protected readonly ICatalogModelFactory _catalogModelFactory;
    //[Mahjar app-home] the stand-in picture rule lives in the factory shared with the app's api/home/categories
    protected readonly IHomeCategoryModelFactory _homeCategoryModelFactory;
    protected readonly MediaSettings _mediaSettings;

    public HomepageCategoriesViewComponent(ICatalogModelFactory catalogModelFactory,
        IHomeCategoryModelFactory homeCategoryModelFactory, //[Mahjar app-home]
        MediaSettings mediaSettings)
    {
        _catalogModelFactory = catalogModelFactory;
        _homeCategoryModelFactory = homeCategoryModelFactory;
        _mediaSettings = mediaSettings;
    }

    /// <summary>
    /// Gets a value indicating whether a home category tile has a picture of its own (not the "no image" placeholder)
    /// [Mahjar home-motion]
    /// </summary>
    public static bool HasOwnImage(CategoryModel item) => !string.IsNullOrEmpty(item?.PictureModel?.ImageUrl)
        && !item.PictureModel.ImageUrl.Contains("default-image", StringComparison.OrdinalIgnoreCase);

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = await _catalogModelFactory.PrepareHomepageCategoryModelsAsync();
        if (!model.Any())
            return Content("");

        //[Mahjar home-motion] a real watch for a tile without a picture of its own — [Mahjar app-home] chosen and
        //cached by the factory the app shares, so the website tile and the app card always show the same watch
        ViewData[MAHJAR_FALLBACK_IMAGES_KEY] = await _homeCategoryModelFactory.PrepareFallbackPictureUrlsAsync(
            model.Select(item => (item.Id, HasOwnImage(item) ? item.PictureModel.ImageUrl : null)).ToList(),
            _mediaSettings.CategoryThumbPictureSize);

        return View(model);
    }
}
