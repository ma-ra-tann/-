using InvestorList.Adapter.Outbound.Db;
using InvestorList.Domain.Models;

namespace InvestorList.Adapter.Tests.Outbound.Db;

public class JsonDataStoreTest : IDisposable
{
    private readonly string _testFilePath;
    private readonly JsonDataStore _store;

    public JsonDataStoreTest()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.json");
        _store = new JsonDataStore(_testFilePath);
    }

    [Fact]
    public async Task VCファンドを保存して取得できる()
    {
        var fund = new VCFund("ABC Capital", "https://abc-capital.com", "Seed", "SaaS");

        await _store.Save(fund);
        var result = await _store.FindByName("ABC Capital");

        Assert.NotNull(result);
        Assert.Equal("ABC Capital", result.Name);
    }

    [Fact]
    public async Task 存在しないVC名はnullを返す()
    {
        var result = await _store.FindByName("存在しないVC");

        Assert.Null(result);
    }

    [Fact]
    public async Task 全件取得できる()
    {
        await _store.Save(new VCFund("VC A", "https://a.com", "Seed", "SaaS"));
        await _store.Save(new VCFund("VC B", "https://b.com", "Pre-Seed", "Fintech"));

        var results = await _store.GetAll();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task キャピタリスト付きで保存復元できる()
    {
        var fund = new VCFund("ABC Capital", "https://abc-capital.com", "Seed", "SaaS");
        var capitalist = new Capitalist("田中太郎", "Partner", "Fintech");
        capitalist.FinancialModelInterest.Status = InterestStatus.Interested;
        capitalist.FinancialModelInterest.AddEvidence(
            new Evidence(EvidenceType.Article, "財務モデル記事", "https://example.com"));
        fund.AddCapitalist(capitalist);

        await _store.Save(fund);
        var result = await _store.FindByName("ABC Capital");

        Assert.Single(result!.Capitalists);
        Assert.Equal("田中太郎", result.Capitalists[0].Name);
        Assert.Equal(InterestStatus.Interested, result.Capitalists[0].FinancialModelInterest.Status);
        Assert.Single(result.Capitalists[0].FinancialModelInterest.Evidences);
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
            File.Delete(_testFilePath);
    }
}
