using InvestorList.Port.Outbound;
using InvestorList.Domain.Models;

namespace InvestorList.Application.Tests.Stubs;

public class StubLLMAnalysisPort : ILLMAnalysisPort
{
    private readonly InterestStatus _status;

    public StubLLMAnalysisPort(InterestStatus status = InterestStatus.Unknown)
    {
        _status = status;
    }

    public Task<InterestStatus> Judge(Capitalist capitalist, string vcName = "")
    {
        return Task.FromResult(_status);
    }
}
