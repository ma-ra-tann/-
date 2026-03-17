using InvestorList.Adapter.Outbound.Db;
using InvestorList.Domain.Models;

namespace InvestorList.Adapter.Tests.Outbound.Db;

public class SqliteDataStoreTest
{
    private async Task<SqliteDataStore> CreateStore()
    {
        var store = new SqliteDataStore("Data Source=:memory:");
        await store.InitializeAsync();
        return store;
    }

    [Fact]
    public async Task キャピタリスト付きVCを保存して復元できる()
    {
        var store = await CreateStore();

        var fund = new VCFund("ABC Capital", "https://abc.com", "Seed", "SaaS");
        var capitalist = new Capitalist("田中太郎", "Partner", "Fintech");
        capitalist.FinancialModelInterest.Status = InterestStatus.Interested;
        capitalist.FinancialModelInterest.AddEvidence(
            new Evidence(EvidenceType.Portfolio, "FP&A SaaS投資", "https://example.com"));
        fund.AddCapitalist(capitalist);

        await store.Save(fund);
        var result = await store.FindByName("ABC Capital");

        Assert.NotNull(result);
        Assert.Single(result!.Capitalists);
        Assert.Equal("田中太郎", result.Capitalists[0].Name);
        Assert.Equal(InterestStatus.Interested, result.Capitalists[0].FinancialModelInterest.Status);
        Assert.Single(result.Capitalists[0].FinancialModelInterest.Evidences);
    }

    [Fact]
    public async Task 同名VCの保存は上書きされる()
    {
        var store = await CreateStore();

        await store.Save(new VCFund("ABC Capital", "https://old.com", "Seed", "SaaS"));
        await store.Save(new VCFund("ABC Capital", "https://new.com", "Seed", "SaaS"));

        var result = await store.FindByName("ABC Capital");
        Assert.Equal("https://new.com", result!.WebsiteUrl);

        var all = await store.GetAll();
        Assert.Single(all);
    }
}
