using InvestorList.Domain.Models;
using InvestorList.Port.Outbound;

namespace InvestorList.Port.Tests.Outbound;

public abstract class DataPersistencePortContractTest
{
    protected abstract IDataPersistencePort CreatePort();

    [Fact]
    public async Task 保存したVCファンドを名前で取得できる()
    {
        var port = CreatePort();
        var fund = new VCFund("Contract VC", "https://contract.com", "Seed", "SaaS");

        await port.Save(fund);
        var result = await port.FindByName("Contract VC");

        Assert.NotNull(result);
        Assert.Equal("Contract VC", result.Name);
    }

    [Fact]
    public async Task 存在しない名前はnullを返す()
    {
        var port = CreatePort();

        var result = await port.FindByName("存在しない");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllは保存した全件を返す()
    {
        var port = CreatePort();
        await port.Save(new VCFund("VC A", "https://a.com", "Seed", "SaaS"));
        await port.Save(new VCFund("VC B", "https://b.com", "Pre-Seed", "AI"));

        var results = await port.GetAll();

        Assert.Equal(2, results.Count);
    }
}
