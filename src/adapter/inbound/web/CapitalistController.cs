using Microsoft.AspNetCore.Mvc;
using InvestorList.Port.Inbound;

namespace InvestorList.Adapter.Inbound.Web;

[ApiController]
[Route("api/capitalist")]
public class CapitalistController : ControllerBase
{
    private readonly IAnalysisPort _analysisPort;
    private readonly IVCListPort _vcListPort;

    public CapitalistController(IAnalysisPort analysisPort, IVCListPort vcListPort)
    {
        _analysisPort = analysisPort;
        _vcListPort = vcListPort;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var funds = await _vcListPort.GetAllVCs();
        var capitalists = funds.SelectMany(f => f.Capitalists.Select(c => new CapitalistListItem(
            f.Name,
            c.Name,
            c.Title,
            c.InvestmentDomain,
            c.FinancialModelInterest.Status.ToString(),
            c.FinancialModelInterest.Evidences.Select(e => e.Summary).FirstOrDefault() ?? ""
        ))).ToList();

        return Ok(capitalists);
    }

    [HttpGet("{vcName}/{capitalistName}")]
    public async Task<IActionResult> GetDetail(string vcName, string capitalistName)
    {
        var capitalist = await _analysisPort.GetCapitalistDetail(vcName, capitalistName);
        if (capitalist is null) return NotFound();

        return Ok(new CapitalistDetail(
            capitalist.Name,
            capitalist.Title,
            capitalist.InvestmentDomain,
            capitalist.FinancialModelInterest.Status.ToString(),
            capitalist.FinancialModelInterest.Evidences
                .Select(e => new EvidenceItem(e.Type.ToString(), e.Summary, e.SourceUrl))
                .ToList()
        ));
    }

    public record CapitalistDetail(
        string Name,
        string Title,
        string InvestmentDomain,
        string InterestStatus,
        List<EvidenceItem> Evidences);

    public record CapitalistListItem(
        string VcName,
        string Name,
        string Title,
        string InvestmentDomain,
        string InterestStatus,
        string EvidenceSummary);

    public record EvidenceItem(string Type, string Summary, string SourceUrl);
}
