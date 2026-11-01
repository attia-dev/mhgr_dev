using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MHGR.Api.Contracts;

/// <summary>Generic paged list returned by list/search endpoints.</summary>
public class PagedResultDto<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public List<T> Items { get; set; } = new();
}

/// <summary>Simple wrapper for non-paged list endpoints (featured, categories, brands).</summary>
public class ListResultDto<T>
{
    public List<T> Items { get; set; } = new();
}

/// <summary>A category node (home category chips / browse).</summary>
public class CategoryDto
{
    public int Id { get; set; }
    public int ParentId { get; set; }
    public string Name { get; set; }
    public string SeName { get; set; }

    /// <summary>ABSOLUTE, square, 600 px: the category's own picture, else the watch its home tile shows; null when there is none. [Mahjar app-home]</summary>
    public string ImageUrl { get; set; }

    /// <summary>Published products a customer can open in it (the website's count rule, subcategories included when the store says so). [Mahjar app-home]</summary>
    public int ProductCount { get; set; }
}

/// <summary>A manufacturer / brand.</summary>
public class ManufacturerDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string SeName { get; set; }
}

/// <summary>A fully-booked rental date range (for the availability calendar).</summary>
public class DateRangeDto
{
    public string From { get; set; }
    public string To { get; set; }
}

public class CustomerSummaryDto
{
    public int Id { get; set; }
    public string Email { get; set; }
}

/// <summary>Returned by login / verify-email / reset-password / refresh (and register when the account is active).</summary>
public class AuthResponseDto
{
    public string Token { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresInDays { get; set; }
    public CustomerSummaryDto Customer { get; set; }
}

/// <summary>Result of registration. Status: "active" (Auth populated, signed in) | "pendingActivation" | "pendingApproval".</summary>
public class RegisterResultDto
{
    public string Status { get; set; }
    public AuthResponseDto Auth { get; set; }
    public string Message { get; set; }
}

/// <summary>Generic success+message response (forgot-password, logout, etc.).</summary>
public class MessageResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; }
}

/// <summary>Product card (list) + detail. Detail-only fields are null on list responses.</summary>
public class ProductDto : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Sku { get; set; }
    public string SeName { get; set; }
    public string ImageUrl { get; set; }
    public string Currency { get; set; }

    // buy / rent model
    public bool AvailableForPurchase { get; set; }
    public decimal Price { get; set; }
    public bool IsRental { get; set; }
    public decimal? RentalPrice { get; set; }
    public string RentalPricePeriod { get; set; }
    public int? RentalPriceLength { get; set; }
    public decimal? RentalSecurityDeposit { get; set; }

    // rating
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }

    // vendor (multi-vendor)
    public int? VendorId { get; set; }
    public string VendorName { get; set; }

    // detail-only
    public string ShortDescription { get; set; }
    public string FullDescription { get; set; }
    public int? StockQuantity { get; set; }
    public bool? InStock { get; set; }
    public List<string> Images { get; set; }
    public List<DateRangeDto> RentalUnavailableRanges { get; set; }
    // [Mahjar item-status] condition report; detail-only and null when the product has none — the app hides the section.
    public ItemStatusDto ItemStatus { get; set; }
    // [Mahjar rental-occupancy] detail-only: "yyyy-MM-dd" — the item is rented out or booked through this day, so it
    // cannot be bought before then (it can still be rented on free dates); null when it can be bought.
    public string PurchaseBlockedUntil { get; set; }

    // [Mahjar] favourite/wishlist state — set client-side from the wishlist; toggling it live-swaps the card heart.
    private bool _isFavourite;
    public bool IsFavourite
    {
        get => _isFavourite;
        set
        {
            if (_isFavourite == value)
                return;
            _isFavourite = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFavourite)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FavouriteIcon)));
        }
    }

    /// <summary>Heart icon for the product card: filled ruby when favourited, outline otherwise. [Mahjar]</summary>
    public string FavouriteIcon => _isFavourite ? "ic_heart.png" : "ic_heart_outline.png";

    public event PropertyChangedEventHandler PropertyChanged;
}

public class CartItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; }
    public string ImageUrl { get; set; }
    public int Quantity { get; set; }
    public bool IsRental { get; set; }
    public string RentalStartDate { get; set; }
    public string RentalEndDate { get; set; }
    public decimal LineSubTotal { get; set; }
    public string LineSubTotalFormatted { get; set; }
    public string Attributes { get; set; }   // formatted selected attributes (e.g. "اللون: ذهبي، المقاس: 42")
    // [Mahjar rental-occupancy] why this cart line cannot be checked out as it is (localized, e.g. the item was rented
    // out after it was added); empty when the line is fine
    public List<string> Warnings { get; set; } = new();
}

public class CartDto
{
    public List<CartItemDto> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public decimal SubTotal { get; set; }
    public string SubTotalFormatted { get; set; }
    public string Currency { get; set; }
}

// ---------- Wishlist ----------

public class WishlistDto
{
    public List<CartItemDto> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public string Currency { get; set; }
}

// ---------- Orders ----------

public class OrderSummaryDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; }
    public string CreatedOn { get; set; }
    public string OrderStatus { get; set; }
    public string PaymentStatus { get; set; }
    public string ShippingStatus { get; set; }
    public decimal OrderTotal { get; set; }
    public string Currency { get; set; }
}

public class OrderItemDto
{
    public int OrderItemId { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; }
    public string ImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string Attributes { get; set; }   // formatted selected attributes
    public bool IsRental { get; set; }
    public string RentalStartDate { get; set; }
    public string RentalEndDate { get; set; }
    public decimal? RentalSecurityDeposit { get; set; }
    public string RentalReturnStatus { get; set; }
    public string RentalReturnRequestedOn { get; set; }
    public int? VendorId { get; set; }
    public string VendorName { get; set; }
    // [Mahjar handover] the item was handed over to the customer (at the handover desk or from the order screens)
    public bool HandedOver { get; set; }
    // [Mahjar handover] a handed-over rental whose return the customer can still announce
    public bool CanRequestReturn { get; set; }
}

public class OrderDetailDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; }
    public string CreatedOn { get; set; }
    public string OrderStatus { get; set; }
    public string PaymentStatus { get; set; }
    public string ShippingStatus { get; set; }
    public bool CanCancel { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public decimal SubTotal { get; set; }
    public decimal Shipping { get; set; }
    public decimal Tax { get; set; }
    public decimal Discount { get; set; }
    public decimal OrderTotal { get; set; }
    public decimal RefundedAmount { get; set; }
    public string Currency { get; set; }
    // [Mahjar handover] the contract the order was placed against (0 = none) and whether it holds condition records
    public int ContractRequestId { get; set; }
    public bool HasConditionRecords { get; set; }
    // [Mahjar handover] customer-visible order notes, newest first (e.g. an item handed over, a deposit refund)
    public List<OrderNoteDto> Notes { get; set; } = new();
}

/// <summary>A customer-visible order note. [Mahjar handover]</summary>
public class OrderNoteDto
{
    public string CreatedOn { get; set; }   // "yyyy-MM-dd HH:mm", local time
    public string Text { get; set; }
}

public class OrderStatusResultDto
{
    public bool Success { get; set; }
    public string OrderStatus { get; set; }
}

public class ReturnResultDto
{
    public bool Success { get; set; }
    public string ReturnStatus { get; set; }
    public int ItemsMarked { get; set; }
    public string Message { get; set; }
}

// ---------- Account ----------

public class CustomerProfileDto
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Phone { get; set; }

    /// <summary>ABSOLUTE url of the customer's profile photo (a 320 px square JPEG); null when there is none or the store
    /// has profile photos switched off. A new photo always gets a new url. [Mahjar app-batch3]</summary>
    public string AvatarUrl { get; set; }

    /// <summary>The store lets customers set a profile photo (CustomerSettings.AllowCustomersToUploadAvatars); the app
    /// offers "Change photo" only then. [Mahjar app-batch3]</summary>
    public bool CanChangeAvatar { get; set; }
}

public class AddressDto
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Company { get; set; }
    public int? CountryId { get; set; }
    public int? StateProvinceId { get; set; }
    public string City { get; set; }
    public string Address1 { get; set; }
    public string Address2 { get; set; }
    public string ZipPostalCode { get; set; }
}

// ---------- Checkout ----------

public class PlacedOrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; }
    public string OrderGuid { get; set; }
    public decimal OrderTotal { get; set; }
    public string Currency { get; set; }
    public string PaymentMethod { get; set; }
    public string OrderStatus { get; set; }
    public string PaymentStatus { get; set; }
}

public class CheckoutTotalsDto
{
    public decimal SubTotal { get; set; }
    public decimal? Total { get; set; }   // null when the total can't yet be computed (needs shipping/payment)
    public decimal RefundableDeposit { get; set; }
    public string Currency { get; set; }
}

public class ShippingOptionDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Rate { get; set; }
    public string Currency { get; set; }
}

public class ShippingOptionsResultDto
{
    public List<ShippingOptionDto> Items { get; set; } = new();
    public List<string> Errors { get; set; }
}

public class PaymentMethodDto
{
    public string SystemName { get; set; }
    public string Name { get; set; }
}

// ---------- Vendors ----------

public class VendorDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string SeName { get; set; }
    public string ImageUrl { get; set; }
    public string Description { get; set; }
    public int? ProductCount { get; set; }
}

/// <summary>"Become a partner" application submitted from the app (mirrors the web ApplyVendor form). [Mahjar]</summary>
public class ApplyVendorApiRequest
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Description { get; set; }

    /// <summary>1 = individual, 2 = corporation (Nop.Core.Domain.Vendors.VendorType). Required. [Mahjar vendor-application]</summary>
    public int VendorTypeId { get; set; }

    /// <summary>National ID / Iqama number — required when <see cref="VendorTypeId"/> is 1 (individual).</summary>
    public string NationalId { get; set; }

    /// <summary>Commercial registration number — required when <see cref="VendorTypeId"/> is 2 (corporation).</summary>
    public string CommercialRegistrationNumber { get; set; }

    /// <summary>VAT registration number — optional for both types.</summary>
    public string VatNumber { get; set; }

    /// <summary>Name of the person authorised to sign for the company — corporations only. [Mahjar partner-agreement]</summary>
    public string AuthorizedSignatoryName { get; set; }

    /// <summary>The capacity that person signs in (owner, manager, power of attorney) — corporations only. [Mahjar partner-agreement]</summary>
    public string AuthorizedSignatoryCapacity { get; set; }

    /// <summary>Activate renting out — Annex B of the partner agreement then binds the partner. [Mahjar partner-agreement]</summary>
    public bool ActivateRental { get; set; }

    /// <summary>Activate selling — Annex C of the partner agreement then binds the partner. [Mahjar partner-agreement]</summary>
    public bool ActivateSale { get; set; }

    /// <summary>Must be true when the store requires terms acceptance (see ApplyVendorStatusDto.TermsOfServiceEnabled).</summary>
    public bool AcceptTermsOfService { get; set; }
}

/// <summary>
/// The partner agreement rendered for a half-filled application, so the app can show the applicant the document
/// with their own details in it rather than a column of dashes. [Mahjar partner-agreement]
/// </summary>
public class AgreementPreviewDto
{
    /// <summary>The agreement as HTML, in the caller's language, with the submitted details already printed.</summary>
    public string Html { get; set; }
}

/// <summary>One selectable partner type, already localized for the caller's language. [Mahjar vendor-application]</summary>
public class VendorTypeOptionDto
{
    public int Id { get; set; }        // 1 = individual, 2 = corporation
    public string Name { get; set; }   // localized label
}

/// <summary>Tells the app whether the current customer may apply to become a partner, plus an email to prefill. [Mahjar]</summary>
public class ApplyVendorStatusDto
{
    public bool Enabled { get; set; }          // store allows customers to apply for a vendor account
    public bool AlreadyApplied { get; set; }   // this customer already has a (pending/active) vendor
    public string Email { get; set; }          // current customer email, to prefill the form

    /// <summary>Whether the applicant must accept the terms before submitting. [Mahjar vendor-application]</summary>
    public bool TermsOfServiceEnabled { get; set; }

    /// <summary>The terms body as HTML, in the caller's language — edited in Admin -> Settings -> Vendor settings.</summary>
    public string TermsOfServiceHtml { get; set; }

    /// <summary>The partner types to choose from (individual / corporation), localized.</summary>
    public List<VendorTypeOptionDto> VendorTypes { get; set; } = new List<VendorTypeOptionDto>();

    /// <summary>
    /// Whether the full master partner agreements replace the short terms body. When true the app shows
    /// <see cref="IndividualAgreementHtml"/> or <see cref="CorporateAgreementHtml"/> according to the type the
    /// applicant picked, and must collect the activated services. [Mahjar partner-agreement]
    /// </summary>
    public bool AgreementEnabled { get; set; }

    /// <summary>The version of the agreement in force, e.g. "1.0". [Mahjar partner-agreement]</summary>
    public string AgreementVersion { get; set; }

    /// <summary>The individual partner agreement as HTML, in the caller's language. [Mahjar partner-agreement]</summary>
    public string IndividualAgreementHtml { get; set; }

    /// <summary>The corporate partner agreement as HTML, in the caller's language. [Mahjar partner-agreement]</summary>
    public string CorporateAgreementHtml { get; set; }
}

// ---------- Reference ----------

public class CountryDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string TwoLetterIsoCode { get; set; }
    public string NumericIsoCode { get; set; }
}

public class StateProvinceDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class CurrencyDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
}

public class LanguageDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public bool Rtl { get; set; }
}

public class TopicDto
{
    public string SystemName { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
}

public class StoreInfoDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public string Currency { get; set; }
}

// ---------- Product attributes (variants) ----------

public class ProductAttributeValueDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal PriceAdjustment { get; set; }
    public bool PriceAdjustmentUsePercentage { get; set; }
    public bool IsPreSelected { get; set; }
    public string ColorSquaresRgb { get; set; }
}

public class ProductAttributeDto
{
    public int Id { get; set; }            // ProductAttributeMapping id — send this back as attributeMappingId
    public string Name { get; set; }
    public string TextPrompt { get; set; }
    public bool IsRequired { get; set; }
    public int ControlType { get; set; }   // 1 Dropdown, 2 Radio, 3 Checkboxes, 4 TextBox, 10 Multiline, 20 Datepicker, 40 ColorSquares, 45 ImageSquares, 50 ReadonlyCheckboxes
    public List<ProductAttributeValueDto> Values { get; set; } = new();
}

// ---------- Reviews ----------

public class ReviewDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string ReviewText { get; set; }
    public int Rating { get; set; }
    public string CustomerName { get; set; }
    public string CreatedOn { get; set; }
    public int HelpfulYes { get; set; }
    public int HelpfulNo { get; set; }
    public int? ProductId { get; set; }      // populated for vendor reviews (which product was reviewed)
    public string ProductName { get; set; }  // populated for vendor reviews
}

public class ReviewsResultDto
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public List<ReviewDto> Items { get; set; } = new();
}

public class StatusResultDto
{
    public bool Success { get; set; }
    public string Status { get; set; }
}

// ---------- Reward points (loyalty) ----------

public class RewardPointDto
{
    public int Points { get; set; }
    public int? PointsBalance { get; set; }
    public string Message { get; set; }
    public string CreatedOn { get; set; }
}

public class RewardPointsResultDto
{
    public int Balance { get; set; }
    public string Currency { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public List<RewardPointDto> Items { get; set; } = new();
}

// ---------- Gift cards ----------

public class GiftCardBalanceDto
{
    public bool Valid { get; set; }
    public decimal RemainingAmount { get; set; }
    public string RemainingAmountFormatted { get; set; }
    public string Currency { get; set; }
}

// ---------- Returns (RMA) ----------

public class ReturnRequestSummaryDto
{
    public int Id { get; set; }
    public string CustomNumber { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public string Status { get; set; }
    public string ReasonForReturn { get; set; }
    public string RequestedAction { get; set; }
    public string CreatedOn { get; set; }
}

public class ReturnableItemDto
{
    public int OrderItemId { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; }
    public string ImageUrl { get; set; }
    public int Quantity { get; set; }
}

public class ReturnRequestReasonDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class ReturnRequestActionDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class ReturnableOrderDto
{
    public bool ReturnAllowed { get; set; }
    public List<ReturnableItemDto> Items { get; set; } = new();
    public List<ReturnRequestReasonDto> Reasons { get; set; } = new();
    public List<ReturnRequestActionDto> Actions { get; set; } = new();
}

public class ReturnRequestResultDto
{
    public bool Success { get; set; }
    public int Created { get; set; }
    public string Message { get; set; }
}

// ---------- Back-in-stock notifications ----------

public class BackInStockSubscriptionDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public string ImageUrl { get; set; }
}

// ---------- Blog ----------

public class BlogPostSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string BodyOverview { get; set; }
    public string CreatedOn { get; set; }
}

public class BlogCommentDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; }
    public string CommentText { get; set; }
    public string CreatedOn { get; set; }
}

public class BlogPostDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public string CreatedOn { get; set; }
    public bool AllowComments { get; set; }
    public List<BlogCommentDto> Comments { get; set; } = new();
}

// ---------- News ----------

public class NewsItemSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Short { get; set; }
    public string CreatedOn { get; set; }
}

public class NewsCommentDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; }
    public string CommentTitle { get; set; }
    public string CommentText { get; set; }
    public string CreatedOn { get; set; }
}

public class NewsItemDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Full { get; set; }
    public string CreatedOn { get; set; }
    public bool AllowComments { get; set; }
    public List<NewsCommentDto> Comments { get; set; } = new();
}

// ============================ Request bodies (shared with the app) ============================

public class LoginApiRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class RegisterApiRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class VerifyEmailApiRequest
{
    public string Email { get; set; }
    public string Token { get; set; }
}

public class ForgotPasswordApiRequest
{
    public string Email { get; set; }
}

public class ResetPasswordApiRequest
{
    public string Email { get; set; }
    public string Token { get; set; }
    public string NewPassword { get; set; }
}

/// <summary>A user's selection for one product attribute. ValueIds for dropdown/radio/checkbox/color/image; Text for textbox/datepicker.</summary>
public class SelectedAttributeApi
{
    public int AttributeMappingId { get; set; }
    public List<int> ValueIds { get; set; } = new();
    public string Text { get; set; }
}

public class AddToCartApiRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTime? RentalStartDate { get; set; }
    public DateTime? RentalEndDate { get; set; }
    public List<SelectedAttributeApi> Attributes { get; set; }
}

public class UpdateCartApiRequest
{
    public int Quantity { get; set; } = 1;
    public DateTime? RentalStartDate { get; set; }
    public DateTime? RentalEndDate { get; set; }
    public List<SelectedAttributeApi> Attributes { get; set; }
}

public class CouponApiRequest
{
    public string Code { get; set; }
}

public class MoveToCartApiRequest
{
    public int ItemId { get; set; }
}

public class ProfileApiRequest
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Phone { get; set; }
}

public class ChangePasswordApiRequest
{
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
}

public class AddressApiRequest
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Company { get; set; }
    public int CountryId { get; set; }
    public int StateProvinceId { get; set; }
    public string City { get; set; }
    public string Address1 { get; set; }
    public string Address2 { get; set; }
    public string ZipPostalCode { get; set; }
}

public class ReturnApiRequest
{
    public int OrderItemId { get; set; }
    public string Note { get; set; }
}

public class CheckoutApiRequest
{
    public ApiAddress Address { get; set; }
}

public class ShippingOptionsApiRequest
{
    public int AddressId { get; set; }
}

public class ApiAddress
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public int CountryId { get; set; }
    public string City { get; set; }
    public string Address1 { get; set; }
    public string ZipPostalCode { get; set; }
    public string PhoneNumber { get; set; }
}

public class ProductReviewApiRequest
{
    public string Title { get; set; }
    public string ReviewText { get; set; }
    public int Rating { get; set; }
}

public class ReturnItemLine
{
    public int OrderItemId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class SubmitReturnApiRequest
{
    public List<ReturnItemLine> Items { get; set; } = new();
    public int ReasonId { get; set; }
    public int ActionId { get; set; }
    public string Comments { get; set; }
}

public class EstimateShippingApiRequest
{
    public int CountryId { get; set; }
    public int? StateProvinceId { get; set; }
    public string City { get; set; }
    public string ZipPostalCode { get; set; }
}

public class ContactApiRequest
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
}

public class CommentApiRequest
{
    public string Title { get; set; }       // used by news comments only
    public string CommentText { get; set; }
}

public class NewsletterApiRequest
{
    public string Email { get; set; }
}

// ----- In-app notifications [Mahjar notifications] -----

/// <summary>A single in-app notification (feed item + the real-time SignalR "ReceiveNotification" payload).</summary>
public class NotificationDto
{
    public int Id { get; set; }
    public int Type { get; set; }                 // NotificationType backing value
    public string Title { get; set; }
    public string Body { get; set; }
    public bool IsRead { get; set; }
    public string CreatedOn { get; set; }         // ISO-8601 ("o")
    public string TargetEntity { get; set; }      // "order" | "product" | null
    public int? TargetId { get; set; }
}

/// <summary>A per-type notification preference row (settings screen).</summary>
public class NotificationPreferenceDto
{
    public int Type { get; set; }                 // NotificationType backing value
    public string Title { get; set; }             // localized type label
    public bool InApp { get; set; }
    public bool Email { get; set; }
}

/// <summary>Body for PUT api/notifications/preferences.</summary>
public class UpdatePreferencesApiRequest
{
    public List<NotificationPreferenceDto> Preferences { get; set; } = new();
}

/// <summary>Response of GET api/notifications/unread-count.</summary>
public class UnreadCountDto
{
    public int Count { get; set; }
}

// ----- Contract approval [Mahjar contracts] -----

/// <summary>A contract request (the pre-payment contract-approval stage).</summary>
public class ContractRequestDto
{
    public int Id { get; set; }
    public string Number { get; set; }            // "MHGR-C-00001"
    public string Status { get; set; }            // "pending" | "contractSent" | "signed" | "approved" | "rejected" | "cancelled" | "completed"
    public string StatusLabel { get; set; }       // localized status text
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? SentOnUtc { get; set; }
    public DateTime? SignedOnUtc { get; set; }
    public string SignedName { get; set; }        // [Mahjar handover] the name the customer signed with
    public string ContractBodyHtml { get; set; }  // present once the contract has been sent
    public string RejectionReason { get; set; }
    public bool CanSign { get; set; }
    public bool CanCancel { get; set; }
    public bool CanCheckout { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DepositTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; }
    public List<ContractLineDto> Lines { get; set; } = new();

    /// <summary>[Mahjar handover] the order placed against the approved contract (0 = none yet).</summary>
    public int OrderId { get; set; }

    /// <summary>[Mahjar handover] that order's number (null = none yet).</summary>
    public string OrderNumber { get; set; }

    /// <summary>
    /// [Mahjar handover] the documented condition of each item — before signing (once the contract is sent), at
    /// delivery and at a rental return — confirmed checks only, grouped by checkpoint; never null, may be empty.
    /// </summary>
    public List<ConditionCheckDto> ConditionChecks { get; set; } = new();
}

/// <summary>
/// One documented condition check of an item: photos, video, grade, notes and damage recorded by the staff at a
/// checkpoint. Strings are localized server-side; media URLs are ABSOLUTE. [Mahjar handover]
/// </summary>
public class ConditionCheckDto
{
    public int Id { get; set; }
    public string Type { get; set; }              // "preContract" | "delivery" | "return"
    public string TypeLabel { get; set; }         // localized checkpoint ("قبل التوقيع" / "Before signing")
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public string RecordedOn { get; set; }        // "yyyy-MM-dd"
    public string GradeText { get; set; }         // null when not graded
    public string Notes { get; set; }
    public bool HasDamage { get; set; }
    public string DamageSeverityText { get; set; }
    public string DamageDescription { get; set; }
    public decimal? DamageDeduction { get; set; } // return only
    public decimal? DepositRefund { get; set; }   // return only
    public string VideoUrl { get; set; }          // ABSOLUTE; null when there is no video
    public bool VideoIsFile { get; set; }         // true = a file we host, false = an external url (open in a browser)
    public List<ItemStatusPhotoDto> Photos { get; set; } = new();
}

/// <summary>One line of the frozen cart snapshot inside a contract request.</summary>
public class ContractLineDto
{
    public int ProductId { get; set; }
    public string Name { get; set; }
    public string Sku { get; set; }
    public bool IsRental { get; set; }
    public int Quantity { get; set; }
    public DateTime? RentalStart { get; set; }
    public DateTime? RentalEnd { get; set; }
    public int RentalDays { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Deposit { get; set; }
    public decimal LineTotal { get; set; }
}

/// <summary>Body for POST api/contract/sign.</summary>
public class ContractSignRequest
{
    public string SignatureDataUrl { get; set; }  // "data:image/png;base64,..."
    public string SignedName { get; set; }
}

// ---------- Item status (condition report) [Mahjar item-status] ----------

/// <summary>
/// The per-product condition report shown in the app's "حالة القطعة" section: overall grade, summary, the real
/// photos of THAT exact piece, one recorded video and the condition notes. Every string is already localized
/// server-side (the API honours Accept-Language), so the app renders it verbatim. Null on <see cref="ProductDto"/>
/// when the feature is off, the product has no report, the partner hid it, or it is empty — the app hides the
/// whole section on null. [Mahjar item-status]
/// </summary>
public class ItemStatusDto
{
    /// <summary>ItemConditionGrade backing value (0 = not specified).</summary>
    public int GradeId { get; set; }

    /// <summary>Localized grade label ("ممتازة" / "Excellent"); null when the grade is not specified.</summary>
    public string GradeText { get; set; }

    /// <summary>Free-text condition summary; null when the partner wrote none.</summary>
    public string Summary { get; set; }

    /// <summary>Inspection date as "yyyy-MM-dd"; null when not recorded.</summary>
    public string InspectedOn { get; set; }

    /// <summary>ABSOLUTE video url (MAUI cannot resolve a relative one); null when there is no video.</summary>
    public string VideoUrl { get; set; }

    /// <summary>true = a file we host (playable/streamable directly), false = an external url to open in a browser.</summary>
    public bool VideoIsFile { get; set; }

    /// <summary>Real photos of this exact item, in display order; never null, may be empty.</summary>
    public List<ItemStatusPhotoDto> Photos { get; set; }

    /// <summary>Condition notes, in display order; never null, may be empty.</summary>
    public List<ItemStatusNoteDto> Notes { get; set; }
}

/// <summary>One condition photo. Both urls are ABSOLUTE. [Mahjar item-status]</summary>
public class ItemStatusPhotoDto
{
    public string Url { get; set; }
    public string ThumbUrl { get; set; }
    public string Caption { get; set; }
}

/// <summary>One condition note (damage / wear / repair / missing part / accessory / general). [Mahjar item-status]</summary>
public class ItemStatusNoteDto
{
    /// <summary>ItemStatusNoteSeverity backing value — 10 Info, 20 Minor, 30 Moderate, 40 Major (drives the dot colour).</summary>
    public int SeverityId { get; set; }

    /// <summary>Localized severity label ("متوسط" / "Moderate").</summary>
    public string SeverityText { get; set; }

    /// <summary>Localized note-type label ("أثر استعمال" / "Wear").</summary>
    public string TypeText { get; set; }

    /// <summary>The note itself.</summary>
    public string Text { get; set; }
}

// ---------- Home slides [Mahjar home-slides] ----------

/// <summary>One home carousel slide. Texts localized per Accept-Language; urls ABSOLUTE. [Mahjar home-slides]</summary>
public class HomeSlideDto
{
    public int Id { get; set; }

    /// <summary>ABSOLUTE url — the mobile picture if the slide has one, else the main picture (≈1200px).</summary>
    public string ImageUrl { get; set; }

    /// <summary>The small eyebrow line above the title; null/empty = none.</summary>
    public string Kicker { get; set; }

    public string Title { get; set; }

    /// <summary>The line under the title; null/empty = none.</summary>
    public string Subtitle { get; set; }

    /// <summary>The call-to-action text; null/empty = no button (the slide is still tappable when it has a target).</summary>
    public string ButtonText { get; set; }

    /// <summary>"fullbleed" (photo covers the slide, text over a gradient) | "spotlight" (brand backdrop, picture shown as an object).</summary>
    public string Layout { get; set; }

    /// <summary>"start" | "center" | "end" — logical: start = right in RTL.</summary>
    public string ContentPosition { get; set; }

    /// <summary>Legibility gradient darkness, 0..90 (fullbleed).</summary>
    public int OverlayOpacity { get; set; }

    /// <summary>null (no link) | "product" | "category" | "manufacturer" | "vendor" | "search" | "url".</summary>
    public string TargetType { get; set; }

    /// <summary>The entity id for product / category / manufacturer / vendor, else null.</summary>
    public int? TargetId { get; set; }

    /// <summary>The search query (search), an ABSOLUTE http(s) url (url — site-relative links are made absolute off the request host),
    /// "rent" for a product the slide opens in rent mode (product → product?id=…&amp;mode=rent), else null.</summary>
    public string TargetValue { get; set; }
}

// ---------- App home categories [Mahjar app-home] ----------

/// <summary>
/// One home category card: the SAME categories, order and pictures as the website home page (published, shown on
/// the home page, ACL + store mapping; a category without a picture of its own shows its first visible watch).
/// Name localized per Accept-Language; ImageUrl ABSOLUTE (600 px) or null. [Mahjar app-home]
/// </summary>
public class HomeCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string SeName { get; set; }
    public string ImageUrl { get; set; }

    /// <summary>Published products a customer can open in it (the website's count rule). The app hides a 0.</summary>
    public int ProductCount { get; set; }
}
