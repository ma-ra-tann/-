using InvestorList.Port.Outbound;
using InvestorList.Domain.Models;

namespace InvestorList.Adapter.Outbound.LLM;

public class LLMAnalysisAdapter : ILLMAnalysisPort
{
    private readonly ILLMClient _client;

    public LLMAnalysisAdapter(ILLMClient client)
    {
        _client = client;
    }

    public async Task<InterestStatus> Judge(Capitalist capitalist)
    {
        var evidenceSummary = string.Join("\n",
            capitalist.FinancialModelInterest.Evidences.Select(e => $"- [{e.Type}] {e.Summary}"));

        var prompt = $"""
            以下のキャピタリストの情報をもとに、財務モデルへの関心度を判定してください。
            回答は Interested, Unknown, NotInterested のいずれか1語のみで返してください。

            名前: {capitalist.Name}
            役職: {capitalist.Title}
            投資担当領域: {capitalist.InvestmentDomain}
            根拠:
            {evidenceSummary}
            """;

        var response = await _client.Ask(prompt);
        var trimmed = response.Trim();

        if (Enum.TryParse<InterestStatus>(trimmed, ignoreCase: true, out var status))
            return status;

        return InterestStatus.Unknown;
    }
}
