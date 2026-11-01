// [Mahjar notifications]
using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Mahjar;
using Nop.Data.Extensions;

namespace Nop.Data.Mapping.Builders.Mahjar;

/// <summary>
/// Notification entity builder. [Mahjar notifications]
/// </summary>
public partial class NotificationBuilder : NopEntityBuilder<Notification>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(Notification.RecipientCustomerId)).AsInt32().ForeignKey<Customer>()
            .WithColumn(nameof(Notification.Title)).AsString(450).NotNullable()
            .WithColumn(nameof(Notification.Body)).AsString(int.MaxValue).Nullable()
            .WithColumn(nameof(Notification.EntityName)).AsString(100).Nullable()
            .WithColumn(nameof(Notification.Url)).AsString(2048).Nullable();
        //RecipientVendorId / RecipientTypeId / NotificationTypeId / EntityId / StoreId / IsRead /
        //CreatedOnUtc / ReadOnUtc are auto-mapped by the column conventions.
    }
}
