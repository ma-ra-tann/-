using InvestorList.Adapter.Outbound.Db;
using InvestorList.Port.Outbound;
using InvestorList.Port.Tests.Outbound;

namespace InvestorList.Adapter.Tests.Outbound.Db;

public class JsonDataStoreContractTest : DataPersistencePortContractTest, IDisposable
{
    private readonly string _testFilePath = Path.Combine(Path.GetTempPath(), $"contract_{Guid.NewGuid()}.json");

    protected override IDataPersistencePort CreatePort() => new JsonDataStore(_testFilePath);

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
            File.Delete(_testFilePath);
    }
}
