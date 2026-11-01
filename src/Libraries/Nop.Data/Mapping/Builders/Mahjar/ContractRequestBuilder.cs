// [Mahjar contracts]
using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Mahjar;
using Nop.Data.Extensions;

namespace Nop.Data.Mapping.Builders.Mahjar;

/// <summary>
/// ContractRequest entity builder. [Mahjar contracts]
/// </summary>
public partial class ContractRequestBuilder : NopEntityBuilder<ContractRequest>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(ContractRequest.CustomerId)).AsInt32().ForeignKey<Customer>()
            .WithColumn(nameof(ContractRequest.CartSnapshotJson)).AsString(int.MaxValue).NotNullable()
            .WithColumn(nameof(ContractRequest.CartHash)).AsString(128).NotNullable()
            .WithColumn(nameof(ContractRequest.ContractBody)).AsString(int.MaxValue).Nullable()
            .WithColumn(nameof(ContractRequest.SignatureDataUrl)).AsString(int.MaxValue).Nullable()
            .WithColumn(nameof(ContractRequest.SignedName)).AsString(400).Nullable()
            .WithColumn(nameof(ContractRequest.SignedIp)).AsString(100).Nullable()
            .WithColumn(nameof(ContractRequest.AdminComment)).AsString(int.MaxValue).Nullable()
            .WithColumn(nameof(ContractRequest.RejectionReason)).AsString(int.MaxValue).Nullable()
            //[Mahjar contracts] per-transaction overrides of the contract settings' default wording; null = use the setting
            .WithColumn(nameof(ContractRequest.DeliveryDetails)).AsString(1000).Nullable()
            .WithColumn(nameof(ContractRequest.ReturnDetails)).AsString(1000).Nullable()
            .WithColumn(nameof(ContractRequest.VerificationReference)).AsString(400).Nullable()
            .WithColumn(nameof(ContractRequest.GuaranteeType)).AsString(1000).Nullable()
            .WithColumn(nameof(ContractRequest.ExtraFees)).AsString(1000).Nullable()
            .WithColumn(nameof(ContractRequest.LateReturnFee)).AsString(1000).Nullable();
        //StoreId / StatusId / Token / LanguageId / OrderId / SignedOnUtc / CreatedOnUtc / SentOnUtc /
        //ReviewedOnUtc / UpdatedOnUtc / GuaranteeAmount / TaxAmount are auto-mapped by the column conventions.
    }
}
