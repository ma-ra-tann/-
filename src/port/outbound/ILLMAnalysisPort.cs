using InvestorList.Domain.Models;

namespace InvestorList.Port.Outbound;

public interface ILLMAnalysisPort
{
    Task<InterestStatus> Judge(Capitalist capitalist);
}
