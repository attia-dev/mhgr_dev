using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Models.Media;
using Nop.Web.Models.ShoppingCart;

namespace Nop.Web.Models.Catalog;

public partial record ProductDetailsModel : BaseNopEntityModel
{
    public ProductDetailsModel()
    {
        DefaultPictureModel = new PictureModel();
        PictureModels = new List<PictureModel>();
        VideoModels = new List<VideoModel>();
        GiftCard = new GiftCardModel();
        ProductPrice = new ProductPriceModel();
        AddToCart = new AddToCartModel();
        ProductAttributes = new List<ProductAttributeModel>();
        AssociatedProducts = new List<ProductDetailsModel>();
        VendorModel = new VendorBriefInfoModel();
        Breadcrumb = new ProductBreadcrumbModel();
        ProductTags = new List<ProductTagModel>();
        ProductSpecificationModel = new ProductSpecificationModel();
        ProductManufacturers = new List<ManufacturerBriefInfoModel>();
        ProductReviewOverview = new ProductReviewOverviewModel();
        ProductReviews = new ProductReviewsModel();
        TierPrices = new List<TierPriceModel>();
        ProductEstimateShipping = new ProductEstimateShippingModel();
        ItemStatus = new ItemStatusModel(); //[Mahjar item-status]
    }

    //picture(s)
    public bool DefaultPictureZoomEnabled { get; set; }
    public PictureModel DefaultPictureModel { get; set; }
    public IList<PictureModel> PictureModels { get; set; }

    //videos
    public IList<VideoModel> VideoModels { get; set; }

    public string Name { get; set; }
    public string ShortDescription { get; set; }
    public string FullDescription { get; set; }
    public string JsonLd { get; set; }
    public string MetaKeywords { get; set; }
    public string MetaDescription { get; set; }
    public string MetaTitle { get; set; }
    public string SeName { get; set; }
    public bool VisibleIndividually { get; set; }

    public ProductType ProductType { get; set; }

    public bool ShowSku { get; set; }
    public string Sku { get; set; }

    public bool ShowManufacturerPartNumber { get; set; }
    public string ManufacturerPartNumber { get; set; }

    public bool ShowGtin { get; set; }
    public string Gtin { get; set; }

    public bool ShowVendor { get; set; }
    public VendorBriefInfoModel VendorModel { get; set; }

    public bool HasSampleDownload { get; set; }

    public GiftCardModel GiftCard { get; set; }

    public bool IsShipEnabled { get; set; }
    public bool IsFreeShipping { get; set; }
    public bool FreeShippingNotificationEnabled { get; set; }
    public string DeliveryDate { get; set; }

    public bool IsRental { get; set; }

    //whether the item can also be bought outright (drives the buy/rent toggle on the product page). [Mahjar buy-or-rent]
    public bool AvailableForPurchase { get; set; }

    //refundable rental deposit, formatted (empty when none). [Mahjar rental-deposit]
    public string RentalSecurityDeposit { get; set; }

    //the condition report shown on the product page (never null; guard on ItemStatus.HasAny). [Mahjar item-status]
    public ItemStatusModel ItemStatus { get; set; }

    public DateTime? RentalStartDate { get; set; }
    public DateTime? RentalEndDate { get; set; }

    public DateTime? AvailableEndDate { get; set; }

    public ManageInventoryMethod ManageInventoryMethod { get; set; }

    public string StockAvailability { get; set; }

    public bool DisplayBackInStockSubscription { get; set; }

    public bool DisplayAttributeCombinationImagesOnly { get; set; }

    public bool EmailAFriendEnabled { get; set; }
    public bool CompareProductsEnabled { get; set; }

    public string PageShareCode { get; set; }

    public ProductPriceModel ProductPrice { get; set; }

    public AddToCartModel AddToCart { get; set; }

    public ProductBreadcrumbModel Breadcrumb { get; set; }

    public IList<ProductTagModel> ProductTags { get; set; }

    public IList<ProductAttributeModel> ProductAttributes { get; set; }

    public ProductSpecificationModel ProductSpecificationModel { get; set; }

    public IList<ManufacturerBriefInfoModel> ProductManufacturers { get; set; }

    public ProductReviewOverviewModel ProductReviewOverview { get; set; }

    public ProductReviewsModel ProductReviews { get; set; }

    public ProductEstimateShippingModel ProductEstimateShipping { get; set; }

    public IList<TierPriceModel> TierPrices { get; set; }

    //a list of associated products. For example, "Grouped" products could have several child "simple" products
    public IList<ProductDetailsModel> AssociatedProducts { get; set; }

    public bool DisplayDiscontinuedMessage { get; set; }

    public string CurrentStoreName { get; set; }

    public bool InStock { get; set; }

    public bool AllowAddingOnlyExistingAttributeCombinations { get; set; }

    #region Nested Classes

    public partial record ProductBreadcrumbModel : BaseNopModel
    {
        public ProductBreadcrumbModel()
        {
            CategoryBreadcrumb = new List<CategorySimpleModel>();
        }

        public bool Enabled { get; set; }
        public string JsonLd { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductSeName { get; set; }
        public IList<CategorySimpleModel> CategoryBreadcrumb { get; set; }
    }

    public partial record AddToCartModel : BaseNopModel
    {
        public AddToCartModel()
        {
            AllowedQuantities = new List<SelectListItem>();
        }
        public int ProductId { get; set; }

        //qty
        [NopResourceDisplayName("Products.Qty")]
        public int EnteredQuantity { get; set; }
        public string MinimumQuantityNotification { get; set; }
        public List<SelectListItem> AllowedQuantities { get; set; }

        //price entered by customers
        [NopResourceDisplayName("Products.EnterProductPrice")]
        public bool CustomerEntersPrice { get; set; }
        [NopResourceDisplayName("Products.EnterProductPrice")]
        public decimal CustomerEnteredPrice { get; set; }
        public string CustomerEnteredPriceRange { get; set; }

        public bool DisableBuyButton { get; set; }
        public bool DisableWishlistButton { get; set; }

        //rental
        public bool IsRental { get; set; }

        //pre-order
        public bool AvailableForPreOrder { get; set; }
        public DateTime? PreOrderAvailabilityStartDateTimeUtc { get; set; }
        public string PreOrderAvailabilityStartDateTimeUserTime { get; set; }

        //updating existing shopping cart or wishlist item?
        public int UpdatedShoppingCartItemId { get; set; }
        public ShoppingCartType? UpdateShoppingCartItemType { get; set; }
    }
    
    public partial record GiftCardModel : BaseNopModel
    {
        public bool IsGiftCard { get; set; }

        [NopResourceDisplayName("Products.GiftCard.RecipientName")]
        public string RecipientName { get; set; }

        [NopResourceDisplayName("Products.GiftCard.RecipientEmail")]
        [DataType(DataType.EmailAddress)]
        public string RecipientEmail { get; set; }

        [NopResourceDisplayName("Products.GiftCard.SenderName")]
        public string SenderName { get; set; }

        [NopResourceDisplayName("Products.GiftCard.SenderEmail")]
        [DataType(DataType.EmailAddress)]
        public string SenderEmail { get; set; }

        [NopResourceDisplayName("Products.GiftCard.Message")]
        public string Message { get; set; }

        public GiftCardType GiftCardType { get; set; }
    }

    public partial record TierPriceModel : BaseNopModel
    {
        public string Price { get; set; }
        public decimal PriceValue { get; set; }

        public int Quantity { get; set; }
    }

    public partial record ProductAttributeModel : BaseNopEntityModel
    {
        public ProductAttributeModel()
        {
            AllowedFileExtensions = new List<string>();
            Values = new List<ProductAttributeValueModel>();
        }

        public int ProductId { get; set; }

        public int ProductAttributeId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string TextPrompt { get; set; }

        public bool IsRequired { get; set; }

        /// <summary>
        /// Default value for textboxes
        /// </summary>
        public string DefaultValue { get; set; }
        /// <summary>
        /// Selected day value for datepicker
        /// </summary>
        public int? SelectedDay { get; set; }
        /// <summary>
        /// Selected month value for datepicker
        /// </summary>
        public int? SelectedMonth { get; set; }
        /// <summary>
        /// Selected year value for datepicker
        /// </summary>
        public int? SelectedYear { get; set; }

        /// <summary>
        /// A value indicating whether this attribute depends on some other attribute
        /// </summary>
        public bool HasCondition { get; set; }

        /// <summary>
        /// Allowed file extensions for customer uploaded files
        /// </summary>
        public IList<string> AllowedFileExtensions { get; set; }

        public AttributeControlType AttributeControlType { get; set; }

        public IList<ProductAttributeValueModel> Values { get; set; }
    }

    public partial record ProductAttributeValueModel : BaseNopEntityModel
    {
        public ProductAttributeValueModel()
        {
            ImageSquaresPictureModel = new PictureModel();
        }

        public string Name { get; set; }

        public string ColorSquaresRgb { get; set; }

        //picture model is used with "image square" attribute type
        public PictureModel ImageSquaresPictureModel { get; set; }

        public string PriceAdjustment { get; set; }

        public bool PriceAdjustmentUsePercentage { get; set; }

        public decimal PriceAdjustmentValue { get; set; }

        public bool IsPreSelected { get; set; }

        //product picture ID (associated to this value)
        public int PictureId { get; set; }

        public bool CustomerEntersQty { get; set; }

        public int Quantity { get; set; }
    }

    public partial record ProductEstimateShippingModel : EstimateShippingModel
    {
        public int ProductId { get; set; }
    }

    /// <summary>
    /// The customer-facing condition report of one item: the overall grade, the summary, the real photos of this
    /// exact piece, one recorded video and the condition notes. Prepared by
    /// ProductModelFactory.PrepareItemStatusModelAsync. [Mahjar item-status]
    /// </summary>
    public partial record ItemStatusModel : BaseNopModel
    {
        public ItemStatusModel()
        {
            Photos = new List<ItemStatusPhotoModel>();
            Notes = new List<ItemStatusNoteModel>();
        }

        /// <summary>
        /// Whether there is anything at all to render. The single flag the product page guards on: false when the
        /// feature is off, when the product has no report, when the partner hid it, or when the report is empty.
        /// </summary>
        public bool HasAny { get; set; }

        /// <summary>The localized overall grade (empty when the grade was left unspecified).</summary>
        public string GradeText { get; set; }

        /// <summary>The raw grade discriminator (ItemConditionGrade), kept for styling/markup hooks.</summary>
        public int GradeId { get; set; }

        /// <summary>The CSS modifier class that colours the grade pill.</summary>
        public string GradeCssClass { get; set; }

        /// <summary>The free-text condition summary (empty when none).</summary>
        public string Summary { get; set; }

        /// <summary>The inspection date, already converted to the customer time zone and culture-formatted.</summary>
        public string InspectedOn { get; set; }

        /// <summary>The video URL: our own uploaded file when <see cref="VideoIsFile"/>, otherwise an external embed.</summary>
        public string VideoUrl { get; set; }

        /// <summary>True when <see cref="VideoUrl"/> is a file we host (an HTML5 player), false for an external embed.</summary>
        public bool VideoIsFile { get; set; }

        /// <summary>The condition photos, in display order.</summary>
        public IList<ItemStatusPhotoModel> Photos { get; set; }

        /// <summary>The condition notes, in display order.</summary>
        public IList<ItemStatusNoteModel> Notes { get; set; }
    }

    /// <summary>
    /// One condition photo of the item. [Mahjar item-status]
    /// </summary>
    public partial record ItemStatusPhotoModel : BaseNopModel
    {
        /// <summary>The original picture, used as the lightbox target.</summary>
        public string FullSizeImageUrl { get; set; }

        /// <summary>The product-details sized picture (the 2x source of the strip tile).</summary>
        public string ImageUrl { get; set; }

        /// <summary>The thumbnail shown in the photo strip.</summary>
        public string ThumbImageUrl { get; set; }

        /// <summary>The partner caption, shown under the tile and as the lightbox title (may be empty).</summary>
        public string Caption { get; set; }

        /// <summary>The "alt" attribute.</summary>
        public string AlternateText { get; set; }
    }

    /// <summary>
    /// One condition note (damage, wear, previous repair, missing part, accessory or a general remark).
    /// [Mahjar item-status]
    /// </summary>
    public partial record ItemStatusNoteModel : BaseNopModel
    {
        /// <summary>The localized note type.</summary>
        public string TypeText { get; set; }

        /// <summary>The localized severity.</summary>
        public string SeverityText { get; set; }

        /// <summary>The raw severity discriminator (ItemStatusNoteSeverity).</summary>
        public int SeverityId { get; set; }

        /// <summary>The CSS modifier class that colours the severity dot and chip.</summary>
        public string SeverityCssClass { get; set; }

        /// <summary>The note text.</summary>
        public string Text { get; set; }
    }

    #endregion
}