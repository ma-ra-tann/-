using System.Text.Json;
using InvestorList.Port.Outbound;
using InvestorList.Domain.Models;

namespace InvestorList.Adapter.Outbound.Search;

public class AgentServiceClient : IWebSearchPort
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public AgentServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<VCFund> SearchVCProfile(string vcName, string? knownUrl = null)
    {
        var query = $"/search/vc?vc_name={Uri.EscapeDataString(vcName)}";
        if (!string.IsNullOrEmpty(knownUrl))
            query += $"&url={Uri.EscapeDataString(knownUrl)}";
            
        var response = await _httpClient.PostAsync(query, null);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var dto = JsonSerializer.Deserialize<VCProfileDto>(json, JsonOptions)!;
        return new VCFund(dto.Name, dto.WebsiteUrl, dto.InvestmentStage, dto.InvestmentTheme);
    }

    public async Task<List<Capitalist>> SearchCapitalists(string vcName)
    {
        var response = await _httpClient.PostAsync(
            $"/search/capitalists?vc_name={Uri.EscapeDataString(vcName)}", null);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var dtos = JsonSerializer.Deserialize<List<CapitalistDto>>(json, JsonOptions) ?? [];
        return dtos.Select(d => new Capitalist(d.Name, d.Title, d.InvestmentDomain)).ToList();
    }

    public async Task<List<Evidence>> SearchEvidences(string capitalistName, string vcName = "")
    {
        var query = $"/search/evidences?capitalist_name={Uri.EscapeDataString(capitalistName)}";
        if (!string.IsNullOrEmpty(vcName))
            query += $"&vc_name={Uri.EscapeDataString(vcName)}";
        var response = await _httpClient.PostAsync(query, null);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var dtos = JsonSerializer.Deserialize<List<EvidenceDto>>(json, JsonOptions) ?? [];
        return dtos.Select(d => new Evidence(
            Enum.Parse<EvidenceType>(d.Type), d.Summary, d.SourceUrl)).ToList();
    }

    private record VCProfileDto(string Name, string WebsiteUrl, string InvestmentStage, string InvestmentTheme);
    private record CapitalistDto(string Name, string Title, string InvestmentDomain);
    private record EvidenceDto(string Type, string Summary, string SourceUrl);
}
