// [Mahjar handover]
namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// The checkpoint a <see cref="ConditionCheck"/> documents in the life of one physical item. Values are explicit
/// and gapped so new checkpoints can be slotted in without renumbering stored rows. [Mahjar handover]
/// </summary>
public enum ConditionCheckType
{
    /// <summary>The platform receives the item from the selling partner.</summary>
    Intake = 10,

    /// <summary>The item's condition as shown to the customer next to the contract, before signing.</summary>
    PreContract = 20,

    /// <summary>The item is handed over to the customer.</summary>
    Delivery = 30,

    /// <summary>A rented item comes back from the customer.</summary>
    Return = 40
}

/// <summary>
/// Whether a condition check can still be edited. A confirmed check is evidence: it is never edited again.
/// [Mahjar handover]
/// </summary>
public enum ConditionCheckStatus
{
    /// <summary>Media and notes are still being recorded.</summary>
    Draft = 10,

    /// <summary>Confirmed and locked.</summary>
    Confirmed = 20
}

/// <summary>
/// The work queues of the handover desk (Admin, Sales, Handovers). [Mahjar handover]
/// </summary>
public enum HandoverStage
{
    /// <summary>Pending contracts whose items still need their before-signing condition media.</summary>
    AwaitingPreContract = 10,

    /// <summary>Paid order items that have not been handed over to the customer yet.</summary>
    AwaitingDelivery = 20,

    /// <summary>Delivered rental items that have not come back yet.</summary>
    AwaitingReturn = 30,

    /// <summary>Partner intakes that were started but not confirmed.</summary>
    IntakeInProgress = 40,

    /// <summary>Every confirmed check, newest first.</summary>
    Completed = 50
}
