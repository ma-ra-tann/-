using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using InvestorList.Port.Inbound;

namespace InvestorList.Adapter.Inbound.Web;

[ApiController]
[Route("api/vc")]
public class VCController : ControllerBase
{
    private readonly IVCListPort _vcListPort;
    private readonly IAnalysisPort _analysisPort;

    public VCController(IVCListPort vcListPort, IAnalysisPort analysisPort)
    {
        _vcListPort = vcListPort;
        _analysisPort = analysisPort;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var funds = await _vcListPort.GetAllVCs();
        var result = funds.Select(f => new VCListItem(
            f.Name,
            f.WebsiteUrl,
            f.InvestmentStage,
            f.InvestmentTheme,
            f.Capitalists.Count == 0 ? "未分析" : FormatAnalysisStatus(f)
        ));
        return Ok(result);
    }

    [HttpGet("{vcName}")]
    public async Task<IActionResult> GetDetail(string vcName)
    {
        var fund = await _vcListPort.GetVCDetail(vcName);
        if (fund is null) return NotFound();

        var capitalists = fund.Capitalists
            .OrderBy(c => c.FinancialModelInterest.Status switch
            {
                Domain.Models.InterestStatus.Interested => 0,
                Domain.Models.InterestStatus.Unknown => 1,
                Domain.Models.InterestStatus.NotInterested => 2,
                _ => 1
            })
            .Select(c => new CapitalistSummary(
                c.Name,
                c.Title,
                c.InvestmentDomain,
                c.FinancialModelInterest.Status.ToString(),
                c.FinancialModelInterest.Evidences
                    .Select(e => e.Summary)
                    .FirstOrDefault() ?? ""
            ));

        return Ok(new VCDetail(
            fund.Name,
            fund.WebsiteUrl,
            fund.InvestmentStage,
            fund.InvestmentTheme,
            capitalists.ToList()
        ));
    }

    [HttpPost("{vcName}/analyze")]
    public async Task<IActionResult> Analyze(string vcName)
    {
        var result = await _analysisPort.AnalyzeVC(vcName);
        return Ok(new { result.Name, CapitalistCount = result.Capitalists.Count });
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] AddVCRequest request)
    {
        var fund = await _vcListPort.AddVC(request.Name, request.InvestmentStage, request.InvestmentTheme);
        return Ok(new { fund.Name, fund.InvestmentStage });
    }

    [HttpPost("import-csv")]
    public async Task<IActionResult> ImportCsv(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var funds = await _vcListPort.ImportFromCsv(stream, file.FileName);
        return Ok(new { Count = funds.Count });
    }

    private static string FormatAnalysisStatus(Domain.Models.VCFund fund)
    {
        var interested = fund.Capitalists.Count(c =>
            c.FinancialModelInterest.Status == Domain.Models.InterestStatus.Interested);
        var unknown = fund.Capitalists.Count(c =>
            c.FinancialModelInterest.Status == Domain.Models.InterestStatus.Unknown);
        var notInterested = fund.Capitalists.Count(c =>
            c.FinancialModelInterest.Status == Domain.Models.InterestStatus.NotInterested);
        return $"⚪{interested} △{unknown} ✖{notInterested}";
    }

    public record VCListItem(string Name, string WebsiteUrl, string InvestmentStage, string InvestmentTheme, string AnalysisStatus);
    public record VCDetail(string Name, string WebsiteUrl, string InvestmentStage, string InvestmentTheme, List<CapitalistSummary> Capitalists);
    public record CapitalistSummary(string Name, string Title, string InvestmentDomain, string InterestStatus, string EvidenceSummary);
    public record AddVCRequest(string Name, string InvestmentStage, string InvestmentTheme);
}
