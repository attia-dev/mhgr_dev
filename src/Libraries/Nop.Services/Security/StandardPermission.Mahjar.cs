// [Mahjar contracts] permissions for the contract-approval stage
namespace Nop.Services.Security;

public static partial class StandardPermission
{
    public static partial class Mahjar
    {
        public const string CONTRACTS_VIEW = "Mahjar.ContractsView";
        public const string CONTRACTS_MANAGE = "Mahjar.ContractsManage";

        //[Mahjar handover] the handover desk: condition checks at intake / before signing / delivery / return
        public const string HANDOVERS_MANAGE = "Mahjar.HandoversManage";
    }
}
