using InvestorList.Domain.Models;

namespace InvestorList.Domain.Tests.Models;

public class EvidenceTest
{
    [Fact]
    public void 根拠を作成できる()
    {
        var evidence = new Evidence(
            type: EvidenceType.Portfolio,
            summary: "財務モデルSaaSに投資",
            sourceUrl: "https://example.com/portfolio"
        );

        Assert.Equal(EvidenceType.Portfolio, evidence.Type);
        Assert.Equal("財務モデルSaaSに投資", evidence.Summary);
        Assert.Equal("https://example.com/portfolio", evidence.SourceUrl);
    }

    [Theory]
    [InlineData(EvidenceType.Portfolio)]
    [InlineData(EvidenceType.Statement)]
    [InlineData(EvidenceType.Article)]
    [InlineData(EvidenceType.Talk)]
    public void 全ての種別で作成できる(EvidenceType type)
    {
        var evidence = new Evidence(
            type: type,
            summary: "テスト",
            sourceUrl: "https://example.com"
        );

        Assert.Equal(type, evidence.Type);
    }
}
