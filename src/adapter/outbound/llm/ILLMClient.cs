namespace InvestorList.Adapter.Outbound.LLM;

public interface ILLMClient
{
    Task<string> Ask(string prompt);
}
