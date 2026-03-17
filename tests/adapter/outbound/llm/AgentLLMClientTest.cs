using System.Net;
using System.Text.Json;
using InvestorList.Adapter.Outbound.LLM;
using InvestorList.Domain.Models;

namespace InvestorList.Adapter.Tests.Outbound.LLM;

public class AgentLLMClientTest
{
    [Fact]
    public async Task キャピタリストの関心度を判定できる()
    {
        var json = JsonSerializer.Serialize(new
        {
            status = "Interested",
            capitalist_name = "田中太郎"
        });
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") };
        var client = new AgentLLMClient(httpClient);

        var capitalist = new Capitalist("田中太郎", "Partner", "Fintech");
        capitalist.FinancialModelInterest.AddEvidence(
            new Evidence(EvidenceType.Portfolio, "FP&A投資", "https://example.com"));

        var result = await client.Judge(capitalist);

        Assert.Equal(InterestStatus.Interested, result);
    }

    [Fact]
    public async Task 不明な応答はUnknownになる()
    {
        var json = JsonSerializer.Serialize(new
        {
            status = "Unknown",
            capitalist_name = "田中太郎"
        });
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8000") };
        var client = new AgentLLMClient(httpClient);

        var capitalist = new Capitalist("田中太郎", "Partner", "Fintech");

        var result = await client.Judge(capitalist);

        Assert.Equal(InterestStatus.Unknown, result);
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
