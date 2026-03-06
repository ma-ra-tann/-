using InvestorList.Adapter.Outbound.Search;
using InvestorList.Port.Outbound;
using InvestorList.Port.Tests.Outbound;

namespace InvestorList.Adapter.Tests.Outbound.Search;

public class WebSearchAdapterContractTest : WebSearchPortContractTest
{
    protected override IWebSearchPort CreatePort()
    {
        var stub = new StubHttpSearchClient(
            vcJson: """{"name":"Test VC","websiteUrl":"https://test.com","investmentStage":"Seed","investmentTheme":"Tech"}""",
            capitalistsJson: """[{"name":"テスト太郎","title":"Partner","investmentDomain":"Fintech"}]""",
            evidencesJson: """[{"type":"Article","summary":"テスト記事","sourceUrl":"https://example.com"}]"""
        );
        return new WebSearchAdapter(stub);
    }
}
