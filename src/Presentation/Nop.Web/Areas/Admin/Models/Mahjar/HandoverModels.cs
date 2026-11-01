// [Mahjar handover]
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Areas.Admin.Models.Mahjar;

/// <summary>
/// Represents the handover desk search model (one work queue per stage)
/// </summary>
public partial record HandoverSearchModel : BaseSearchModel
{
    public HandoverSearchModel()
    {
        AvailableStages = new List<SelectListItem>();
        AvailableAssignees = new List<SelectListItem>();
        AvailableStatuses = new List<SelectListItem>();
    }

    [NopResourceDisplayName("admin.mahjar.handover.list.stage")]
    public int StageId { get; set; }

    /// <summary>[Mahjar handover] Item name or SKU, customer name or e-mail, order number, contract number.</summary>
    [NopResourceDisplayName("admin.mahjar.handover.list.keyword")]
    public string SearchKeyword { get; set; }

    /// <summary>[Mahjar handover] 0 = anyone, -1 = waiting to be assigned, otherwise that employee.</summary>
    [NopResourceDisplayName("admin.mahjar.handover.fields.assignedto")]
    public int SearchAssignedCustomerId { get; set; }

    /// <summary>[Mahjar handover] 0 = any, -1 = not started, otherwise a condition-check status.</summary>
    [NopResourceDisplayName("admin.mahjar.handover.fields.status")]
    public int SearchStatusId { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.list.startdate")]
    [UIHint("DateNullable")]
    public DateTime? SearchStartDate { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.list.enddate")]
    [UIHint("DateNullable")]
    public DateTime? SearchEndDate { get; set; }

    /// <summary>[Mahjar handover] Only rentals whose return date has already passed.</summary>
    [NopResourceDisplayName("admin.mahjar.handover.list.overdueonly")]
    public bool SearchOverdueOnly { get; set; }

    public IList<SelectListItem> AvailableStages { get; set; }

    public IList<SelectListItem> AvailableAssignees { get; set; }

    public IList<SelectListItem> AvailableStatuses { get; set; }
}

/// <summary>
/// Represents a handover desk queue list model
/// </summary>
public partial record HandoverTaskListModel : BasePagedListModel<HandoverTaskModel>
{
}

/// <summary>
/// Represents one row of a handover desk queue
/// </summary>
public partial record HandoverTaskModel : BaseNopModel
{
    /// <summary>The check already started for the row; 0 = not started.</summary>
    public int CheckId { get; set; }

    public int CheckTypeId { get; set; }

    public string CheckType { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; }

    public int ContractRequestId { get; set; }

    public int OrderId { get; set; }

    public int OrderItemId { get; set; }

    public string Reference { get; set; }

    public int CustomerId { get; set; }

    public string CustomerInfo { get; set; }

    /// <summary>[Mahjar handover] The employee the order's handover is assigned to; 0 = nobody yet.</summary>
    public int AssignedCustomerId { get; set; }

    /// <summary>[Mahjar handover] That employee's name, or the "not assigned yet" text.</summary>
    public string AssigneeInfo { get; set; }

    public string Period { get; set; }

    public bool IsOverdue { get; set; }

    public string Status { get; set; }

    public string Media { get; set; }

    public DateTime Date { get; set; }
}

/// <summary>
/// Represents the condition check editor (one checkpoint of one item)
/// </summary>
public partial record ConditionCheckModel : BaseNopEntityModel
{
    public ConditionCheckModel()
    {
        AllowedVideoExtensions = new List<string>();
        Pictures = new List<ConditionCheckPictureModel>();
        PictureSearchModel = new ConditionCheckPictureSearchModel();
        AddPictureModel = new ConditionCheckPictureModel();
    }

    #region Context (read-only)

    public int CheckTypeId { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.fields.type")]
    public string CheckType { get; set; }

    public int StatusId { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.fields.status")]
    public string Status { get; set; }

    public bool IsEditable { get; set; }

    public int ProductId { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.fields.product")]
    public string ProductName { get; set; }

    public string ProductSku { get; set; }

    public string VendorName { get; set; }

    public int ContractRequestId { get; set; }

    public string ContractNumber { get; set; }

    public int OrderId { get; set; }

    public string OrderNumber { get; set; }

    public int OrderItemId { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.fields.reference")]
    public string Reference { get; set; }

    public int CustomerId { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.fields.customer")]
    public string CustomerInfo { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.fields.period")]
    public string Period { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.fields.createdby")]
    public string CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.fields.confirmedby")]
    public string ConfirmedBy { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.fields.confirmedon")]
    public DateTime? ConfirmedOn { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.fields.deposit")]
    public string DepositText { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.fields.refunddue")]
    public string RefundText { get; set; }

    public string Hint { get; set; }

    public string ConfirmButtonText { get; set; }

    public bool CanConfirm { get; set; }

    public bool CanImportFromProduct { get; set; }

    public int MaxVideoSizeMb { get; set; }

    public IList<string> AllowedVideoExtensions { get; set; }

    public string VideoFile { get; set; }

    #endregion

    #region Editable fields

    [NopResourceDisplayName("admin.mahjar.handover.fields.grade")]
    public int ConditionGradeId { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.fields.conditionnotes")]
    public string ConditionNotes { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.fields.hasdamage")]
    public bool HasDamage { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.fields.damageseverity")]
    public int DamageSeverityId { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.fields.damagedescription")]
    public string DamageDescription { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.fields.internalcomment")]
    public string InternalComment { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.fields.damagededuction")]
    public decimal DamageDeduction { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.fields.videourl")]
    public string VideoUrl { get; set; }

    #endregion

    #region Media

    public ConditionCheckPictureSearchModel PictureSearchModel { get; set; }

    public ConditionCheckPictureModel AddPictureModel { get; set; }

    public IList<ConditionCheckPictureModel> Pictures { get; set; }

    #endregion
}

/// <summary>
/// Represents a condition check photo search model
/// </summary>
public partial record ConditionCheckPictureSearchModel : BaseSearchModel
{
    public int ConditionCheckId { get; set; }

    public bool IsEditable { get; set; }
}

/// <summary>
/// Represents a condition check photo list model
/// </summary>
public partial record ConditionCheckPictureListModel : BasePagedListModel<ConditionCheckPictureModel>
{
}

/// <summary>
/// Represents one photo of a condition check
/// </summary>
public partial record ConditionCheckPictureModel : BaseNopEntityModel
{
    public int ConditionCheckId { get; set; }

    [UIHint("MultiPicture")]
    [NopResourceDisplayName("admin.mahjar.handover.fields.pictureid")]
    public int PictureId { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.fields.photo")]
    public string PictureUrl { get; set; }

    public string FullSizeUrl { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.fields.caption")]
    public string Caption { get; set; }

    [NopResourceDisplayName("admin.mahjar.handover.fields.displayorder")]
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Represents the "record an intake" page (pick the product the partner delivered)
/// </summary>
public partial record HandoverIntakeModel : BaseNopModel
{
    [NopResourceDisplayName("admin.mahjar.handover.intake.search")]
    public string SearchTerm { get; set; }
}

/// <summary>
/// Represents one condition check row on the admin contract request page
/// </summary>
public partial record ContractConditionCheckRowModel : BaseNopModel
{
    /// <summary>The check; 0 = not started yet.</summary>
    public int CheckId { get; set; }

    public int CheckTypeId { get; set; }

    public string CheckType { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; }

    public string Status { get; set; }

    public string Media { get; set; }

    public bool HasMedia { get; set; }

    public DateTime? Date { get; set; }

    /// <summary>Whether a before-signing check can still be started for the row (the contract is Pending).</summary>
    public bool CanStart { get; set; }
}
