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
        const int maxEvidences = 6;

        var funds = await _vcListPort.GetAllVCs();
        var rows = funds.SelectMany(f => f.Capitalists.Select(c => new
        {
            VcName = f.Name,
            VcUrl = f.WebsiteUrl,
            c.Name,
            c.Title,
            c.InvestmentDomain,
            InterestStatus = c.FinancialModelInterest.Status.ToString(),
            Evidences = c.FinancialModelInterest.Evidences.ToList()
        })).ToList();

        var sb = new StringBuilder();
        var header = new List<string> { "VC名", "VCサイトURL", "氏名", "役職", "投資担当領域", "関心度" };
        for (int i = 1; i <= maxEvidences; i++)
        {
            header.Add($"判定理由{i}");
            header.Add($"根拠URL{i}");
        }
        sb.AppendLine(string.Join(",", header.Select(h => CsvEscape(h))));

        foreach (var c in rows)
        {
            var interest = c.InterestStatus switch
            {
                "Interested" => "関心あり",
                "NotInterested" => "関心なし",
                _ => "不明"
            };
            var cols = new List<string> { c.VcName, c.VcUrl, c.Name, c.Title, c.InvestmentDomain, interest };
            for (int i = 0; i < maxEvidences; i++)
            {
                if (i < c.Evidences.Count)
                {
                    cols.Add(c.Evidences[i].Summary ?? "");
                    cols.Add(c.Evidences[i].SourceUrl ?? "");
                }
                else
                {
                    cols.Add("");
                    cols.Add("");
                }
            }
            sb.AppendLine(string.Join(",", cols.Select(v => CsvEscape(v))));
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
