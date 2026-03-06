using InvestorList.Port.Outbound;
using InvestorList.Domain.Models;

namespace InvestorList.Application.UseCases;

public class JudgeInterest
{
    private readonly ILLMAnalysisPort _llmPort;

    public JudgeInterest(ILLMAnalysisPort llmPort)
    {
        _llmPort = llmPort;
    }

    public async Task<Capitalist> Execute(Capitalist capitalist)
    {
        var status = await _llmPort.Judge(capitalist);
        capitalist.FinancialModelInterest.Status = status;
        return capitalist;
    }
}
