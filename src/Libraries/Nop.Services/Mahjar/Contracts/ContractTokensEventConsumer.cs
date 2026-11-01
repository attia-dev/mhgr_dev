// [Mahjar contracts]
using Nop.Core.Domain.Messages;
using Nop.Services.Events;

namespace Nop.Services.Mahjar.Contracts;

/// <summary>
/// Registers the %Contract.*% tokens as allowed message tokens so they show up in the admin message-template
/// token list and validate on save. Auto-discovered by ITypeFinder. [Mahjar contracts]
/// </summary>
public partial class ContractTokensEventConsumer : IConsumer<AdditionalTokensAddedEvent>
{
    /// <summary>
    /// Handle the "additional tokens added" event
    /// </summary>
    /// <param name="eventMessage">Event message</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public virtual Task HandleEventAsync(AdditionalTokensAddedEvent eventMessage)
    {
        eventMessage.AddTokens(
            "%Contract.Number%",
            "%Contract.Date%",
            "%Contract.ItemsTable%",
            "%Contract.SubTotal%",
            "%Contract.DepositTotal%",
            "%Contract.GrandTotal%",
            "%Contract.SignURL%",
            "%Contract.RejectReason%");

        return Task.CompletedTask;
    }
}
