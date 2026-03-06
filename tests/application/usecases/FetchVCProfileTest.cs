using InvestorList.Application.Tests.Stubs;
using InvestorList.Application.UseCases;
using InvestorList.Domain.Models;

namespace InvestorList.Application.Tests.UseCases;

public class FetchVCProfileTest
{
    [Fact]
    public async Task VC名からVCファンドを生成できる()
    {
        var fund = new VCFund("ABC Capital", "https://abc-capital.com", "Seed", "SaaS");
        var stub = new StubWebSearchPort(fund: fund);

        var useCase = new FetchVCProfile(stub);
        var result = await useCase.Execute("ABC Capital");

        Assert.Equal("ABC Capital", result.Name);
        Assert.Equal("Seed", result.InvestmentStage);
    }
}
