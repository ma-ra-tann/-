using InvestorList.Port.Outbound;
using InvestorList.Domain.Models;

namespace InvestorList.Application.UseCases;

public class FetchVCProfile
{
    private readonly IWebSearchPort _searchPort;

    public FetchVCProfile(IWebSearchPort searchPort)
    {
        _searchPort = searchPort;
    }

    public async Task<VCFund> Execute(string vcName)
    {
        return await _searchPort.SearchVCProfile(vcName);
    }
}
