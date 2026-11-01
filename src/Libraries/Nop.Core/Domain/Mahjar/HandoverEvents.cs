// [Mahjar handover]
namespace Nop.Core.Domain.Mahjar;

/// <summary>
/// Published when an employee confirms (locks) a condition check at the intake, delivery or return checkpoint.
/// Pre-contract checks are locked by the contract being sent and do not raise it. [Mahjar handover]
/// </summary>
public partial class ConditionCheckConfirmedEvent
{
    /// <summary>Ctor</summary>
    /// <param name="check">The confirmed condition check</param>
    public ConditionCheckConfirmedEvent(ConditionCheck check)
    {
        Check = check;
    }

    /// <summary>The confirmed condition check.</summary>
    public ConditionCheck Check { get; }
}
