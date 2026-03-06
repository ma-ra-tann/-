using InvestorList.Port.Outbound;
using InvestorList.Domain.Models;

namespace InvestorList.Application.UseCases;

public class ExtractCapitalists
{
    private readonly IWebSearchPort _searchPort;

    public ExtractCapitalists(IWebSearchPort searchPort)
    {
        _searchPort = searchPort;
    }

    public async Task<VCFund> Execute(VCFund fund)
    {
        var capitalists = await _searchPort.SearchCapitalists(fund.Name);
        foreach (var capitalist in capitalists)
        {
            fund.AddCapitalist(capitalist);
        }
        return fund;
    }
}
