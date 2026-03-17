using InvestorList.Domain.Models;

namespace InvestorList.Port.Inbound;

public interface IAnalysisPort
{
    Task<VCFund> AnalyzeVC(string vcName);
    Task<Capitalist?> GetCapitalistDetail(string vcName, string capitalistName);
}
