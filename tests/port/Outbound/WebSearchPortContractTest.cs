using InvestorList.Domain.Models;
using InvestorList.Port.Outbound;

namespace InvestorList.Port.Tests.Outbound;

public abstract class WebSearchPortContractTest
{
    protected abstract IWebSearchPort CreatePort();

    [Fact]
    public async Task SearchVCProfileはVCファンドを返す()
    {
        var port = CreatePort();
        var result = await port.SearchVCProfile("Test VC");

        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Name));
    }

    [Fact]
    public async Task SearchCapitalistsはリストを返す()
    {
        var port = CreatePort();
        var result = await port.SearchCapitalists("Test VC");

        Assert.NotNull(result);
        Assert.IsType<List<Capitalist>>(result);
    }

    [Fact]
    public async Task SearchEvidencesはリストを返す()
    {
        var port = CreatePort();
        var result = await port.SearchEvidences("Test Person");

        Assert.NotNull(result);
        Assert.IsType<List<Evidence>>(result);
    }
}
