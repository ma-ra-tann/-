using InvestorList.Domain.Models;

namespace InvestorList.Port.Outbound;

public interface IWebSearchPort
{
    Task<VCFund> SearchVCProfile(string vcName, string? knownUrl = null);
    Task<List<Capitalist>> SearchCapitalists(string vcName);
    Task<List<Evidence>> SearchEvidences(string capitalistName, string vcName = "");
}
