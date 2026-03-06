using System.Text.Json;
using InvestorList.Port.Outbound;
using InvestorList.Domain.Models;

namespace InvestorList.Adapter.Outbound.Search;

public class WebSearchAdapter : IWebSearchPort
{
    private readonly IHttpSearchClient _httpClient;

    public WebSearchAdapter(IHttpSearchClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<VCFund> SearchVCProfile(string vcName)
    {
        var json = await _httpClient.SearchVC(vcName);
        var dto = JsonSerializer.Deserialize<VCDto>(json, JsonOptions)!;
        return new VCFund(dto.Name, dto.WebsiteUrl, dto.InvestmentStage, dto.InvestmentTheme);
    }

    public async Task<List<Capitalist>> SearchCapitalists(string vcName)
    {
        var json = await _httpClient.SearchCapitalists(vcName);
        var dtos = JsonSerializer.Deserialize<List<CapitalistDto>>(json, JsonOptions) ?? [];
        return dtos.Select(d => new Capitalist(d.Name, d.Title, d.InvestmentDomain)).ToList();
    }

    public async Task<List<Evidence>> SearchEvidences(string capitalistName)
    {
        var json = await _httpClient.SearchEvidences(capitalistName);
        var dtos = JsonSerializer.Deserialize<List<EvidenceDto>>(json, JsonOptions) ?? [];
        return dtos.Select(d => new Evidence(
            Enum.Parse<EvidenceType>(d.Type), d.Summary, d.SourceUrl)).ToList();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private record VCDto(string Name, string WebsiteUrl, string InvestmentStage, string InvestmentTheme);
    private record CapitalistDto(string Name, string Title, string InvestmentDomain);
    private record EvidenceDto(string Type, string Summary, string SourceUrl);
}
