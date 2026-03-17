using System.Net;
using System.Text.Json;
using InvestorList.Adapter.Outbound.Search;
using InvestorList.Domain.Models;

namespace InvestorList.Adapter.Tests.Outbound.Search;

public class AgentServiceClientTest
{
    private static HttpClient CreateMockHttpClient(HttpStatusCode status, string json)
    {
        var handler = new StubHttpMessageHandler(status, json);
        return new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") };
    }

    [Fact]
    public async Task VCプロフィールを検索できる()
    {
        var json = JsonSerializer.Serialize(new
        {
            name = "ABC Capital",
            website_url = "https://abc.com",
            investment_stage = "Seed",
            investment_theme = "SaaS"
        });
        var client = new AgentServiceClient(CreateMockHttpClient(HttpStatusCode.OK, json));

        var result = await client.SearchVCProfile("ABC Capital");

        Assert.Equal("ABC Capital", result.Name);
        Assert.Equal("Seed", result.InvestmentStage);
    }

    [Fact]
    public async Task キャピタリスト一覧を検索できる()
    {
        var json = JsonSerializer.Serialize(new[]
        {
            new { name = "田中太郎", title = "Partner", investment_domain = "Fintech" }
        });
        var client = new AgentServiceClient(CreateMockHttpClient(HttpStatusCode.OK, json));

        var result = await client.SearchCapitalists("ABC Capital");

        Assert.Single(result);
        Assert.Equal("田中太郎", result[0].Name);
    }

    [Fact]
    public async Task 根拠を検索できる()
    {
        var json = JsonSerializer.Serialize(new[]
        {
            new { type = "Portfolio", summary = "FP&A投資", source_url = "https://example.com" }
        });
        var client = new AgentServiceClient(CreateMockHttpClient(HttpStatusCode.OK, json));

        var result = await client.SearchEvidences("田中太郎");

        Assert.Single(result);
        Assert.Equal(EvidenceType.Portfolio, result[0].Type);
    }
}

internal class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _content;

    public StubHttpMessageHandler(HttpStatusCode statusCode, string content)
    {
        _statusCode = statusCode;
        _content = content;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_content, System.Text.Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
