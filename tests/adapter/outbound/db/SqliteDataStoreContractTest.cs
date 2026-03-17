using InvestorList.Port.Outbound;
using InvestorList.Port.Tests.Outbound;
using InvestorList.Adapter.Outbound.Db;

namespace InvestorList.Adapter.Tests.Outbound.Db;

public class SqliteDataStoreContractTest : DataPersistencePortContractTest
{
    protected override IDataPersistencePort CreatePort()
    {
        var store = new SqliteDataStore("Data Source=:memory:");
        store.InitializeAsync().GetAwaiter().GetResult();
        return store;
    }
}
