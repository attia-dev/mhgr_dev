// [Mahjar home-slides]
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Configuration;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Mahjar;
using Nop.Core.Domain.Vendors;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Services.Vendors;

namespace Nop.Services.Mahjar.HomeSlides;

/// <summary>
/// The admin-managed home slides service. The storefront/app read path loads the whole (small) table once into
/// the static cache under <see cref="NopHomeSlideDefaults.HomeSlidesAllCacheKey"/> — cleared by the HomeSlide
/// cache event consumer on every insert / update / delete — and filters placement, channel and the schedule
/// window in memory, so a slide goes live or expires on time without any cache invalidation. [Mahjar home-slides]
/// </summary>
public partial class HomeSlideService : IHomeSlideService
{
    #region Fields

    protected readonly IAclService _aclService;
    protected readonly ICategoryService _categoryService;
    protected readonly IManufacturerService _manufacturerService;
    protected readonly IPictureService _pictureService;
    protected readonly IProductService _productService;
    protected readonly IStoreMappingService _storeMappingService;
    protected readonly IVendorService _vendorService;
    protected readonly IRepository<Category> _categoryRepository;
    protected readonly IRepository<ConditionCheckPicture> _conditionCheckPictureRepository;
    protected readonly IRepository<GenericAttribute> _genericAttributeRepository;
    protected readonly IRepository<HomeSlide> _homeSlideRepository;
    protected readonly IRepository<Manufacturer> _manufacturerRepository;
    protected readonly IRepository<ProductAttributeCombinationPicture> _combinationPictureRepository;
    protected readonly IRepository<ProductAttributeValue> _attributeValueRepository;
    protected readonly IRepository<ProductAttributeValuePicture> _attributeValuePictureRepository;
    protected readonly IRepository<ProductItemStatusPicture> _itemStatusPictureRepository;
    protected readonly IRepository<ProductPicture> _productPictureRepository;
    protected readonly IRepository<Setting> _settingRepository;
    protected readonly IRepository<Vendor> _vendorRepository;

    #endregion

    #region Ctor

    public HomeSlideService(IAclService aclService,
        ICategoryService categoryService,
        IManufacturerService manufacturerService,
        IPictureService pictureService,
        IProductService productService,
        IStoreMappingService storeMappingService,
        IVendorService vendorService,
        IRepository<Category> categoryRepository,
        IRepository<ConditionCheckPicture> conditionCheckPictureRepository,
        IRepository<GenericAttribute> genericAttributeRepository,
        IRepository<HomeSlide> homeSlideRepository,
        IRepository<Manufacturer> manufacturerRepository,
        IRepository<ProductAttributeCombinationPicture> combinationPictureRepository,
        IRepository<ProductAttributeValue> attributeValueRepository,
        IRepository<ProductAttributeValuePicture> attributeValuePictureRepository,
        IRepository<ProductItemStatusPicture> itemStatusPictureRepository,
        IRepository<ProductPicture> productPictureRepository,
        IRepository<Setting> settingRepository,
        IRepository<Vendor> vendorRepository)
    {
        _aclService = aclService;
        _categoryService = categoryService;
        _manufacturerService = manufacturerService;
        _pictureService = pictureService;
        _productService = productService;
        _storeMappingService = storeMappingService;
        _vendorService = vendorService;
        _categoryRepository = categoryRepository;
        _conditionCheckPictureRepository = conditionCheckPictureRepository;
        _genericAttributeRepository = genericAttributeRepository;
        _homeSlideRepository = homeSlideRepository;
        _manufacturerRepository = manufacturerRepository;
        _combinationPictureRepository = combinationPictureRepository;
        _attributeValueRepository = attributeValueRepository;
        _attributeValuePictureRepository = attributeValuePictureRepository;
        _itemStatusPictureRepository = itemStatusPictureRepository;
        _productPictureRepository = productPictureRepository;
        _settingRepository = settingRepository;
        _vendorRepository = vendorRepository;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets the home slides (admin list), ordered by placement, display order then identifier
    /// </summary>
    /// <param name="placement">Placement; null to load every placement</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the home slides
    /// </returns>
    public virtual async Task<IPagedList<HomeSlide>> SearchHomeSlidesAsync(HomeSlidePlacement? placement = null,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        return await _homeSlideRepository.GetAllPagedAsync(query =>
        {
            if (placement.HasValue)
            {
                var placementId = (int)placement.Value;
                query = query.Where(slide => slide.PlacementId == placementId);
            }

            return query.OrderBy(slide => slide.PlacementId).ThenBy(slide => slide.DisplayOrder).ThenBy(slide => slide.Id);
        }, pageIndex, pageSize);
    }

    /// <summary>
    /// Gets a home slide
    /// </summary>
    /// <param name="homeSlideId">Home slide identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the home slide, or null when it does not exist
    /// </returns>
    public virtual async Task<HomeSlide> GetHomeSlideByIdAsync(int homeSlideId)
    {
        return await _homeSlideRepository.GetByIdAsync(homeSlideId, cache => default);
    }

    /// <summary>
    /// Inserts a home slide (stamps CreatedOnUtc and UpdatedOnUtc)
    /// </summary>
    /// <param name="homeSlide">Home slide</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task InsertHomeSlideAsync(HomeSlide homeSlide)
    {
        ArgumentNullException.ThrowIfNull(homeSlide);

        homeSlide.CreatedOnUtc = DateTime.UtcNow;
        homeSlide.UpdatedOnUtc = homeSlide.CreatedOnUtc;

        await _homeSlideRepository.InsertAsync(homeSlide);
    }

    /// <summary>
    /// Updates a home slide (stamps UpdatedOnUtc)
    /// </summary>
    /// <param name="homeSlide">Home slide</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task UpdateHomeSlideAsync(HomeSlide homeSlide)
    {
        ArgumentNullException.ThrowIfNull(homeSlide);

        homeSlide.UpdatedOnUtc = DateTime.UtcNow;

        await _homeSlideRepository.UpdateAsync(homeSlide);
    }

    /// <summary>
    /// Deletes a home slide (hard delete), then its picture and mobile picture — each only when nothing else uses
    /// it (the seed slides share product pictures)
    /// </summary>
    /// <param name="homeSlide">Home slide</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual async Task DeleteHomeSlideAsync(HomeSlide homeSlide)
    {
        ArgumentNullException.ThrowIfNull(homeSlide);

        var pictureIds = new[] { homeSlide.PictureId, homeSlide.MobilePictureId }
            .Where(pictureId => pictureId > 0)
            .Distinct()
            .ToList();

        //the row goes first, so the in-use check below no longer sees this slide as a user of its own pictures
        await _homeSlideRepository.DeleteAsync(homeSlide);

        foreach (var pictureId in pictureIds)
            await DeletePictureIfUnusedAsync(pictureId);
    }

    /// <summary>
    /// Gets the slides to show now: published, with a picture, enabled for the channel and inside their schedule
    /// window, ordered by display order then identifier
    /// </summary>
    /// <param name="placement">Placement</param>
    /// <param name="channel">The asking surface (web store or mobile app)</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the active home slides
    /// </returns>
    public virtual async Task<IList<HomeSlide>> GetActiveHomeSlidesAsync(HomeSlidePlacement placement, HomeSlideChannel channel)
    {
        //the whole table is cached (it holds a handful of rows); the time window is NEVER part of the cached query
        var allSlides = await _homeSlideRepository.GetAllAsync(
            query => query.OrderBy(slide => slide.DisplayOrder).ThenBy(slide => slide.Id),
            cache => cache.PrepareKeyForDefaultCache(NopHomeSlideDefaults.HomeSlidesAllCacheKey));

        var placementId = (int)placement;
        var nowUtc = DateTime.UtcNow;

        return allSlides
            .Where(slide => slide.Published
                && slide.PictureId > 0
                && slide.PlacementId == placementId
                && (channel == HomeSlideChannel.App ? slide.ShowOnApp : slide.ShowOnWeb)
                && (!slide.StartDateUtc.HasValue || slide.StartDateUtc.Value <= nowUtc)
                && (!slide.EndDateUtc.HasValue || slide.EndDateUtc.Value >= nowUtc))
            .OrderBy(slide => slide.DisplayOrder)
            .ThenBy(slide => slide.Id)
            .ToList();
    }

    /// <summary>
    /// Gets the entity a slide links to (its product, category, manufacturer or vendor), but only while a customer can
    /// still open it — the same rules as the storefront pages themselves: it exists and is not deleted; a product is
    /// published, allowed by ACL and store mapping and inside its availability dates; a category / manufacturer is
    /// published and allowed by ACL and store mapping; a vendor is active. The storefront and the mobile API both
    /// drop the link of a slide this returns null for, so a sold-and-unpublished watch never leaves a dead button.
    /// Every lookup is served from the entity / ACL caches.
    /// </summary>
    /// <param name="slide">Home slide</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the live target entity; null for a gone or hidden target and for the other link kinds
    /// (none, search, custom link)
    /// </returns>
    public virtual async Task<BaseEntity> GetLiveTargetEntityAsync(HomeSlide slide)
    {
        if (slide == null || slide.TargetId <= 0)
            return null;

        switch (slide.TargetType)
        {
            case HomeSlideTargetType.Product:
                var product = await _productService.GetProductByIdAsync(slide.TargetId);
                if (product == null || product.Deleted || !product.Published
                    || !await _aclService.AuthorizeAsync(product) || !await _storeMappingService.AuthorizeAsync(product)
                    || !_productService.ProductIsAvailable(product))
                    return null;
                return product;

            case HomeSlideTargetType.Category:
                var category = await _categoryService.GetCategoryByIdAsync(slide.TargetId);
                if (category == null || category.Deleted || !category.Published
                    || !await _aclService.AuthorizeAsync(category) || !await _storeMappingService.AuthorizeAsync(category))
                    return null;
                return category;

            case HomeSlideTargetType.Manufacturer:
                var manufacturer = await _manufacturerService.GetManufacturerByIdAsync(slide.TargetId);
                if (manufacturer == null || manufacturer.Deleted || !manufacturer.Published
                    || !await _aclService.AuthorizeAsync(manufacturer) || !await _storeMappingService.AuthorizeAsync(manufacturer))
                    return null;
                return manufacturer;

            case HomeSlideTargetType.Vendor:
                var vendor = await _vendorService.GetVendorByIdAsync(slide.TargetId);
                return vendor == null || vendor.Deleted || !vendor.Active ? null : vendor;

            default:
                return null;
        }
    }

    /// <summary>
    /// Gets a value indicating whether anything references a picture: a product gallery, an attribute value image
    /// (color square or gallery) or a combination picture, a category / manufacturer / vendor image, a condition
    /// photo, a customer avatar, the store or PDF logo, a Swiper widget slide, or any home slide in the table (as
    /// its picture or mobile picture). The admin accepts only a picture nothing uses (a fresh upload) as a slide's
    /// new picture, and a slide picture is deleted only when this is false.
    /// </summary>
    /// <param name="pictureId">Picture identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result is true when the picture is in use
    /// </returns>
    public virtual async Task<bool> IsPictureInUseAsync(int pictureId)
    {
        if (pictureId <= 0)
            return false;

        if (await _productPictureRepository.Table.AnyAsync(mapping => mapping.PictureId == pictureId)
            || await _homeSlideRepository.Table.AnyAsync(slide => slide.PictureId == pictureId || slide.MobilePictureId == pictureId)
            || await _categoryRepository.Table.AnyAsync(category => category.PictureId == pictureId)
            || await _manufacturerRepository.Table.AnyAsync(manufacturer => manufacturer.PictureId == pictureId)
            || await _vendorRepository.Table.AnyAsync(vendor => vendor.PictureId == pictureId)
            || await _itemStatusPictureRepository.Table.AnyAsync(photo => photo.PictureId == pictureId)
            || await _conditionCheckPictureRepository.Table.AnyAsync(photo => photo.PictureId == pictureId)
            || await _attributeValueRepository.Table.AnyAsync(value => value.ImageSquaresPictureId == pictureId)
            || await _attributeValuePictureRepository.Table.AnyAsync(mapping => mapping.PictureId == pictureId)
            || await _combinationPictureRepository.Table.AnyAsync(mapping => mapping.PictureId == pictureId))
            return true;

        //references stored as text: a customer avatar (generic attribute), the store and PDF logos (settings, any
        //store) and the Swiper widget slides (one setting holding a JSON list of {"PictureId":n,...})
        var idText = pictureId.ToString(CultureInfo.InvariantCulture);
        var customerKeyGroup = nameof(Customer);
        var avatarKey = NopCustomerDefaults.AvatarPictureIdAttribute;
        var swiperToken = "\"PictureId\":" + idText + ",";
        var swiperLastToken = "\"PictureId\":" + idText + "}";

        return await _genericAttributeRepository.Table.AnyAsync(attribute => attribute.KeyGroup == customerKeyGroup
                && attribute.Key == avatarKey && attribute.Value == idText)
            || await _settingRepository.Table.AnyAsync(setting =>
                ((setting.Name == "storeinformationsettings.logopictureid" || setting.Name == "pdfsettings.logopictureid") && setting.Value == idText)
                || (setting.Name == "swipersettings.slides" && (setting.Value.Contains(swiperToken) || setting.Value.Contains(swiperLastToken))));
    }

    /// <summary>
    /// Deletes a picture a home slide no longer uses (a replaced or removed slide picture) — but only when nothing
    /// else still references it (see <see cref="IsPictureInUseAsync"/>)
    /// </summary>
    /// <param name="pictureId">Picture identifier; 0 is ignored</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result is true when the picture row was deleted
    /// </returns>
    public virtual async Task<bool> DeletePictureIfUnusedAsync(int pictureId)
    {
        if (pictureId <= 0 || await IsPictureInUseAsync(pictureId))
            return false;

        var picture = await _pictureService.GetPictureByIdAsync(pictureId);
        if (picture == null)
            return false;

        await _pictureService.DeletePictureAsync(picture);

        return true;
    }

    #endregion
}
