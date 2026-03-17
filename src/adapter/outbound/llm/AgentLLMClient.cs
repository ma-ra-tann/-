using System.Text;
using System.Text.Json;
using InvestorList.Port.Outbound;
using InvestorList.Domain.Models;

namespace InvestorList.Adapter.Outbound.LLM;

public class AgentLLMClient : ILLMAnalysisPort
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public AgentLLMClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<InterestStatus> Judge(Capitalist capitalist, string vcName = "")
    {
        var request = new JudgeRequestDto
        {
            CapitalistName = capitalist.Name,
            Title = capitalist.Title,
            InvestmentDomain = capitalist.InvestmentDomain,
            VcName = vcName,
            Evidences = capitalist.FinancialModelInterest.Evidences
                .Select(e => new EvidenceDto
                {
                    Type = e.Type.ToString(),
                    Summary = e.Summary,
                    SourceUrl = e.SourceUrl
                }).ToList()
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/llm/judge", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JudgeResponseDto>(responseJson, JsonOptions)!;

        if (Enum.TryParse<InterestStatus>(result.Status, ignoreCase: true, out var status))
            return status;

        return InterestStatus.Unknown;
    }

    private class JudgeRequestDto
    {
        public string CapitalistName { get; set; } = "";
        public string Title { get; set; } = "";
        public string InvestmentDomain { get; set; } = "";
        public string VcName { get; set; } = "";
        public List<EvidenceDto> Evidences { get; set; } = [];
    }

    private class EvidenceDto
    {
        public string Type { get; set; } = "";
        public string Summary { get; set; } = "";
        public string SourceUrl { get; set; } = "";
    }

    private record JudgeResponseDto(string Status, string CapitalistName);
}
