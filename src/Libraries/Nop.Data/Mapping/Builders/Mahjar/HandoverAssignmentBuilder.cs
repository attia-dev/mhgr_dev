// [Mahjar handover]
using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Mahjar;
using Nop.Core.Domain.Orders;
using Nop.Data.Extensions;

namespace Nop.Data.Mapping.Builders.Mahjar;

/// <summary>
/// HandoverAssignment entity builder. [Mahjar handover]
/// </summary>
public partial class HandoverAssignmentBuilder : NopEntityBuilder<HandoverAssignment>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(HandoverAssignment.OrderId)).AsInt32().ForeignKey<Order>()
            .WithColumn(nameof(HandoverAssignment.AssignedToCustomerId)).AsInt32().ForeignKey<Customer>();
        //AssignedByCustomerId / CreatedOnUtc / UpdatedOnUtc are auto-mapped by the column conventions (the admin who
        //assigned is deliberately NOT a foreign key: deleting that admin must never cascade into the assignment).
    }
}
