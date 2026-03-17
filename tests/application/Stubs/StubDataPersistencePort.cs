using InvestorList.Port.Outbound;
using InvestorList.Domain.Models;

namespace InvestorList.Application.Tests.Stubs;

public class StubDataPersistencePort : IDataPersistencePort
{
    private readonly List<VCFund> _funds = [];

    public Task Save(VCFund fund)
    {
        _funds.RemoveAll(f => f.Name == fund.Name);
        _funds.Add(fund);
        return Task.CompletedTask;
    }

    public Task DeleteAll()
    {
        _funds.Clear();
        return Task.CompletedTask;
    }

    public Task DeleteAllExcept(IEnumerable<string> keepNames)
    {
        var keep = new HashSet<string>(keepNames);
        _funds.RemoveAll(f => !keep.Contains(f.Name));
        return Task.CompletedTask;
    }

    public Task<VCFund?> FindByName(string vcName)
    {
        return Task.FromResult(_funds.FirstOrDefault(f => f.Name == vcName));
    }

    public Task<List<VCFund>> GetAll()
    {
        return Task.FromResult(_funds.ToList());
    }
}
