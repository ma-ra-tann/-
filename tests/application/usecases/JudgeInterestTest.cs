using InvestorList.Application.Tests.Stubs;
using InvestorList.Port.Outbound;
using InvestorList.Application.UseCases;
using InvestorList.Domain.Models;

namespace InvestorList.Application.Tests.UseCases;

public class JudgeInterestTest
{
    [Fact]
    public async Task 財務モデルに関する根拠があれば興味ありと判定される()
    {
        // Arrange
        var stubLLM = new StubLLMAnalysisPort(InterestStatus.Interested);
        var useCase = new JudgeInterest(stubLLM);

        var capitalist = new Capitalist("田中太郎", "Partner", "Fintech");
        capitalist.FinancialModelInterest.AddEvidence(
            new Evidence(EvidenceType.Article, "財務モデル記事を執筆", "https://example.com"));

        // Act
        var result = await useCase.Execute(capitalist);

        // Assert
        Assert.Equal(InterestStatus.Interested, result.FinancialModelInterest.Status);
    }

    [Fact]
    public async Task ターゲット外であることが明確な根拠があれば興味なしと判定される()
    {
        // Arrange
        var stubLLM = new StubLLMAnalysisPort(InterestStatus.NotInterested);
        var useCase = new JudgeInterest(stubLLM);

        var capitalist = new Capitalist("山田花子", "Partner", "Agriculture");
        capitalist.FinancialModelInterest.AddEvidence(
            new Evidence(EvidenceType.Article, "農業系スタートアップのみに投資していく方針", "https://example.com/agriculture"));

        // Act
        var result = await useCase.Execute(capitalist);

        // Assert
        Assert.Equal(InterestStatus.NotInterested, result.FinancialModelInterest.Status);
    }

    [Fact]
    public async Task 根拠が全くない場合は不明と判定される()
    {
        // Arrange
        var stubLLM = new StubLLMAnalysisPort(InterestStatus.Unknown);
        var useCase = new JudgeInterest(stubLLM);

        var capitalist = new Capitalist("佐藤次郎", "Associate", "Marketing");
        // 証拠（Evidence）は何も追加しない

        // Act
        var result = await useCase.Execute(capitalist);

        // Assert
        Assert.Equal(InterestStatus.Unknown, result.FinancialModelInterest.Status);
    }
}

public class StubLLMAnalysisPort : ILLMAnalysisPort
{
    private readonly InterestStatus _status;

    public StubLLMAnalysisPort(InterestStatus status)
    {
        _status = status;
    }

    public Task<InterestStatus> Judge(Capitalist capitalist) => Task.FromResult(_status);
}
