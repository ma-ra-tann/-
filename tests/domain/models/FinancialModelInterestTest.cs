using InvestorList.Domain.Models;

namespace InvestorList.Domain.Tests.Models;

public class FinancialModelInterestTest
{
    [Fact]
    public void 初期状態は不明()
    {
        var interest = new FinancialModelInterest();

        Assert.Equal(InterestStatus.Unknown, interest.Status);
    }

    [Fact]
    public void 根拠を追加できる()
    {
        var interest = new FinancialModelInterest();
        var evidence = new Evidence(
            type: EvidenceType.Article,
            summary: "財務モデルに関する記事を執筆",
            sourceUrl: "https://example.com/article"
        );

        interest.AddEvidence(evidence);

        Assert.Single(interest.Evidences);
    }

    [Fact]
    public void 初期状態では根拠は空()
    {
        var interest = new FinancialModelInterest();

        Assert.Empty(interest.Evidences);
    }
}
