using InvestorList.Domain.Models;

namespace InvestorList.Port.Outbound;

public interface IDataPersistencePort
{
    Task Save(VCFund fund);
    Task<VCFund?> FindByName(string vcName);
    Task<List<VCFund>> GetAll();
}
