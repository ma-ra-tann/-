using InvestorList.Adapter.Outbound.Search;
using InvestorList.Domain.Models;

namespace InvestorList.Adapter.Tests.Outbound.Search;

public class WebSearchAdapterTest
{
    [Fact]
    public async Task VC名からVCファンドを検索できる()
    {
        var stubHttp = new StubHttpSearchClient(
            vcJson: """{"name":"ABC Capital","websiteUrl":"https://abc.com","investmentStage":"Seed","investmentTheme":"SaaS"}"""
        );

        var adapter = new WebSearchAdapter(stubHttp);
        var result = await adapter.SearchVCProfile("ABC Capital");

        Assert.Equal("ABC Capital", result.Name);
        Assert.Equal("Seed", result.InvestmentStage);
    }

    [Fact]
    public async Task VC名からキャピタリスト一覧を検索できる()
    {
        var stubHttp = new StubHttpSearchClient(
            capitalistsJson: """[{"name":"田中太郎","title":"Partner","investmentDomain":"Fintech"}]"""
        );

        var adapter = new WebSearchAdapter(stubHttp);
        var result = await adapter.SearchCapitalists("ABC Capital");

        Assert.Single(result);
        Assert.Equal("田中太郎", result[0].Name);
    }

    [Fact]
    public async Task キャピタリスト名から根拠を検索できる()
    {
        var stubHttp = new StubHttpSearchClient(
            evidencesJson: """[{"type":"Article","summary":"財務モデル記事","sourceUrl":"https://example.com"}]"""
        );

        var adapter = new WebSearchAdapter(stubHttp);
        var result = await adapter.SearchEvidences("田中太郎");

        Assert.Single(result);
        Assert.Equal(EvidenceType.Article, result[0].Type);
    }
}

public class StubHttpSearchClient : IHttpSearchClient
{
    private readonly string _vcJson;
    private readonly string _capitalistsJson;
    private readonly string _evidencesJson;

    public StubHttpSearchClient(
        string? vcJson = null,
        string? capitalistsJson = null,
        string? evidencesJson = null)
    {
        _vcJson = vcJson ?? "{}";
        _capitalistsJson = capitalistsJson ?? "[]";
        _evidencesJson = evidencesJson ?? "[]";
    }

    public Task<string> SearchVC(string vcName) => Task.FromResult(_vcJson);
    public Task<string> SearchCapitalists(string vcName) => Task.FromResult(_capitalistsJson);
    public Task<string> SearchEvidences(string capitalistName) => Task.FromResult(_evidencesJson);
}
