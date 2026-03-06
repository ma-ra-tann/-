using InvestorList.Domain.Models;

namespace InvestorList.Domain.Tests.Models;

public class CapitalistTest
{
    [Fact]
    public void キャピタリストを作成できる()
    {
        var capitalist = new Capitalist(
            name: "田中太郎",
            title: "Partner",
            investmentDomain: "Fintech"
        );

        Assert.Equal("田中太郎", capitalist.Name);
        Assert.Equal("Partner", capitalist.Title);
        Assert.Equal("Fintech", capitalist.InvestmentDomain);
    }

    [Fact]
    public void 財務モデル関心度を持つ()
    {
        var capitalist = new Capitalist(
            name: "田中太郎",
            title: "Partner",
            investmentDomain: "Fintech"
        );

        Assert.NotNull(capitalist.FinancialModelInterest);
        Assert.Equal(InterestStatus.Unknown, capitalist.FinancialModelInterest.Status);
    }
}
