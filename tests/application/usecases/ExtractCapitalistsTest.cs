using InvestorList.Application.Tests.Stubs;
using InvestorList.Application.UseCases;
using InvestorList.Domain.Models;

namespace InvestorList.Application.Tests.UseCases;

public class ExtractCapitalistsTest
{
    [Fact]
    public async Task VCファンドにキャピタリストを抽出して追加できる()
    {
        var capitalists = new List<Capitalist>
        {
            new("田中太郎", "Partner", "Fintech"),
            new("鈴木花子", "Associate", "SaaS")
        };

        var stub = new StubWebSearchPort(capitalists: capitalists);
        var useCase = new ExtractCapitalists(stub);
        var fund = new VCFund("ABC Capital", "https://abc-capital.com", "Seed", "SaaS");

        var result = await useCase.Execute(fund);

        Assert.Equal(2, result.Capitalists.Count);
        Assert.Equal("田中太郎", result.Capitalists[0].Name);
        Assert.Equal("鈴木花子", result.Capitalists[1].Name);
    }
}
