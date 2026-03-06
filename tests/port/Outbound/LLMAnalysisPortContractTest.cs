using InvestorList.Domain.Models;
using InvestorList.Port.Outbound;

namespace InvestorList.Port.Tests.Outbound;

public abstract class LLMAnalysisPortContractTest
{
    protected abstract ILLMAnalysisPort CreatePort();

    [Fact]
    public async Task Judgeは有効なInterestStatusを返す()
    {
        var port = CreatePort();
        var capitalist = new Capitalist("テスト太郎", "Partner", "Fintech");

        var result = await port.Judge(capitalist);

        Assert.True(Enum.IsDefined(typeof(InterestStatus), result));
    }
}
