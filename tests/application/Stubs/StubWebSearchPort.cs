using InvestorList.Port.Outbound;
using InvestorList.Domain.Models;

namespace InvestorList.Application.Tests.Stubs;

public class StubWebSearchPort : IWebSearchPort
{
    private readonly VCFund _fund;
    private readonly List<Capitalist> _capitalists;
    private readonly List<Evidence> _evidences;

    public StubWebSearchPort(
        VCFund? fund = null,
        List<Capitalist>? capitalists = null,
        List<Evidence>? evidences = null)
    {
        _fund = fund ?? new VCFund("Stub VC", "https://stub.com", "Seed", "Tech");
        _capitalists = capitalists ?? [];
        _evidences = evidences ?? [];
    }

    public Task<VCFund> SearchVCProfile(string vcName, string? knownUrl = null) => Task.FromResult(_fund);
    public Task<List<Capitalist>> SearchCapitalists(string vcName, string websiteUrl = "") => Task.FromResult(_capitalists);
    public Task<List<Evidence>> SearchEvidences(string capitalistName, string vcName = "") => Task.FromResult(_evidences);
}
