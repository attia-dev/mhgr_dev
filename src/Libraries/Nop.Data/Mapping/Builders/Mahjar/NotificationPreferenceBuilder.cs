// [Mahjar notifications]
using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Mahjar;
using Nop.Data.Extensions;

namespace Nop.Data.Mapping.Builders.Mahjar;

/// <summary>
/// Notification preference entity builder. [Mahjar notifications]
/// </summary>
public partial class NotificationPreferenceBuilder : NopEntityBuilder<NotificationPreference>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(NotificationPreference.CustomerId)).AsInt32().ForeignKey<Customer>();
        //NotificationTypeId / InAppEnabled / EmailEnabled are auto-mapped.
    }
}
