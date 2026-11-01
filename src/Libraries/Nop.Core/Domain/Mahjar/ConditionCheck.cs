// [Mahjar handover]
namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// One documented condition check of one physical item at one checkpoint of its life: when the platform receives it
/// from the partner (intake), next to the contract before the customer signs (pre-contract), when it is handed over
/// to the customer (delivery) and when a rented item comes back (return). Each check carries photos
/// (<see cref="ConditionCheckPicture"/>), at most one video, the condition grade, notes the customer may read, the
/// damage found, an internal comment and, for a return, the deduction from the security deposit.
///
/// A check is a <see cref="ConditionCheckStatus.Draft"/> while it is being recorded and becomes
/// <see cref="ConditionCheckStatus.Confirmed"/> — locked for good — when the employee confirms it (or, for a
/// pre-contract check, when the contract is sent). [Mahjar handover]
/// </summary>
public partial class ConditionCheck : BaseEntity
{
    /// <summary>The checkpoint discriminator (backing value for <see cref="CheckType"/>).</summary>
    public int CheckTypeId { get; set; }

    /// <summary>The status discriminator (backing value for <see cref="Status"/>).</summary>
    public int StatusId { get; set; }

    /// <summary>The product (the physical item) that was checked.</summary>
    public int ProductId { get; set; }

    /// <summary>The contract request the check belongs to (pre-contract), or the contract the order was placed against; 0 = none.</summary>
    public int ContractRequestId { get; set; }

    /// <summary>The order of a delivery/return check; 0 = none.</summary>
    public int OrderId { get; set; }

    /// <summary>The order item of a delivery/return check; 0 = none.</summary>
    public int OrderItemId { get; set; }

    /// <summary>The overall condition grade (backing value for <see cref="ConditionGrade"/>).</summary>
    public int ConditionGradeId { get; set; }

    /// <summary>Free-text condition notes; shown to the customer.</summary>
    public string ConditionNotes { get; set; }

    /// <summary>Whether damage was found.</summary>
    public bool HasDamage { get; set; }

    /// <summary>How serious the damage is (backing value for <see cref="DamageSeverity"/>); 0 when no damage.</summary>
    public int DamageSeverityId { get; set; }

    /// <summary>What the damage is; shown to the customer.</summary>
    public string DamageDescription { get; set; }

    /// <summary>A comment for the staff only (e.g. on how the receipt went); never shown to the customer.</summary>
    public string InternalComment { get; set; }

    /// <summary>The refundable security deposit of the returned item, frozen when the return is confirmed.</summary>
    public decimal DepositAmount { get; set; }

    /// <summary>The amount kept from the security deposit because of damage (return only).</summary>
    public decimal DamageDeduction { get; set; }

    /// <summary>Site-relative path of an uploaded video, e.g. <c>/images/uploaded/condition-checks/12/9f3a....mp4</c>.</summary>
    public string VideoFile { get; set; }

    /// <summary>An external video URL pasted instead of uploading a file.</summary>
    public string VideoUrl { get; set; }

    /// <summary>The employee who started the check.</summary>
    public int CreatedByCustomerId { get; set; }

    /// <summary>When the check was started (UTC).</summary>
    public DateTime CreatedOnUtc { get; set; }

    /// <summary>The employee who confirmed the check (or sent the contract); 0 while a draft.</summary>
    public int ConfirmedByCustomerId { get; set; }

    /// <summary>When the check was confirmed (UTC).</summary>
    public DateTime? ConfirmedOnUtc { get; set; }

    /// <summary>Last edit date (UTC).</summary>
    public DateTime UpdatedOnUtc { get; set; }

    /// <summary>Typed wrapper over <see cref="CheckTypeId"/>.</summary>
    public ConditionCheckType CheckType
    {
        get => (ConditionCheckType)CheckTypeId;
        set => CheckTypeId = (int)value;
    }

    /// <summary>Typed wrapper over <see cref="StatusId"/>.</summary>
    public ConditionCheckStatus Status
    {
        get => (ConditionCheckStatus)StatusId;
        set => StatusId = (int)value;
    }

    /// <summary>Typed wrapper over <see cref="ConditionGradeId"/>.</summary>
    public ItemConditionGrade ConditionGrade
    {
        get => (ItemConditionGrade)ConditionGradeId;
        set => ConditionGradeId = (int)value;
    }

    /// <summary>Typed wrapper over <see cref="DamageSeverityId"/>.</summary>
    public ItemStatusNoteSeverity DamageSeverity
    {
        get => (ItemStatusNoteSeverity)DamageSeverityId;
        set => DamageSeverityId = (int)value;
    }
}
