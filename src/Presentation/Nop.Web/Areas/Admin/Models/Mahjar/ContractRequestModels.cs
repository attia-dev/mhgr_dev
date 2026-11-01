// [Mahjar contracts]
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Models.Mahjar;

/// <summary>
/// Represents a contract request search model
/// </summary>
public partial record ContractRequestSearchModel : BaseSearchModel
{
    #region Ctor

    public ContractRequestSearchModel()
    {
        AvailableStatuses = new List<SelectListItem>();
    }

    #endregion

    #region Properties

    [NopResourceDisplayName("admin.mahjar.contracts.list.searchstatus")]
    public int StatusId { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.list.searchcreatedfrom")]
    [UIHint("DateNullable")]
    public DateTime? CreatedFrom { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.list.searchcreatedto")]
    [UIHint("DateNullable")]
    public DateTime? CreatedTo { get; set; }

    public IList<SelectListItem> AvailableStatuses { get; set; }

    #endregion
}

/// <summary>
/// Represents a contract request list model
/// </summary>
public partial record ContractRequestListModel : BasePagedListModel<ContractRequestModel>
{
}

/// <summary>
/// Represents a contract request model
/// </summary>
public partial record ContractRequestModel : BaseNopEntityModel
{
    #region Ctor

    public ContractRequestModel()
    {
        SnapshotLines = new List<ContractSnapshotLineModel>();
    }

    #endregion

    #region Properties

    public string Number { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.customer")]
    public int CustomerId { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.customer")]
    public string CustomerEmail { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.customer")]
    public string CustomerFullName { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.createdon")]
    public DateTime CreatedOn { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.status")]
    public string Status { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.status")]
    public int StatusId { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.total")]
    public string GrandTotal { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.signedon")]
    public DateTime? SignedOn { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.signedname")]
    public string SignedName { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.signedip")]
    public string SignedIp { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.signature")]
    public string SignatureDataUrl { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.admincomment")]
    public string AdminComment { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.rejectionreason")]
    public string RejectionReason { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.contractbody")]
    public string ContractBody { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.order")]
    public int OrderId { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.language")]
    public string LanguageName { get; set; }

    /// <summary>[Mahjar contracts] which template kind the frozen cart resolves to (purchase / rental / mixed).</summary>
    [NopResourceDisplayName("admin.mahjar.contracts.fields.kind")]
    public string Kind { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.snapshot")]
    public IList<ContractSnapshotLineModel> SnapshotLines { get; set; }

    public string SubTotal { get; set; }

    public string DepositTotal { get; set; }

    public bool CanSendContract { get; set; }

    public bool CanApprove { get; set; }

    public bool CanReject { get; set; }

    /// <summary>[Mahjar handover] whether the viewer may change the request (handover staff only view it).</summary>
    public bool CanManage { get; set; } = true;

    /// <summary>[Mahjar handover] whether the viewer may work the handover desk (open / start condition checks).</summary>
    public bool CanManageHandovers { get; set; }

    /// <summary>[Mahjar handover] whether every item has its before-signing condition media (required to send).</summary>
    public bool PreContractReady { get; set; } = true;

    /// <summary>[Mahjar handover] the condition checks of the request and of the order placed against it.</summary>
    public IList<ContractConditionCheckRowModel> ConditionChecks { get; set; } = new List<ContractConditionCheckRowModel>();

    #endregion

    #region The contract values of this transaction ([Mahjar contracts])

    //Each overrides the matching default in Contract settings for this contract only; empty = use the setting.
    //They can only be changed while the contract has not been sent, because sending freezes the rendered body.

    [NopResourceDisplayName("admin.mahjar.contracts.fields.deliverydetails")]
    public string DeliveryDetails { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.returndetails")]
    public string ReturnDetails { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.verificationreference")]
    public string VerificationReference { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.guaranteetype")]
    public string GuaranteeType { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.guaranteeamount")]
    [UIHint("DecimalNullable")]
    public decimal? GuaranteeAmount { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.extrafees")]
    public string ExtraFees { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.taxamount")]
    [UIHint("DecimalNullable")]
    public decimal? TaxAmount { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.fields.latefee")]
    public string LateReturnFee { get; set; }

    /// <summary>Whether the transaction values may still be edited (the contract has not been sent yet).</summary>
    public bool CanEditTerms { get; set; }

    #endregion
}

/// <summary>
/// Represents a single frozen cart line of a contract request
/// </summary>
public partial record ContractSnapshotLineModel : BaseNopModel
{
    public int ProductId { get; set; }

    public string Name { get; set; }

    public string Sku { get; set; }

    public string Attributes { get; set; }

    public string Mode { get; set; }

    public int Quantity { get; set; }

    public string Period { get; set; }

    public string UnitPrice { get; set; }

    public string Deposit { get; set; }

    public string LineTotal { get; set; }
}

/// <summary>
/// Represents a contract template search model
/// </summary>
public partial record ContractTemplateSearchModel : BaseSearchModel
{
}

/// <summary>
/// Represents a contract template list model
/// </summary>
public partial record ContractTemplateListModel : BasePagedListModel<ContractTemplateContentModel>
{
}

/// <summary>
/// Represents the contract template content model (admin-editable, localized). One row per contract kind —
/// purchase, rental and mixed — so a buy-only cart is never sent rental clauses. [Mahjar contracts]
/// </summary>
public partial record ContractTemplateContentModel : BaseNopEntityModel, ILocalizedModel<ContractTemplateLocalizedModel>
{
    #region Ctor

    public ContractTemplateContentModel()
    {
        Locales = new List<ContractTemplateLocalizedModel>();
    }

    #endregion

    #region Properties

    [NopResourceDisplayName("admin.mahjar.contracts.template.fields.name")]
    public string Name { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.template.fields.type")]
    public int TemplateTypeId { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.template.fields.type")]
    public string TemplateType { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.template.fields.updatedon")]
    public DateTime UpdatedOn { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.template.body")]
    public string Body { get; set; }

    public IList<ContractTemplateLocalizedModel> Locales { get; set; }

    #endregion
}

/// <summary>
/// Represents the localized contract template content model
/// </summary>
public partial record ContractTemplateLocalizedModel : ILocalizedLocaleModel
{
    public int LanguageId { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.template.body")]
    public string Body { get; set; }
}

/// <summary>
/// Represents the contract settings model: the contract stage itself, plus the store-level values the contract
/// templates print through their tokens. The free-text values are localized (one text per language). [Mahjar contracts]
/// </summary>
public partial record ContractSettingsModel : BaseNopModel, ILocalizedModel<ContractSettingsLocalizedModel>
{
    #region Ctor

    public ContractSettingsModel()
    {
        Locales = new List<ContractSettingsLocalizedModel>();
    }

    #endregion

    [NopResourceDisplayName("admin.mahjar.contracts.settings.enabled")]
    public bool Enabled { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.requireonlyforrentals")]
    public bool RequireOnlyForRentals { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.commercialregistration")]
    public string CommercialRegistrationNumber { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.agreementversion")]
    public string AgreementVersion { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.effectivedate")]
    [UIHint("DateNullable")]
    public DateTime? AgreementEffectiveDate { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.updateddate")]
    [UIHint("DateNullable")]
    public DateTime? AgreementUpdatedDate { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.itemvaluecap")]
    public decimal RentalItemValueCap { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.lategracehours")]
    public int LateReturnGraceHours { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.refunddays")]
    public int RefundProcessingDays { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.objectiondays")]
    public int DamageObjectionDays { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.complaintdays")]
    public int ComplaintHandlingDays { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.settlementdays")]
    public int AmicableSettlementDays { get; set; }

    //the localized texts, also kept here for the "Standard" (non-localized) tab
    [NopResourceDisplayName("admin.mahjar.contracts.settings.operatorlegalname")]
    public string OperatorLegalName { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.supportchannel")]
    public string SupportChannel { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.latefeeterms")]
    public string LateReturnFeeTerms { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.cancellationterms")]
    public string CancellationFeeTerms { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.guaranteetype")]
    public string GuaranteeType { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.deliverydetails")]
    public string DeliveryDetails { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.returndetails")]
    public string ReturnDetails { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.extrafees")]
    public string ExtraFeesTerms { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.taxterms")]
    public string TaxTerms { get; set; }

    public IList<ContractSettingsLocalizedModel> Locales { get; set; }
}

/// <summary>
/// Represents the localized contract settings model — the contract wording in one language. [Mahjar contracts]
/// </summary>
public partial record ContractSettingsLocalizedModel : ILocalizedLocaleModel
{
    public int LanguageId { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.operatorlegalname")]
    public string OperatorLegalName { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.supportchannel")]
    public string SupportChannel { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.latefeeterms")]
    public string LateReturnFeeTerms { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.cancellationterms")]
    public string CancellationFeeTerms { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.guaranteetype")]
    public string GuaranteeType { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.deliverydetails")]
    public string DeliveryDetails { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.returndetails")]
    public string ReturnDetails { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.extrafees")]
    public string ExtraFeesTerms { get; set; }

    [NopResourceDisplayName("admin.mahjar.contracts.settings.taxterms")]
    public string TaxTerms { get; set; }
}
