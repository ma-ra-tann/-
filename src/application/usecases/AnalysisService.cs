using InvestorList.Port.Inbound;
using InvestorList.Port.Outbound;
using InvestorList.Domain.Models;

namespace InvestorList.Application.UseCases;

public class AnalysisService : IAnalysisPort
{
    private readonly IWebSearchPort _search;
    private readonly ILLMAnalysisPort _llm;
    private readonly IDataPersistencePort _db;

    public AnalysisService(IWebSearchPort search, ILLMAnalysisPort llm, IDataPersistencePort db)
    {
        _search = search;
        _llm = llm;
        _db = db;
    }

    public async Task<VCFund> AnalyzeVC(string vcName)
    {
        var existingFund = await _db.FindByName(vcName);
        var knownUrl = existingFund?.WebsiteUrl;

        var fund = await _search.SearchVCProfile(vcName, knownUrl);

        // APIがURLを見つけられなかったが、DBに既存のURLがある場合はそれを維持する
        if ((string.IsNullOrEmpty(fund.WebsiteUrl) || fund.WebsiteUrl.Contains("調査不足")) && 
            !string.IsNullOrEmpty(knownUrl) && !knownUrl.Contains("調査不足"))
        {
            fund = new VCFund(fund.Name, knownUrl, fund.InvestmentStage, fund.InvestmentTheme);
        }

        var capitalists = await _search.SearchCapitalists(vcName);
        
        // キャピタリストごとの分析を並列実行
        var analysisTasks = capitalists.Select(async capitalist => 
        {
            var evidences = await _search.SearchEvidences(capitalist.Name, vcName);
            foreach (var evidence in evidences)
            {
                capitalist.FinancialModelInterest.AddEvidence(evidence);
            }

            var status = await _llm.Judge(capitalist);
            capitalist.FinancialModelInterest.Status = status;
            
            return capitalist;
        });

        var analyzedCapitalists = await Task.WhenAll(analysisTasks);

        foreach (var capitalist in analyzedCapitalists)
        {
            fund.AddCapitalist(capitalist);
        }

        await _db.Save(fund);
        return fund;
    }

    public async Task<Capitalist?> GetCapitalistDetail(string vcName, string capitalistName)
    {
        var fund = await _db.FindByName(vcName);
        return fund?.Capitalists.FirstOrDefault(c => c.Name == capitalistName);
    }
}
