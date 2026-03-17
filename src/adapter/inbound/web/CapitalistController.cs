using System.Text;
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

    [HttpGet("export")]
    public async Task<IActionResult> ExportCsv()
    {
        var funds = await _vcListPort.GetAllVCs();
        var capitalists = funds.SelectMany(f => f.Capitalists.Select(c => new
        {
            VcName = f.Name,
            c.Name,
            c.Title,
            c.InvestmentDomain,
            InterestStatus = c.FinancialModelInterest.Status.ToString(),
            EvidenceSummary = c.FinancialModelInterest.Evidences.Select(e => e.Summary).FirstOrDefault() ?? ""
        })).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("\"VC名\",\"氏名\",\"役職\",\"投資担当領域\",\"関心度\",\"判定理由（エビデンス）\"");

        foreach (var c in capitalists)
        {
            var interest = c.InterestStatus switch
            {
                "Interested" => "関心あり",
                "NotInterested" => "関心なし",
                _ => "不明"
            };
            sb.AppendLine($"{CsvEscape(c.VcName)},{CsvEscape(c.Name)},{CsvEscape(c.Title)},{CsvEscape(c.InvestmentDomain)},{CsvEscape(interest)},{CsvEscape(c.EvidenceSummary)}");
        }

        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var csvBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[bom.Length + csvBytes.Length];
        bom.CopyTo(result, 0);
        csvBytes.CopyTo(result, bom.Length);

        return File(result, "text/csv", "capitalists.csv");
    }

    private static string CsvEscape(string value)
    {
        if (value is null) return "\"\"";
        return "\"" + value.Replace("\"", "\"\"") + "\"";
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
