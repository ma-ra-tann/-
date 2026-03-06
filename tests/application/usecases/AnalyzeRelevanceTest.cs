using InvestorList.Application.Tests.Stubs;
using InvestorList.Application.UseCases;
using InvestorList.Domain.Models;

namespace InvestorList.Application.Tests.UseCases;

public class AnalyzeRelevanceTest
{
    [Fact]
    public async Task キャピタリストの根拠を収集して追加できる()
    {
        var evidences = new List<Evidence>
        {
            new(EvidenceType.Article, "財務モデルに関する記事", "https://example.com/1"),
            new(EvidenceType.Portfolio, "FP&A SaaSに投資", "https://example.com/2")
        };

        var stub = new StubWebSearchPort(evidences: evidences);
        var useCase = new AnalyzeRelevance(stub);
        var capitalist = new Capitalist("田中太郎", "Partner", "Fintech");

        var result = await useCase.Execute(capitalist);

        Assert.Equal(2, result.FinancialModelInterest.Evidences.Count);
    }
}
