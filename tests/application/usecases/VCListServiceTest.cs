using InvestorList.Application.Tests.Stubs;
using InvestorList.Application.UseCases;
using InvestorList.Domain.Models;
using System.Text;

namespace InvestorList.Application.Tests.UseCases;

public class VCListServiceTest
{
    [Fact]
    public async Task VCを手動追加して保存できる()
    {
        var db = new StubDataPersistencePort();
        var search = new StubWebSearchPort();
        var llm = new StubLLMAnalysisPort(InterestStatus.Interested);
        var analysisPort = new AnalysisService(search, llm, db);
        var service = new VCListService(db, analysisPort);

        var result = await service.AddVC("ABC Capital", "Seed", "SaaS");

        Assert.Equal("ABC Capital", result.Name);
        Assert.Equal("Seed", result.InvestmentStage);
        Assert.Equal("SaaS", result.InvestmentTheme);
    }

    [Fact]
    public async Task 追加したVCが永続化される()
    {
        var db = new StubDataPersistencePort();
        var search = new StubWebSearchPort();
        var llm = new StubLLMAnalysisPort(InterestStatus.Interested);
        var analysisPort = new AnalysisService(search, llm, db);
        var service = new VCListService(db, analysisPort);

        await service.AddVC("ABC Capital", "Seed", "SaaS");
        var found = await service.GetVCDetail("ABC Capital");

        Assert.NotNull(found);
        Assert.Equal("ABC Capital", found!.Name);
    }

    [Fact]
    public async Task 全VC一覧を取得できる()
    {
        var db = new StubDataPersistencePort();
        var search = new StubWebSearchPort();
        var llm = new StubLLMAnalysisPort(InterestStatus.Interested);
        var analysisPort = new AnalysisService(search, llm, db);
        var service = new VCListService(db, analysisPort);

        await service.AddVC("ABC Capital", "Seed", "SaaS");
        await service.AddVC("XYZ Ventures", "Pre-Seed", "Fintech");

        var all = await service.GetAllVCs();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task 存在しないVC名はnullを返す()
    {
        var db = new StubDataPersistencePort();
        var search = new StubWebSearchPort();
        var llm = new StubLLMAnalysisPort(InterestStatus.Interested);
        var analysisPort = new AnalysisService(search, llm, db);
        var service = new VCListService(db, analysisPort);

        var result = await service.GetVCDetail("存在しない");

        Assert.Null(result);
    }

    [Fact]
    public async Task CSVからVCリストを一括取り込みできる()
    {
        var db = new StubDataPersistencePort();
        var search = new StubWebSearchPort();
        var llm = new StubLLMAnalysisPort(InterestStatus.Interested);
        var analysisPort = new AnalysisService(search, llm, db);
        var service = new VCListService(db, analysisPort);

        var csv = "VC名,投資ステージ,投資テーマ\nABC Capital,Seed,SaaS\nXYZ Ventures,Pre-Seed,Fintech";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await service.ImportFromCsv(stream);

        Assert.Equal(2, result.Count);
        Assert.Equal("ABC Capital", result[0].Name);
        Assert.Equal("XYZ Ventures", result[1].Name);
    }

    [Fact]
    public async Task CSV取り込み後に永続化される()
    {
        var db = new StubDataPersistencePort();
        var search = new StubWebSearchPort();
        var llm = new StubLLMAnalysisPort(InterestStatus.Interested);
        var analysisPort = new AnalysisService(search, llm, db);
        var service = new VCListService(db, analysisPort);

        var csv = "VC名,投資ステージ,投資テーマ\nABC Capital,Seed,SaaS";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        await service.ImportFromCsv(stream);
        var all = await service.GetAllVCs();

        Assert.Single(all);
    }

    [Fact]
    public async Task Excel形式CSVからVC名とステージを取り込める()
    {
        var db = new StubDataPersistencePort();
        var search = new StubWebSearchPort();
        var llm = new StubLLMAnalysisPort(InterestStatus.Interested);
        var analysisPort = new AnalysisService(search, llm, db);
        var service = new VCListService(db, analysisPort);

        var csv = string.Join("\n",
            "72,,,,,,,,,,,,,",
            ",,,,,,,,,,,,,",
            ",,,,ステージ,,,,,,,,,",
            ",No.,Company/Fund name（URL付き）,Seed,Early,Middle,Later,GV Intro,対象地域,紹介者,優先度,ステータス,進捗メモ,その他メモ",
            "o,,East Ventures,◎,,,,◎,日本・海外,,,,,",
            "o,,THE SEED,◎,,,,◎,日本,,,,,",
            ",,,,,,,,,,,,,"
        );
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await service.ImportFromCsv(stream);

        Assert.Equal(2, result.Count);
        Assert.Equal("East Ventures", result[0].Name);
        Assert.Equal("Seed", result[0].InvestmentStage);
        Assert.Equal("日本・海外", result[0].InvestmentTheme);
        Assert.Equal("THE SEED", result[1].Name);
    }

    [Fact]
    public async Task CSV内のカンマ含みVC名を正しくパースできる()
    {
        var db = new StubDataPersistencePort();
        var search = new StubWebSearchPort();
        var llm = new StubLLMAnalysisPort(InterestStatus.Interested);
        var analysisPort = new AnalysisService(search, llm, db);
        var service = new VCListService(db, analysisPort);

        var csv = string.Join("\n",
            ",No.,Company/Fund name,Seed,Early,Middle,Later,GV Intro,対象地域",
            "o,,\"Eclectic Management, LLC\",,,◎,◎,◎,"
        );
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await service.ImportFromCsv(stream);

        Assert.Single(result);
        Assert.Equal("Eclectic Management, LLC", result[0].Name);
        Assert.Equal("Middle", result[0].InvestmentStage);
    }
}
