using InvestorList.Port.Outbound;
using InvestorList.Domain.Models;

namespace InvestorList.Application.UseCases;

public class AnalyzeRelevance
{
    private readonly IWebSearchPort _searchPort;

    public AnalyzeRelevance(IWebSearchPort searchPort)
    {
        _searchPort = searchPort;
    }

    public async Task<Capitalist> Execute(Capitalist capitalist)
    {
        var evidences = await _searchPort.SearchEvidences(capitalist.Name);
        foreach (var evidence in evidences)
        {
            capitalist.FinancialModelInterest.AddEvidence(evidence);
        }
        return capitalist;
    }
}
