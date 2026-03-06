using InvestorList.Adapter.Outbound.LLM;
using InvestorList.Domain.Models;

namespace InvestorList.Adapter.Tests.Outbound.LLM;

public class LLMAnalysisAdapterTest
{
    [Fact]
    public async Task 根拠ありのキャピタリストを関心ありと判定できる()
    {
        var stubClient = new StubLLMClient("Interested");
        var adapter = new LLMAnalysisAdapter(stubClient);

        var capitalist = new Capitalist("田中太郎", "Partner", "Fintech");
        capitalist.FinancialModelInterest.AddEvidence(
            new Evidence(EvidenceType.Article, "財務モデル記事", "https://example.com"));

        var result = await adapter.Judge(capitalist);

        Assert.Equal(InterestStatus.Interested, result);
    }

    [Fact]
    public async Task 不明と返されたら不明になる()
    {
        var stubClient = new StubLLMClient("Unknown");
        var adapter = new LLMAnalysisAdapter(stubClient);

        var capitalist = new Capitalist("鈴木花子", "Associate", "General");

        var result = await adapter.Judge(capitalist);

        Assert.Equal(InterestStatus.Unknown, result);
    }

    [Fact]
    public async Task パースできない応答は不明にフォールバック()
    {
        var stubClient = new StubLLMClient("invalid response");
        var adapter = new LLMAnalysisAdapter(stubClient);

        var capitalist = new Capitalist("山田次郎", "VP", "AI");

        var result = await adapter.Judge(capitalist);

        Assert.Equal(InterestStatus.Unknown, result);
    }
}

public class StubLLMClient : ILLMClient
{
    private readonly string _response;

    public StubLLMClient(string response)
    {
        _response = response;
    }

    public Task<string> Ask(string prompt) => Task.FromResult(_response);
}
