using File.Paging.PhysicalLevel.Classes.Pages;

namespace FIle.Paging.LogicalLevel.Classes.Configurations
{
    public abstract class TransactionParticipancyConfiguration
    {
        public abstract IPage CreateTransactionLayerPage(IPage physicalPage);
    }
}