using InvestorList.Domain.Models;

namespace InvestorList.Domain.Tests.Models;

public class VCFundTest
{
    [Fact]
    public void VCファンドを作成できる()
    {
        var fund = new VCFund(
            name: "ABC Capital",
            websiteUrl: "https://abc-capital.com",
            investmentStage: "Seed",
            investmentTheme: "SaaS"
        );

        Assert.Equal("ABC Capital", fund.Name);
        Assert.Equal("https://abc-capital.com", fund.WebsiteUrl);
        Assert.Equal("Seed", fund.InvestmentStage);
        Assert.Equal("SaaS", fund.InvestmentTheme);
    }

    [Fact]
    public void キャピタリストを追加できる()
    {
        var fund = new VCFund(
            name: "ABC Capital",
            websiteUrl: "https://abc-capital.com",
            investmentStage: "Seed",
            investmentTheme: "SaaS"
        );

        var capitalist = new Capitalist(
            name: "田中太郎",
            title: "Partner",
            investmentDomain: "Fintech"
        );

        fund.AddCapitalist(capitalist);

        Assert.Single(fund.Capitalists);
        Assert.Equal("田中太郎", fund.Capitalists[0].Name);
    }

    [Fact]
    public void 初期状態ではキャピタリストは空()
    {
        var fund = new VCFund(
            name: "ABC Capital",
            websiteUrl: "https://abc-capital.com",
            investmentStage: "Seed",
            investmentTheme: "SaaS"
        );

        Assert.Empty(fund.Capitalists);
    }
}
