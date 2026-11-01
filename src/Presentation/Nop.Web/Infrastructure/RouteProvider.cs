using Nop.Services.Installation;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Web.Infrastructure;

/// <summary>
/// Represents provider that provided basic routes
/// </summary>
public partial class RouteProvider : BaseRouteProvider, IRouteProvider
{
    #region Methods

    /// <summary>
    /// Register routes
    /// </summary>
    /// <param name="endpointRouteBuilder">Route builder</param>
    public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
    {
        //get language pattern
        //it's not needed to use language pattern in AJAX requests and for actions returning the result directly (e.g. file to download),
        //use it only for URLs of pages that the user can go to
        var lang = GetLanguageRoutePattern();

        //areas
        endpointRouteBuilder.MapControllerRoute(name: "areaRoute",
            pattern: $"{{area:exists}}/{{controller=Home}}/{{action=Index}}/{{id?}}");

        //home page
        endpointRouteBuilder.MapControllerRoute(name: "Homepage",
            pattern: $"{lang}",
            defaults: new { controller = "Home", action = "Index" });

        //login
        endpointRouteBuilder.MapControllerRoute(name: "Login",
            pattern: $"{lang}/login/",
            defaults: new { controller = "Customer", action = "Login" });

        // multi-factor verification digit code page
        endpointRouteBuilder.MapControllerRoute(name: "MultiFactorVerification",
            pattern: $"{lang}/multi-factor-verification/",
            defaults: new { controller = "Customer", action = "MultiFactorVerification" });

        //register
        endpointRouteBuilder.MapControllerRoute(name: "Register",
            pattern: $"{lang}/register/",
            defaults: new { controller = "Customer", action = "Register" });

        //logout
        endpointRouteBuilder.MapControllerRoute(name: "Logout",
            pattern: $"{lang}/logout/",
            defaults: new { controller = "Customer", action = "Logout" });

        //shopping cart
        endpointRouteBuilder.MapControllerRoute(name: "ShoppingCart",
            pattern: $"{lang}/cart/",
            defaults: new { controller = "ShoppingCart", action = "Cart" });

        //estimate shipping (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "EstimateShipping",
            pattern: $"cart/estimateshipping",
            defaults: new { controller = "ShoppingCart", action = "GetEstimateShipping" });

        //select shipping option (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "SelectShippingOption",
            pattern: $"cart/selectshippingoption",
            defaults: new { controller = "ShoppingCart", action = "SelectShippingOption" });

        //wishlist
        endpointRouteBuilder.MapControllerRoute(name: "Wishlist",
            pattern: $"{lang}/wishlist/{{customerGuid?}}",
            defaults: new { controller = "ShoppingCart", action = "Wishlist" });

        //checkout attribute change (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "CheckoutAttributeChange",
            pattern: "shoppingcart/checkoutattributechange/{{isEditable}}",
            defaults: new { controller = "ShoppingCart", action = "CheckoutAttributeChange" });

        //customer account links
        endpointRouteBuilder.MapControllerRoute(name: "CustomerInfo",
            pattern: $"{lang}/customer/info",
            defaults: new { controller = "Customer", action = "Info" });

        endpointRouteBuilder.MapControllerRoute(name: "CustomerAddresses",
            pattern: $"{lang}/customer/addresses",
            defaults: new { controller = "Customer", action = "Addresses" });

        //customer address delete (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "CustomerAddressDelete",
            pattern: $"customer/addressdelete",
            defaults: new { controller = "Customer", action = "AddressDelete" });

        //remove external association (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "CustomerRemoveExternalAssociation",
            pattern: $"customer/removeexternalassociation",
            defaults: new { controller = "Customer", action = "RemoveExternalAssociation" });

        endpointRouteBuilder.MapControllerRoute(name: "CustomerOrders",
            pattern: $"{lang}/order/history",
            defaults: new { controller = "Order", action = "CustomerOrders" });

        //contact us
        endpointRouteBuilder.MapControllerRoute(name: "ContactUs",
            pattern: $"{lang}/contactus",
            defaults: new { controller = "Common", action = "ContactUs" });

        //product search
        endpointRouteBuilder.MapControllerRoute(name: "ProductSearch",
            pattern: $"{lang}/search/",
            defaults: new { controller = "Catalog", action = "Search" });

        //autocomplete search term (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "ProductSearchAutoComplete",
            pattern: $"catalog/searchtermautocomplete",
            defaults: new { controller = "Catalog", action = "SearchTermAutoComplete" });

        //change currency
        endpointRouteBuilder.MapControllerRoute(name: "ChangeCurrency",
            pattern: $"{lang}/changecurrency/{{customercurrency:min(0)}}",
            defaults: new { controller = "Common", action = "SetCurrency" });

        //change language
        endpointRouteBuilder.MapControllerRoute(name: "ChangeLanguage",
            pattern: $"{lang}/changelanguage/{{langid:min(0)}}",
            defaults: new { controller = "Common", action = "SetLanguage" });

        //change tax
        endpointRouteBuilder.MapControllerRoute(name: "ChangeTaxType",
            pattern: $"{lang}/changetaxtype/{{customertaxtype:min(0)}}",
            defaults: new { controller = "Common", action = "SetTaxType" });

        //set store theme
        endpointRouteBuilder.MapControllerRoute(name: "SetStoreTheme",
            pattern: $"{lang}/setstoretheme/{{themeName}}/{{returnUrl}}",
            defaults: new { controller = "Common", action = "SetStoreTheme" });

        //recently viewed products
        endpointRouteBuilder.MapControllerRoute(name: "RecentlyViewedProducts",
            pattern: $"{lang}/recentlyviewedproducts/",
            defaults: new { controller = "Product", action = "RecentlyViewedProducts" });

        //new products
        endpointRouteBuilder.MapControllerRoute(name: "NewProducts",
            pattern: $"{lang}/newproducts/",
            defaults: new { controller = "Catalog", action = "NewProducts" });

        //blog
        endpointRouteBuilder.MapControllerRoute(name: "Blog",
            pattern: $"{lang}/blog",
            defaults: new { controller = "Blog", action = "List" });

        //news
        endpointRouteBuilder.MapControllerRoute(name: "NewsArchive",
            pattern: $"{lang}/news",
            defaults: new { controller = "News", action = "List" });

        //forum
        endpointRouteBuilder.MapControllerRoute(name: "Boards",
            pattern: $"{lang}/boards",
            defaults: new { controller = "Boards", action = "Index" });

        //compare products
        endpointRouteBuilder.MapControllerRoute(name: "CompareProducts",
            pattern: $"{lang}/compareproducts/",
            defaults: new { controller = "Product", action = "CompareProducts" });

        //product tags
        endpointRouteBuilder.MapControllerRoute(name: "ProductTagsAll",
            pattern: $"{lang}/producttag/all/",
            defaults: new { controller = "Catalog", action = "ProductTagsAll" });

        //manufacturers
        endpointRouteBuilder.MapControllerRoute(name: "ManufacturerList",
            pattern: $"{lang}/manufacturer/all/",
            defaults: new { controller = "Catalog", action = "ManufacturerAll" });

        //vendors
        endpointRouteBuilder.MapControllerRoute(name: "VendorList",
            pattern: $"{lang}/vendor/all/",
            defaults: new { controller = "Catalog", action = "VendorAll" });

        endpointRouteBuilder.MapControllerRoute(name: "VendorReviews",
            pattern: $"{lang}/vendor/{{vendorId:min(0)}}/reviews",
            defaults: new { controller = "Catalog", action = "VendorReviews" });

        //add product to cart (without any attributes and options). used on catalog pages. (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "AddProductToCart-Catalog",
            pattern: $"addproducttocart/catalog/{{productId:min(0)}}/{{shoppingCartTypeId:min(0)}}/{{quantity:min(0)}}",
            defaults: new { controller = "ShoppingCart", action = "AddProductToCart_Catalog" });

        //add product to cart (with attributes and options). used on the product details pages. (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "AddProductToCart-Details",
            pattern: $"addproducttocart/details/{{productId:min(0)}}/{{shoppingCartTypeId:min(0)}}",
            defaults: new { controller = "ShoppingCart", action = "AddProductToCart_Details" });

        //comparing products (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "AddProductToCompare",
            pattern: $"compareproducts/add/{{productId:min(0)}}",
            defaults: new { controller = "Product", action = "AddProductToCompareList" });

        //product email a friend
        endpointRouteBuilder.MapControllerRoute(name: "ProductEmailAFriend",
            pattern: $"{lang}/productemailafriend/{{productId:min(0)}}",
            defaults: new { controller = "Product", action = "ProductEmailAFriend" });

        //product estimate shipping (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "ProductEstimateShipping",
            pattern: "product/estimateshipping/{{ProductId:min(0)}}",
            defaults: new { controller = "Product", action = "EstimateShipping" });

        //reviews
        endpointRouteBuilder.MapControllerRoute(name: "CustomerProductReviews",
            pattern: $"{lang}/customer/productreviews",
            defaults: new { controller = "Product", action = "CustomerProductReviews" });

        endpointRouteBuilder.MapControllerRoute(name: "CustomerProductReviewsPaged",
            pattern: $"{lang}/customer/productreviews/page/{{pageNumber:min(0)}}",
            defaults: new { controller = "Product", action = "CustomerProductReviews" });

        //back in stock notifications (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "BackInStockSubscribePopup",
            pattern: $"backinstocksubscribe/{{productId:min(0)}}",
            defaults: new { controller = "BackInStockSubscription", action = "SubscribePopup" });

        endpointRouteBuilder.MapControllerRoute(name: "BackInStockSubscribeSend",
            pattern: $"backinstocksubscribesend/{{productId:min(0)}}",
            defaults: new { controller = "BackInStockSubscription", action = "SubscribePopupPOST" });

        //downloads (file result)
        endpointRouteBuilder.MapControllerRoute(name: "GetSampleDownload",
            pattern: $"download/sample/{{productid:min(0)}}",
            defaults: new { controller = "Download", action = "Sample" });

        //downloads
        endpointRouteBuilder.MapControllerRoute(name: "DownloadGetFileUpload",
            pattern: $"download/getfileupload/{{downloadId}}",
            defaults: new { controller = "Download", action = "GetFileUpload" });

        //checkout pages
        endpointRouteBuilder.MapControllerRoute(name: "Checkout",
            pattern: $"{lang}/checkout/",
            defaults: new { controller = "Checkout", action = "Index" });

        endpointRouteBuilder.MapControllerRoute(name: "CheckoutOnePage",
            pattern: $"{lang}/onepagecheckout/",
            defaults: new { controller = "Checkout", action = "OnePageCheckout" });

        endpointRouteBuilder.MapControllerRoute(name: "CheckoutShippingAddress",
            pattern: $"{lang}/checkout/shippingaddress",
            defaults: new { controller = "Checkout", action = "ShippingAddress" });

        endpointRouteBuilder.MapControllerRoute(name: "CheckoutSelectShippingAddress",
            pattern: $"{lang}/checkout/selectshippingaddress",
            defaults: new { controller = "Checkout", action = "SelectShippingAddress" });

        endpointRouteBuilder.MapControllerRoute(name: "CheckoutBillingAddress",
            pattern: $"{lang}/checkout/billingaddress",
            defaults: new { controller = "Checkout", action = "BillingAddress" });

        endpointRouteBuilder.MapControllerRoute(name: "CheckoutSelectBillingAddress",
            pattern: $"{lang}/checkout/selectbillingaddress",
            defaults: new { controller = "Checkout", action = "SelectBillingAddress" });

        endpointRouteBuilder.MapControllerRoute(name: "CheckoutShippingMethod",
            pattern: $"{lang}/checkout/shippingmethod",
            defaults: new { controller = "Checkout", action = "ShippingMethod" });

        endpointRouteBuilder.MapControllerRoute(name: "CheckoutPaymentMethod",
            pattern: $"{lang}/checkout/paymentmethod",
            defaults: new { controller = "Checkout", action = "PaymentMethod" });

        endpointRouteBuilder.MapControllerRoute(name: "CheckoutPaymentInfo",
            pattern: $"{lang}/checkout/paymentinfo",
            defaults: new { controller = "Checkout", action = "PaymentInfo" });

        endpointRouteBuilder.MapControllerRoute(name: "CheckoutConfirm",
            pattern: $"{lang}/checkout/confirm",
            defaults: new { controller = "Checkout", action = "Confirm" });

        endpointRouteBuilder.MapControllerRoute(name: "CheckoutCompleted",
            pattern: $"{lang}/checkout/completed/{{orderId:int?}}",
            defaults: new { controller = "Checkout", action = "Completed" });

        //subscribe newsletters (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "SubscribeNewsletter",
            pattern: $"subscribenewsletter",
            defaults: new { controller = "Newsletter", action = "SubscribeNewsletter" });

        //email wishlist
        endpointRouteBuilder.MapControllerRoute(name: "EmailWishlist",
            pattern: $"{lang}/emailwishlist",
            defaults: new { controller = "ShoppingCart", action = "EmailWishlist" });

        //login page for checkout as guest
        endpointRouteBuilder.MapControllerRoute(name: "LoginCheckoutAsGuest",
            pattern: $"{lang}/login/checkoutasguest",
            defaults: new { controller = "Customer", action = "Login", checkoutAsGuest = true });

        //register result page
        endpointRouteBuilder.MapControllerRoute(name: "RegisterResult",
            pattern: $"{lang}/registerresult/{{resultId:min(0)}}",
            defaults: new { controller = "Customer", action = "RegisterResult" });

        //check username availability (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "CheckUsernameAvailability",
            pattern: $"customer/checkusernameavailability",
            defaults: new { controller = "Customer", action = "CheckUsernameAvailability" });

        //passwordrecovery
        endpointRouteBuilder.MapControllerRoute(name: "PasswordRecovery",
            pattern: $"{lang}/passwordrecovery",
            defaults: new { controller = "Customer", action = "PasswordRecovery" });

        //password recovery confirmation
        endpointRouteBuilder.MapControllerRoute(name: "PasswordRecoveryConfirm",
            pattern: $"{lang}/passwordrecovery/confirm",
            defaults: new { controller = "Customer", action = "PasswordRecoveryConfirm" });

        //topics (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "TopicPopup",
            pattern: $"t-popup/{{SystemName}}",
            defaults: new { controller = "Topic", action = "TopicDetailsPopup" });

        //blog
        endpointRouteBuilder.MapControllerRoute(name: "BlogByTag",
            pattern: $"{lang}/blog/tag/{{tag}}",
            defaults: new { controller = "Blog", action = "BlogByTag" });

        endpointRouteBuilder.MapControllerRoute(name: "BlogByMonth",
            pattern: $"{lang}/blog/month/{{month}}",
            defaults: new { controller = "Blog", action = "BlogByMonth" });

        //blog RSS (file result)
        endpointRouteBuilder.MapControllerRoute(name: "BlogRSS",
            pattern: $"blog/rss/{{languageId:min(0)}}",
            defaults: new { controller = "Blog", action = "ListRss" });

        //news RSS (file result)
        endpointRouteBuilder.MapControllerRoute(name: "NewsRSS",
            pattern: $"news/rss/{{languageId:min(0)}}",
            defaults: new { controller = "News", action = "ListRss" });

        //set review helpfulness (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "SetProductReviewHelpfulness",
            pattern: $"setproductreviewhelpfulness",
            defaults: new { controller = "Product", action = "SetProductReviewHelpfulness" });

        //customer account links
        endpointRouteBuilder.MapControllerRoute(name: "CustomerReturnRequests",
            pattern: $"{lang}/returnrequest/history",
            defaults: new { controller = "ReturnRequest", action = "CustomerReturnRequests" });

        endpointRouteBuilder.MapControllerRoute(name: "CustomerDownloadableProducts",
            pattern: $"{lang}/customer/downloadableproducts",
            defaults: new { controller = "Customer", action = "DownloadableProducts" });

        endpointRouteBuilder.MapControllerRoute(name: "CustomerBackInStockSubscriptions",
            pattern: $"{lang}/backinstocksubscriptions/manage/{{pageNumber:int?}}",
            defaults: new { controller = "BackInStockSubscription", action = "CustomerSubscriptions" });

        //[Mahjar notifications] customer notification feed + settings (pages) and AJAX endpoints
        endpointRouteBuilder.MapControllerRoute(name: "CustomerNotifications",
            pattern: $"{lang}/customer/notifications",
            defaults: new { controller = "Notification", action = "Index" });
        endpointRouteBuilder.MapControllerRoute(name: "CustomerNotificationsPaged",
            pattern: $"{lang}/customer/notifications/page/{{pageNumber:min(0)}}",
            defaults: new { controller = "Notification", action = "Index" });
        endpointRouteBuilder.MapControllerRoute(name: "CustomerNotificationSettings",
            pattern: $"{lang}/customer/notification-settings",
            defaults: new { controller = "Notification", action = "Settings" });
        endpointRouteBuilder.MapControllerRoute(name: "NotificationFeed",
            pattern: $"notification/feed",
            defaults: new { controller = "Notification", action = "Feed" });
        endpointRouteBuilder.MapControllerRoute(name: "NotificationMarkRead",
            pattern: $"notification/markread",
            defaults: new { controller = "Notification", action = "MarkRead" });
        endpointRouteBuilder.MapControllerRoute(name: "NotificationMarkAllRead",
            pattern: $"notification/markallread",
            defaults: new { controller = "Notification", action = "MarkAllRead" });
        endpointRouteBuilder.MapControllerRoute(name: "NotificationSaveSettings",
            pattern: $"notification/savesettings",
            defaults: new { controller = "Notification", action = "SaveSettings" });

        //[Mahjar contracts] contract-approval stage: customer pages, sign/cancel posts and mobile API endpoints
        endpointRouteBuilder.MapControllerRoute(name: "CustomerContracts",
            pattern: $"{lang}/customer/contracts",
            defaults: new { controller = "Contract", action = "Index" });
        endpointRouteBuilder.MapControllerRoute(name: "ContractSubmit",
            pattern: $"contract/submit",
            defaults: new { controller = "Contract", action = "Submit" });
        endpointRouteBuilder.MapControllerRoute(name: "ContractSign",
            pattern: $"{lang}/contract/sign/{{token:guid}}",
            defaults: new { controller = "Contract", action = "Sign" });
        endpointRouteBuilder.MapControllerRoute(name: "ContractSignPost",
            pattern: $"contract/sign/{{token:guid}}",
            defaults: new { controller = "Contract", action = "SignPost" });
        endpointRouteBuilder.MapControllerRoute(name: "ContractCancel",
            pattern: $"contract/cancel/{{id:min(1)}}",
            defaults: new { controller = "Contract", action = "Cancel" });
        //[Mahjar handover] read-only contract page: document + signature + condition checks
        endpointRouteBuilder.MapControllerRoute(name: "ContractDetails",
            pattern: $"{lang}/contract/details/{{id:min(1)}}",
            defaults: new { controller = "Contract", action = "Details" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiContractGet",
            pattern: $"api/contract",
            defaults: new { controller = "ContractApi", action = "Current" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiContractRequest",
            pattern: $"api/contract/request",
            defaults: new { controller = "ContractApi", action = "Submit" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiContractSign",
            pattern: $"api/contract/sign",
            defaults: new { controller = "ContractApi", action = "Sign" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiContractCancel",
            pattern: $"api/contract/cancel",
            defaults: new { controller = "ContractApi", action = "Cancel" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiContractHistory",
            pattern: $"api/contract/history",
            defaults: new { controller = "ContractApi", action = "History" });
        //[Mahjar handover] one of the customer's own contracts, with its condition checks (int-constrained, so it
        //never shadows the literal request / sign / cancel / history segments above)
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiContractById",
            pattern: $"api/contract/{{id:min(1)}}",
            defaults: new { controller = "ContractApi", action = "ById" });

        endpointRouteBuilder.MapControllerRoute(name: "CustomerRewardPoints",
            pattern: $"{lang}/rewardpoints/history",
            defaults: new { controller = "Order", action = "CustomerRewardPoints" });

        endpointRouteBuilder.MapControllerRoute(name: "CustomerRewardPointsPaged",
            pattern: $"{lang}/rewardpoints/history/page/{{pageNumber:min(0)}}",
            defaults: new { controller = "Order", action = "CustomerRewardPoints" });

        endpointRouteBuilder.MapControllerRoute(name: "CustomerChangePassword",
            pattern: $"{lang}/customer/changepassword",
            defaults: new { controller = "Customer", action = "ChangePassword" });

        endpointRouteBuilder.MapControllerRoute(name: "CustomerAvatar",
            pattern: $"{lang}/customer/avatar",
            defaults: new { controller = "Customer", action = "Avatar" });

        endpointRouteBuilder.MapControllerRoute(name: "AccountActivation",
            pattern: $"{lang}/customer/activation",
            defaults: new { controller = "Customer", action = "AccountActivation" });

        endpointRouteBuilder.MapControllerRoute(name: "EmailRevalidation",
            pattern: $"{lang}/customer/revalidateemail",
            defaults: new { controller = "Customer", action = "EmailRevalidation" });

        endpointRouteBuilder.MapControllerRoute(name: "CustomerForumSubscriptions",
            pattern: $"{lang}/boards/forumsubscriptions/{{pageNumber:int?}}",
            defaults: new { controller = "Boards", action = "CustomerForumSubscriptions" });

        endpointRouteBuilder.MapControllerRoute(name: "CustomerAddressEdit",
            pattern: $"{lang}/customer/addressedit/{{addressId:min(0)}}",
            defaults: new { controller = "Customer", action = "AddressEdit" });

        endpointRouteBuilder.MapControllerRoute(name: "CustomerAddressAdd",
            pattern: $"{lang}/customer/addressadd",
            defaults: new { controller = "Customer", action = "AddressAdd" });

        endpointRouteBuilder.MapControllerRoute(name: "CustomerMultiFactorAuthenticationProviderConfig",
            pattern: $"{lang}/customer/providerconfig",
            defaults: new { controller = "Customer", action = "ConfigureMultiFactorAuthenticationProvider" });

        //customer profile page
        endpointRouteBuilder.MapControllerRoute(name: "CustomerProfile",
            pattern: $"{lang}/profile/{{id:min(0)}}",
            defaults: new { controller = "Profile", action = "Index" });

        endpointRouteBuilder.MapControllerRoute(name: "CustomerProfilePaged",
            pattern: $"{lang}/profile/{{id:min(0)}}/page/{{pageNumber:min(0)}}",
            defaults: new { controller = "Profile", action = "Index" });

        //orders
        endpointRouteBuilder.MapControllerRoute(name: "OrderDetails",
            pattern: $"{lang}/orderdetails/{{orderId:min(0)}}",
            defaults: new { controller = "Order", action = "Details" });

        endpointRouteBuilder.MapControllerRoute(name: "ShipmentDetails",
            pattern: $"{lang}/orderdetails/shipment/{{shipmentId}}",
            defaults: new { controller = "Order", action = "ShipmentDetails" });

        endpointRouteBuilder.MapControllerRoute(name: "ReturnRequest",
            pattern: $"{lang}/returnrequest/{{orderId:min(0)}}",
            defaults: new { controller = "ReturnRequest", action = "ReturnRequest" });

        endpointRouteBuilder.MapControllerRoute(name: "ReOrder",
            pattern: $"{lang}/reorder/{{orderId:min(0)}}",
            defaults: new { controller = "Order", action = "ReOrder" });

        //pdf invoice (file result)
        endpointRouteBuilder.MapControllerRoute(name: "GetOrderPdfInvoice",
            pattern: $"orderdetails/pdf/{{orderId}}",
            defaults: new { controller = "Order", action = "GetPdfInvoice" });

        endpointRouteBuilder.MapControllerRoute(name: "PrintOrderDetails",
            pattern: $"{lang}/orderdetails/print/{{orderId}}",
            defaults: new { controller = "Order", action = "PrintOrderDetails" });

        //order downloads (file result)
        endpointRouteBuilder.MapControllerRoute(name: "GetDownload",
            pattern: $"download/getdownload/{{orderItemId:guid}}/{{agree?}}",
            defaults: new { controller = "Download", action = "GetDownload" });

        endpointRouteBuilder.MapControllerRoute(name: "GetLicense",
            pattern: $"download/getlicense/{{orderItemId:guid}}/",
            defaults: new { controller = "Download", action = "GetLicense" });

        endpointRouteBuilder.MapControllerRoute(name: "DownloadUserAgreement",
            pattern: $"customer/useragreement/{{orderItemId:guid}}",
            defaults: new { controller = "Customer", action = "UserAgreement" });

        endpointRouteBuilder.MapControllerRoute(name: "GetOrderNoteFile",
            pattern: $"download/ordernotefile/{{ordernoteid:min(0)}}",
            defaults: new { controller = "Download", action = "GetOrderNoteFile" });

        //contact vendor
        endpointRouteBuilder.MapControllerRoute(name: "ContactVendor",
            pattern: $"{lang}/contactvendor/{{vendorId}}",
            defaults: new { controller = "Common", action = "ContactVendor" });

        //apply for vendor account
        endpointRouteBuilder.MapControllerRoute(name: "ApplyVendorAccount",
            pattern: $"{lang}/vendor/apply",
            defaults: new { controller = "Vendor", action = "ApplyVendor" });

        //vendor info
        endpointRouteBuilder.MapControllerRoute(name: "CustomerVendorInfo",
            pattern: $"{lang}/customer/vendorinfo",
            defaults: new { controller = "Vendor", action = "Info" });

        //customer GDPR
        endpointRouteBuilder.MapControllerRoute(name: "GdprTools",
            pattern: $"{lang}/customer/gdpr",
            defaults: new { controller = "Customer", action = "GdprTools" });

        //customer check gift card balance 
        endpointRouteBuilder.MapControllerRoute(name: "CheckGiftCardBalance",
            pattern: $"{lang}/customer/checkgiftcardbalance",
            defaults: new { controller = "Customer", action = "CheckGiftCardBalance" });

        //customer multi-factor authentication settings 
        endpointRouteBuilder.MapControllerRoute(name: "MultiFactorAuthenticationSettings",
            pattern: $"{lang}/customer/multifactorauthentication",
            defaults: new { controller = "Customer", action = "MultiFactorAuthentication" });

        //poll vote (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "PollVote",
            pattern: $"poll/vote",
            defaults: new { controller = "Poll", action = "Vote" });

        //comparing products
        endpointRouteBuilder.MapControllerRoute(name: "RemoveProductFromCompareList",
            pattern: $"{lang}/compareproducts/remove/{{productId}}",
            defaults: new { controller = "Product", action = "RemoveProductFromCompareList" });

        endpointRouteBuilder.MapControllerRoute(name: "ClearCompareList",
            pattern: $"{lang}/clearcomparelist/",
            defaults: new { controller = "Product", action = "ClearCompareList" });

        //new RSS (file result)
        endpointRouteBuilder.MapControllerRoute(name: "NewProductsRSS",
            pattern: $"newproducts/rss",
            defaults: new { controller = "Catalog", action = "NewProductsRss" });

        //get state list by country ID (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "GetStatesByCountryId",
            pattern: $"country/getstatesbycountryid/",
            defaults: new { controller = "Country", action = "GetStatesByCountryId" });

        //EU Cookie law accept button handler (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "EuCookieLawAccept",
            pattern: $"eucookielawaccept",
            defaults: new { controller = "Common", action = "EuCookieLawAccept" });

        //authenticate topic (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "TopicAuthenticate",
            pattern: $"topic/authenticate",
            defaults: new { controller = "Topic", action = "Authenticate" });

        //prepare top menu (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "GetCatalogRoot",
            pattern: $"catalog/getcatalogroot",
            defaults: new { controller = "Catalog", action = "GetCatalogRoot" });

        endpointRouteBuilder.MapControllerRoute(name: "GetCatalogSubCategories",
            pattern: $"catalog/getcatalogsubcategories",
            defaults: new { controller = "Catalog", action = "GetCatalogSubCategories" });

        //Catalog products (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "GetCategoryProducts",
            pattern: $"category/products/",
            defaults: new { controller = "Catalog", action = "GetCategoryProducts" });

        endpointRouteBuilder.MapControllerRoute(name: "GetManufacturerProducts",
            pattern: $"manufacturer/products/",
            defaults: new { controller = "Catalog", action = "GetManufacturerProducts" });

        endpointRouteBuilder.MapControllerRoute(name: "GetTagProducts",
            pattern: $"tag/products",
            defaults: new { controller = "Catalog", action = "GetTagProducts" });

        endpointRouteBuilder.MapControllerRoute(name: "SearchProducts",
            pattern: "product/search",
            defaults: new { controller = "Catalog", action = "SearchProducts" });

        endpointRouteBuilder.MapControllerRoute(name: "GetVendorProducts",
            pattern: $"vendor/products",
            defaults: new { controller = "Catalog", action = "GetVendorProducts" });

        endpointRouteBuilder.MapControllerRoute(name: "GetNewProducts",
            pattern: $"newproducts/products/",
            defaults: new { controller = "Catalog", action = "GetNewProducts" });

        //product combinations (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "GetProductCombinations",
            pattern: $"product/combinations",
            defaults: new { controller = "Product", action = "GetProductCombinations" });

        //product attributes with "upload file" type (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "UploadFileProductAttribute",
            pattern: $"uploadfileproductattribute/{{attributeId:min(0)}}",
            defaults: new { controller = "ShoppingCart", action = "UploadFileProductAttribute" });

        //checkout attributes with "upload file" type (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "UploadFileCheckoutAttribute",
            pattern: $"uploadfilecheckoutattribute/{{attributeId:min(0)}}",
            defaults: new { controller = "ShoppingCart", action = "UploadFileCheckoutAttribute" });

        //attribute change (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "ProductDetailsAttributeChange",
            pattern: $"shoppingcart/productdetails_attributechange/{{productId:min(0)}}/{{validateAttributeConditions}}/{{loadPicture}}",
            defaults: new { controller = "ShoppingCart", action = "ProductDetails_AttributeChange" });

        //rental booked dates (AJAX) [Mahjar buy-or-rent]
        endpointRouteBuilder.MapControllerRoute(name: "RentalBookedDates",
            pattern: $"rental/bookeddates/{{productId:min(0)}}",
            defaults: new { controller = "Rental", action = "BookedDates" });

        //read-only catalog REST API (mobile/headless) [Mahjar mobile-api]
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiProducts",
            pattern: $"api/products",
            defaults: new { controller = "CatalogApi", action = "Products" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiProduct",
            pattern: $"api/products/{{id:min(1)}}",
            defaults: new { controller = "CatalogApi", action = "Product" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiProductAttributes",
            pattern: $"api/products/{{id:min(1)}}/attributes",
            defaults: new { controller = "CatalogApi", action = "Attributes" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiCategories",
            pattern: $"api/categories",
            defaults: new { controller = "CatalogApi", action = "Categories" });

        //mobile API: auth + cart [Mahjar mobile-api]
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiRegister",
            pattern: $"api/auth/register",
            defaults: new { controller = "AuthApi", action = "Register" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiLogin",
            pattern: $"api/auth/login",
            defaults: new { controller = "AuthApi", action = "Login" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiForgotPassword",
            pattern: $"api/auth/forgot-password",
            defaults: new { controller = "AuthApi", action = "ForgotPassword" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiResetPassword",
            pattern: $"api/auth/reset-password",
            defaults: new { controller = "AuthApi", action = "ResetPassword" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiRefresh",
            pattern: $"api/auth/refresh",
            defaults: new { controller = "AuthApi", action = "Refresh" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiVerifyEmail",
            pattern: $"api/auth/verify-email",
            defaults: new { controller = "AuthApi", action = "VerifyEmail" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiLogout",
            pattern: $"api/auth/logout",
            defaults: new { controller = "AuthApi", action = "Logout" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiCartAdd",
            pattern: $"api/cart/add",
            defaults: new { controller = "CartApi", action = "Add" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiCartRemove",
            pattern: $"api/cart/{{itemId:min(1)}}",
            defaults: new { controller = "CartApi", action = "Remove" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiCartGet",
            pattern: $"api/cart",
            defaults: new { controller = "CartApi", action = "Get" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiCheckout",
            pattern: $"api/checkout",
            defaults: new { controller = "CheckoutApi", action = "Place" });

        //mobile API: catalog discovery + vendor storefront + orders [Mahjar mobile-api]
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiSearch",
            pattern: $"api/search",
            defaults: new { controller = "CatalogApi", action = "Search" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiManufacturers",
            pattern: $"api/manufacturers",
            defaults: new { controller = "CatalogApi", action = "Manufacturers" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiFeatured",
            pattern: $"api/products/featured",
            defaults: new { controller = "CatalogApi", action = "Featured" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiVendors",
            pattern: $"api/vendors",
            defaults: new { controller = "VendorsApi", action = "List" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiVendor",
            pattern: $"api/vendors/{{id:min(1)}}",
            defaults: new { controller = "VendorsApi", action = "Detail" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiVendorProducts",
            pattern: $"api/vendors/{{vendorId:min(1)}}/products",
            defaults: new { controller = "CatalogApi", action = "Products" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiOrders",
            pattern: $"api/orders",
            defaults: new { controller = "OrdersApi", action = "List" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiOrder",
            pattern: $"api/orders/{{id:min(1)}}",
            defaults: new { controller = "OrdersApi", action = "Detail" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiOrderCancel",
            pattern: $"api/orders/{{id:min(1)}}/cancel",
            defaults: new { controller = "OrdersApi", action = "Cancel" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiOrderReturn",
            pattern: $"api/orders/{{id:min(1)}}/return",
            defaults: new { controller = "OrdersApi", action = "ReturnRequest" });

        //mobile API: customer account — profile, password, address book [Mahjar mobile-api]
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiProfileGet",
            pattern: $"api/customer/profile",
            defaults: new { controller = "AccountApi", action = "Profile" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiProfileUpdate",
            pattern: $"api/customer/profile",
            defaults: new { controller = "AccountApi", action = "UpdateProfile" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiChangePassword",
            pattern: $"api/customer/change-password",
            defaults: new { controller = "AccountApi", action = "ChangePassword" });
        //[Mahjar app-batch3] the profile photo: POST uploads (multipart "file"), DELETE removes (same pattern, by verb)
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiAvatarUpload",
            pattern: $"api/customer/avatar",
            defaults: new { controller = "AccountApi", action = "UploadAvatar" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiAvatarDelete",
            pattern: $"api/customer/avatar",
            defaults: new { controller = "AccountApi", action = "DeleteAvatar" });
        //[Mahjar] "Become a seller": GET status + POST apply (same pattern, disambiguated by HTTP verb)
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiApplyVendorStatus",
            pattern: $"api/customer/apply-vendor",
            defaults: new { controller = "VendorApplyApi", action = "Status" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiApplyVendor",
            pattern: $"api/customer/apply-vendor",
            defaults: new { controller = "VendorApplyApi", action = "Apply" });
        //[Mahjar partner-agreement] the agreement rendered from a half-filled application, so the app can show
        //the applicant their own details in it
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiApplyVendorAgreement",
            pattern: $"api/customer/apply-vendor/agreement",
            defaults: new { controller = "VendorApplyApi", action = "Agreement" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiAddressesGet",
            pattern: $"api/customer/addresses",
            defaults: new { controller = "AccountApi", action = "Addresses" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiAddressAdd",
            pattern: $"api/customer/addresses",
            defaults: new { controller = "AccountApi", action = "AddAddress" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiAddressUpdate",
            pattern: $"api/customer/addresses/{{id:min(1)}}",
            defaults: new { controller = "AccountApi", action = "UpdateAddress" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiAddressDelete",
            pattern: $"api/customer/addresses/{{id:min(1)}}",
            defaults: new { controller = "AccountApi", action = "DeleteAddress" });

        //mobile API: wishlist [Mahjar mobile-api]
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiWishlistGet",
            pattern: $"api/wishlist",
            defaults: new { controller = "WishlistApi", action = "Get" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiWishlistAdd",
            pattern: $"api/wishlist/add",
            defaults: new { controller = "WishlistApi", action = "Add" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiWishlistMove",
            pattern: $"api/wishlist/move-to-cart",
            defaults: new { controller = "WishlistApi", action = "MoveToCart" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiWishlistRemove",
            pattern: $"api/wishlist/{{itemId:min(1)}}",
            defaults: new { controller = "WishlistApi", action = "Remove" });

        //mobile API: reference / config data [Mahjar mobile-api]
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiCountries",
            pattern: $"api/countries",
            defaults: new { controller = "ReferenceApi", action = "Countries" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiStates",
            pattern: $"api/countries/{{id:min(1)}}/states",
            defaults: new { controller = "ReferenceApi", action = "States" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiCurrencies",
            pattern: $"api/currencies",
            defaults: new { controller = "ReferenceApi", action = "Currencies" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiLanguages",
            pattern: $"api/languages",
            defaults: new { controller = "ReferenceApi", action = "Languages" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiTopic",
            pattern: $"api/topics/{{systemName}}",
            defaults: new { controller = "ReferenceApi", action = "Topic" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiStoreInfo",
            pattern: $"api/store/info",
            defaults: new { controller = "ReferenceApi", action = "StoreInfo" });

        //mobile API: admin-managed home carousel slides [Mahjar home-slides]
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiHomeSlides",
            pattern: $"api/home/slides",
            defaults: new { controller = "HomeApi", action = "Slides" });

        //mobile API: the website's home categories as app cards (picture + product count) [Mahjar app-home]
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiHomeCategories",
            pattern: $"api/home/categories",
            defaults: new { controller = "HomeApi", action = "Categories" });

        //mobile API: product reviews [Mahjar mobile-api]
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiReviewsGet",
            pattern: $"api/products/{{id:min(1)}}/reviews",
            defaults: new { controller = "ReviewsApi", action = "List" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiReviewAdd",
            pattern: $"api/products/{{id:min(1)}}/reviews",
            defaults: new { controller = "ReviewsApi", action = "Add" });

        //mobile API: cart extensions — update qty, clear, coupon [Mahjar mobile-api]
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiCartUpdate",
            pattern: $"api/cart/{{itemId:min(1)}}",
            defaults: new { controller = "CartApi", action = "Update" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiCartClear",
            pattern: $"api/cart",
            defaults: new { controller = "CartApi", action = "Clear" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiCartCoupon",
            pattern: $"api/cart/coupon",
            defaults: new { controller = "CartApi", action = "ApplyCoupon" });

        //mobile API: checkout extensions — totals preview, shipping options, payment methods [Mahjar mobile-api]
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiCheckoutTotals",
            pattern: $"api/checkout/totals",
            defaults: new { controller = "CheckoutApi", action = "Totals" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiCheckoutShipping",
            pattern: $"api/checkout/shipping-options",
            defaults: new { controller = "CheckoutApi", action = "ShippingOptions" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiCheckoutPayments",
            pattern: $"api/checkout/payment-methods",
            defaults: new { controller = "CheckoutApi", action = "PaymentMethods" });

        //mobile API: Tier-B buyer features — loyalty, gift cards, reorder, estimate shipping,
        //returns (RMA), back-in-stock, vendor reviews, contact [Mahjar mobile-api]
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiRewardPoints",
            pattern: $"api/customer/reward-points",
            defaults: new { controller = "AccountApi", action = "RewardPoints" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiGiftCardBalance",
            pattern: $"api/gift-cards/balance",
            defaults: new { controller = "AccountApi", action = "GiftCardBalance" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiCartGiftCard",
            pattern: $"api/cart/gift-card",
            defaults: new { controller = "CartApi", action = "ApplyGiftCard" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiOrderReorder",
            pattern: $"api/orders/{{id:min(1)}}/reorder",
            defaults: new { controller = "OrdersApi", action = "ReOrder" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiCartEstimateShipping",
            pattern: $"api/cart/estimate-shipping",
            defaults: new { controller = "CheckoutApi", action = "EstimateShipping" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiVendorReviews",
            pattern: $"api/vendors/{{id:min(1)}}/reviews",
            defaults: new { controller = "VendorsApi", action = "Reviews" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiBackInStockSubscribe",
            pattern: $"api/products/{{id:min(1)}}/back-in-stock",
            defaults: new { controller = "BackInStockApi", action = "Subscribe" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiBackInStockUnsubscribe",
            pattern: $"api/products/{{id:min(1)}}/back-in-stock",
            defaults: new { controller = "BackInStockApi", action = "Unsubscribe" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiBackInStockList",
            pattern: $"api/back-in-stock",
            defaults: new { controller = "BackInStockApi", action = "List" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiContactUs",
            pattern: $"api/contact-us",
            defaults: new { controller = "SupportApi", action = "ContactUs" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiContactVendor",
            pattern: $"api/vendors/{{id:min(1)}}/contact",
            defaults: new { controller = "SupportApi", action = "ContactVendor" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiReturns",
            pattern: $"api/returns",
            defaults: new { controller = "ReturnsApi", action = "List" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiOrderReturnable",
            pattern: $"api/orders/{{id:min(1)}}/returnable",
            defaults: new { controller = "ReturnsApi", action = "Returnable" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiOrderReturnRequest",
            pattern: $"api/orders/{{id:min(1)}}/return-request",
            defaults: new { controller = "ReturnsApi", action = "Submit" });

        //mobile API: content — blog, news, newsletter [Mahjar mobile-api]
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiBlog",
            pattern: $"api/blog",
            defaults: new { controller = "BlogApi", action = "List" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiBlogPost",
            pattern: $"api/blog/{{id:min(1)}}",
            defaults: new { controller = "BlogApi", action = "Detail" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiBlogComment",
            pattern: $"api/blog/{{id:min(1)}}/comments",
            defaults: new { controller = "BlogApi", action = "AddComment" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiNews",
            pattern: $"api/news",
            defaults: new { controller = "NewsApi", action = "List" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiNewsItem",
            pattern: $"api/news/{{id:min(1)}}",
            defaults: new { controller = "NewsApi", action = "Detail" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiNewsComment",
            pattern: $"api/news/{{id:min(1)}}/comments",
            defaults: new { controller = "NewsApi", action = "AddComment" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiNewsletterSubscribe",
            pattern: $"api/newsletter/subscribe",
            defaults: new { controller = "NewsletterApi", action = "Subscribe" });

        //mobile API: in-app notifications [Mahjar notifications]
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiNotifications",
            pattern: $"api/notifications",
            defaults: new { controller = "NotificationsApi", action = "List" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiNotificationsUnreadCount",
            pattern: $"api/notifications/unread-count",
            defaults: new { controller = "NotificationsApi", action = "UnreadCount" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiNotificationRead",
            pattern: $"api/notifications/{{id:min(1)}}/read",
            defaults: new { controller = "NotificationsApi", action = "MarkRead" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiNotificationsReadAll",
            pattern: $"api/notifications/read-all",
            defaults: new { controller = "NotificationsApi", action = "MarkAllRead" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiNotificationPreferencesGet",
            pattern: $"api/notifications/preferences",
            defaults: new { controller = "NotificationsApi", action = "GetPreferences" });
        endpointRouteBuilder.MapControllerRoute(name: "MahjarApiNotificationPreferencesPut",
            pattern: $"api/notifications/preferences",
            defaults: new { controller = "NotificationsApi", action = "UpdatePreferences" });

        //return request with "upload file" support (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "UploadFileReturnRequest",
            pattern: $"uploadfilereturnrequest",
            defaults: new { controller = "ReturnRequest", action = "UploadFileReturnRequest" });

        //forums
        endpointRouteBuilder.MapControllerRoute(name: "ActiveDiscussions",
            pattern: $"{lang}/boards/activediscussions",
            defaults: new { controller = "Boards", action = "ActiveDiscussions" });

        endpointRouteBuilder.MapControllerRoute(name: "ActiveDiscussionsPaged",
            pattern: $"{lang}/boards/activediscussions/page/{{pageNumber:int}}",
            defaults: new { controller = "Boards", action = "ActiveDiscussions" });

        //forums RSS (file result)
        endpointRouteBuilder.MapControllerRoute(name: "ActiveDiscussionsRSS",
            pattern: $"boards/activediscussionsrss",
            defaults: new { controller = "Boards", action = "ActiveDiscussionsRSS" });

        endpointRouteBuilder.MapControllerRoute(name: "PostEdit",
            pattern: $"{lang}/boards/postedit/{{id:min(0)}}",
            defaults: new { controller = "Boards", action = "PostEdit" });

        endpointRouteBuilder.MapControllerRoute(name: "PostDelete",
            pattern: $"{lang}/boards/postdelete/{{id:int?}}",
            defaults: new { controller = "Boards", action = "PostDelete" });

        endpointRouteBuilder.MapControllerRoute(name: "PostCreate",
            pattern: $"{lang}/boards/postcreate/{{id:min(0)}}",
            defaults: new { controller = "Boards", action = "PostCreate" });

        endpointRouteBuilder.MapControllerRoute(name: "PostCreateQuote",
            pattern: $"{lang}/boards/postcreate/{{id:min(0)}}/{{quote:min(0)}}",
            defaults: new { controller = "Boards", action = "PostCreate" });

        endpointRouteBuilder.MapControllerRoute(name: "TopicEdit",
            pattern: $"{lang}/boards/topicedit/{{id:min(0)}}",
            defaults: new { controller = "Boards", action = "TopicEdit" });

        endpointRouteBuilder.MapControllerRoute(name: "TopicDelete",
            pattern: $"{lang}/boards/topicdelete/{{id:int?}}",
            defaults: new { controller = "Boards", action = "TopicDelete" });

        endpointRouteBuilder.MapControllerRoute(name: "TopicCreate",
            pattern: $"{lang}/boards/topiccreate/{{id:min(0)}}",
            defaults: new { controller = "Boards", action = "TopicCreate" });

        endpointRouteBuilder.MapControllerRoute(name: "TopicMove",
            pattern: $"{lang}/boards/topicmove/{{id:min(0)}}",
            defaults: new { controller = "Boards", action = "TopicMove" });

        //topic watch (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "TopicWatch",
            pattern: $"boards/topicwatch/{{id:min(0)}}",
            defaults: new { controller = "Boards", action = "TopicWatch" });

        endpointRouteBuilder.MapControllerRoute(name: "TopicSlug",
            pattern: $"{lang}/boards/topic/{{id:min(0)}}/{{slug?}}",
            defaults: new { controller = "Boards", action = "Topic" });

        endpointRouteBuilder.MapControllerRoute(name: "TopicSlugPaged",
            pattern: $"{lang}/boards/topic/{{id:min(0)}}/{{slug?}}/page/{{pageNumber:int}}",
            defaults: new { controller = "Boards", action = "Topic" });

        //forum watch (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "ForumWatch",
            pattern: $"boards/forumwatch/{{id:min(0)}}",
            defaults: new { controller = "Boards", action = "ForumWatch" });

        //forums RSS (file result)
        endpointRouteBuilder.MapControllerRoute(name: "ForumRSS",
            pattern: $"boards/forumrss/{{id:min(0)}}",
            defaults: new { controller = "Boards", action = "ForumRSS" });

        endpointRouteBuilder.MapControllerRoute(name: "ForumSlug",
            pattern: $"{lang}/boards/forum/{{id:min(0)}}/{{slug?}}",
            defaults: new { controller = "Boards", action = "Forum" });

        endpointRouteBuilder.MapControllerRoute(name: "ForumSlugPaged",
            pattern: $"{lang}/boards/forum/{{id:min(0)}}/{{slug?}}/page/{{pageNumber:int}}",
            defaults: new { controller = "Boards", action = "Forum" });

        endpointRouteBuilder.MapControllerRoute(name: "ForumGroupSlug",
            pattern: $"{lang}/boards/forumgroup/{{id:min(0)}}/{{slug?}}",
            defaults: new { controller = "Boards", action = "ForumGroup" });

        endpointRouteBuilder.MapControllerRoute(name: "Search",
            pattern: $"{lang}/boards/search",
            defaults: new { controller = "Boards", action = "Search" });

        //post vote (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "PostVote",
            pattern: "boards/postvote",
            defaults: new { controller = "Boards", action = "PostVote" });

        //private messages
        endpointRouteBuilder.MapControllerRoute(name: "PrivateMessages",
            pattern: $"{lang}/privatemessages/{{tab?}}",
            defaults: new { controller = "PrivateMessages", action = "Index" });

        endpointRouteBuilder.MapControllerRoute(name: "PrivateMessagesPaged",
            pattern: $"{lang}/privatemessages/{{tab?}}/page/{{pageNumber:min(0)}}",
            defaults: new { controller = "PrivateMessages", action = "Index" });

        endpointRouteBuilder.MapControllerRoute(name: "PrivateMessagesInbox",
            pattern: $"{lang}/inboxupdate",
            defaults: new { controller = "PrivateMessages", action = "InboxUpdate" });

        endpointRouteBuilder.MapControllerRoute(name: "PrivateMessagesSent",
            pattern: $"{lang}/sentupdate",
            defaults: new { controller = "PrivateMessages", action = "SentUpdate" });

        endpointRouteBuilder.MapControllerRoute(name: "SendPM",
            pattern: $"{lang}/sendpm/{{toCustomerId:min(0)}}",
            defaults: new { controller = "PrivateMessages", action = "SendPM" });

        endpointRouteBuilder.MapControllerRoute(name: "SendPMReply",
            pattern: $"{lang}/sendpm/{{toCustomerId:min(0)}}/{{replyToMessageId:min(0)}}",
            defaults: new { controller = "PrivateMessages", action = "SendPM" });

        endpointRouteBuilder.MapControllerRoute(name: "ViewPM",
            pattern: $"{lang}/viewpm/{{privateMessageId:min(0)}}",
            defaults: new { controller = "PrivateMessages", action = "ViewPM" });

        endpointRouteBuilder.MapControllerRoute(name: "DeletePM",
            pattern: $"{lang}/deletepm/{{privateMessageId:min(0)}}",
            defaults: new { controller = "PrivateMessages", action = "DeletePM" });

        //activate newsletters
        endpointRouteBuilder.MapControllerRoute(name: "NewsletterActivation",
            pattern: $"{lang}/newsletter/subscriptionactivation/{{token:guid}}/{{active}}",
            defaults: new { controller = "Newsletter", action = "SubscriptionActivation" });

        //robots.txt (file result)
        endpointRouteBuilder.MapControllerRoute(name: "robots.txt",
            pattern: $"robots.txt",
            defaults: new { controller = "Common", action = "RobotsTextFile" });

        //sitemap
        endpointRouteBuilder.MapControllerRoute(name: "Sitemap",
            pattern: $"{lang}/sitemap",
            defaults: new { controller = "Common", action = "Sitemap" });

        //sitemap.xml (file result)
        endpointRouteBuilder.MapControllerRoute(name: "sitemap.xml",
            pattern: $"sitemap.xml",
            defaults: new { controller = "Common", action = "SitemapXml" });

        endpointRouteBuilder.MapControllerRoute(name: "sitemap-indexed.xml",
            pattern: $"sitemap-{{Id:min(0)}}.xml",
            defaults: new { controller = "Common", action = "SitemapXml" });

        //store closed
        endpointRouteBuilder.MapControllerRoute(name: "StoreClosed",
            pattern: $"{lang}/storeclosed",
            defaults: new { controller = "Common", action = "StoreClosed" });

        //install
        endpointRouteBuilder.MapControllerRoute(name: "Installation",
            pattern: $"{NopInstallationDefaults.InstallPath}",
            defaults: new { controller = "Install", action = "Index" });

        endpointRouteBuilder.MapControllerRoute(name: "InstallationChangeLanguage",
            pattern: $"{NopInstallationDefaults.InstallPath}/ChangeLanguage/{{language}}",
            defaults: new { controller = "Install", action = "ChangeLanguage" });

        //restart application (AJAX)
        endpointRouteBuilder.MapControllerRoute(name: "InstallationRestartApplication",
            pattern: $"{NopInstallationDefaults.InstallPath}/restartapplication",
            defaults: new { controller = "Install", action = "RestartApplication" });

        //page not found
        endpointRouteBuilder.MapControllerRoute(name: "PageNotFound",
            pattern: $"{lang}/page-not-found",
            defaults: new { controller = "Common", action = "PageNotFound" });

        //fallback is intended to handle cases when no other endpoint has matched
        //we use it to invoke [CheckLanguageSeoCode] and give a chance to find a localized route
        endpointRouteBuilder.MapFallbackToController("FallbackRedirect", "Common");
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets a priority of route provider
    /// </summary>
    public int Priority => 0;

    #endregion
}