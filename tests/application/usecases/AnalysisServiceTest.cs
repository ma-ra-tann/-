using InvestorList.Application.Tests.Stubs;
using InvestorList.Application.UseCases;
using InvestorList.Domain.Models;

namespace InvestorList.Application.Tests.UseCases;

public class AnalysisServiceTest
{
    private static StubWebSearchPort CreateSearchStub()
    {
        var fund = new VCFund("ABC Capital", "https://abc-capital.com", "Seed", "SaaS");
        var capitalists = new List<Capitalist>
        {
            new("田中太郎", "Partner", "Fintech"),
            new("鈴木花子", "Associate", "General")
        };
        var evidences = new List<Evidence>
        {
            new(EvidenceType.Portfolio, "FP&A SaaSへのSeed投資実績", "https://example.com/portfolio")
        };
        return new StubWebSearchPort(fund: fund, capitalists: capitalists, evidences: evidences);
    }

    [Fact]
    public async Task VC分析で検索から判定まで一括実行できる()
    {
        var search = CreateSearchStub();
        var llm = new StubLLMAnalysisPort(InterestStatus.Interested);
        var db = new StubDataPersistencePort();
        await db.Save(new VCFund("ABC Capital", "", "Seed", "SaaS"));

        var service = new AnalysisService(search, llm, db);
        var result = await service.AnalyzeVC("ABC Capital");

        Assert.Equal("ABC Capital", result.Name);
        Assert.Equal(2, result.Capitalists.Count);
    }

    [Fact]
    public async Task 分析後にキャピタリストの関心度が判定される()
    {
        var search = CreateSearchStub();
        var llm = new StubLLMAnalysisPort(InterestStatus.Interested);
        var db = new StubDataPersistencePort();
        await db.Save(new VCFund("ABC Capital", "", "Seed", "SaaS"));

        var service = new AnalysisService(search, llm, db);
        var result = await service.AnalyzeVC("ABC Capital");

        Assert.All(result.Capitalists, c =>
            Assert.Equal(InterestStatus.Interested, c.FinancialModelInterest.Status));
    }

    [Fact]
    public async Task 分析後にキャピタリストに根拠が付与される()
    {
        var search = CreateSearchStub();
        var llm = new StubLLMAnalysisPort(InterestStatus.Interested);
        var db = new StubDataPersistencePort();
        await db.Save(new VCFund("ABC Capital", "", "Seed", "SaaS"));

        var service = new AnalysisService(search, llm, db);
        var result = await service.AnalyzeVC("ABC Capital");

        Assert.All(result.Capitalists, c =>
            Assert.NotEmpty(c.FinancialModelInterest.Evidences));
    }

    [Fact]
    public async Task 分析結果が永続化される()
    {
        var search = CreateSearchStub();
        var llm = new StubLLMAnalysisPort(InterestStatus.Interested);
        var db = new StubDataPersistencePort();
        await db.Save(new VCFund("ABC Capital", "", "Seed", "SaaS"));

        var service = new AnalysisService(search, llm, db);
        await service.AnalyzeVC("ABC Capital");

        var saved = await db.FindByName("ABC Capital");
        Assert.NotNull(saved);
        Assert.Equal(2, saved!.Capitalists.Count);
    }

    [Fact]
    public async Task キャピタリスト詳細を取得できる()
    {
        var search = CreateSearchStub();
        var llm = new StubLLMAnalysisPort(InterestStatus.Interested);
        var db = new StubDataPersistencePort();

        var fund = new VCFund("ABC Capital", "https://abc-capital.com", "Seed", "SaaS");
        fund.AddCapitalist(new Capitalist("田中太郎", "Partner", "Fintech"));
        await db.Save(fund);

        var service = new AnalysisService(search, llm, db);
        var result = await service.GetCapitalistDetail("ABC Capital", "田中太郎");

        Assert.NotNull(result);
        Assert.Equal("田中太郎", result!.Name);
    }

    [Fact]
    public async Task 存在しないキャピタリストはnullを返す()
    {
        var search = CreateSearchStub();
        var llm = new StubLLMAnalysisPort(InterestStatus.Unknown);
        var db = new StubDataPersistencePort();

        var service = new AnalysisService(search, llm, db);
        var result = await service.GetCapitalistDetail("ABC Capital", "存在しない");

        Assert.Null(result);
    }
}
