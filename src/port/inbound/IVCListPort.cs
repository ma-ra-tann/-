using InvestorList.Domain.Models;

namespace InvestorList.Port.Inbound;

public interface IVCListPort
{
    Task<List<VCFund>> ImportFromCsv(Stream fileStream, string fileName = "");
    Task<VCFund> AddVC(string name, string investmentStage, string investmentTheme);
    Task<List<VCFund>> GetAllVCs();
    Task<VCFund?> GetVCDetail(string vcName);
}
