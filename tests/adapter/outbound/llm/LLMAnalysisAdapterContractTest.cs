using InvestorList.Adapter.Outbound.LLM;
using InvestorList.Port.Outbound;
using InvestorList.Port.Tests.Outbound;

namespace InvestorList.Adapter.Tests.Outbound.LLM;

public class LLMAnalysisAdapterContractTest : LLMAnalysisPortContractTest
{
    protected override ILLMAnalysisPort CreatePort()
    {
        var stub = new StubLLMClient("Interested");
        return new LLMAnalysisAdapter(stub);
    }
}
