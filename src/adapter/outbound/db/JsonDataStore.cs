using System.Text.Json;
using InvestorList.Port.Outbound;
using InvestorList.Domain.Models;

namespace InvestorList.Adapter.Outbound.Db;

public class JsonDataStore : IDataPersistencePort
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public JsonDataStore(string filePath)
    {
        _filePath = filePath;
    }

    public async Task Save(VCFund fund)
    {
        var all = await LoadAll();
        all.RemoveAll(f => f.Name == fund.Name);
        all.Add(ToDto(fund));
        var json = JsonSerializer.Serialize(all, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }

    public async Task<VCFund?> FindByName(string vcName)
    {
        var all = await LoadAll();
        var dto = all.FirstOrDefault(f => f.Name == vcName);
        return dto is null ? null : FromDto(dto);
    }

    public async Task<List<VCFund>> GetAll()
    {
        var all = await LoadAll();
        return all.Select(FromDto).ToList();
    }

    private async Task<List<VCFundDto>> LoadAll()
    {
        if (!File.Exists(_filePath))
            return [];
        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<VCFundDto>>(json) ?? [];
    }

    private static VCFundDto ToDto(VCFund fund) => new()
    {
        Name = fund.Name,
        WebsiteUrl = fund.WebsiteUrl,
        InvestmentStage = fund.InvestmentStage,
        InvestmentTheme = fund.InvestmentTheme,
        Capitalists = fund.Capitalists.Select(c => new CapitalistDto
        {
            Name = c.Name,
            Title = c.Title,
            InvestmentDomain = c.InvestmentDomain,
            InterestStatus = c.FinancialModelInterest.Status.ToString(),
            Evidences = c.FinancialModelInterest.Evidences.Select(e => new EvidenceDto
            {
                Type = e.Type.ToString(),
                Summary = e.Summary,
                SourceUrl = e.SourceUrl
            }).ToList()
        }).ToList()
    };

    private static VCFund FromDto(VCFundDto dto)
    {
        var fund = new VCFund(dto.Name, dto.WebsiteUrl, dto.InvestmentStage, dto.InvestmentTheme);
        foreach (var cDto in dto.Capitalists)
        {
            var capitalist = new Capitalist(cDto.Name, cDto.Title, cDto.InvestmentDomain);
            capitalist.FinancialModelInterest.Status = Enum.Parse<InterestStatus>(cDto.InterestStatus);
            foreach (var eDto in cDto.Evidences)
            {
                capitalist.FinancialModelInterest.AddEvidence(
                    new Evidence(Enum.Parse<EvidenceType>(eDto.Type), eDto.Summary, eDto.SourceUrl));
            }
            fund.AddCapitalist(capitalist);
        }
        return fund;
    }
}

internal class VCFundDto
{
    public string Name { get; set; } = "";
    public string WebsiteUrl { get; set; } = "";
    public string InvestmentStage { get; set; } = "";
    public string InvestmentTheme { get; set; } = "";
    public List<CapitalistDto> Capitalists { get; set; } = [];
}

internal class CapitalistDto
{
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    public string InvestmentDomain { get; set; } = "";
    public string InterestStatus { get; set; } = "Unknown";
    public List<EvidenceDto> Evidences { get; set; } = [];
}

internal class EvidenceDto
{
    public string Type { get; set; } = "";
    public string Summary { get; set; } = "";
    public string SourceUrl { get; set; } = "";
}
