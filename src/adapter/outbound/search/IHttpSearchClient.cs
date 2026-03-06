namespace InvestorList.Adapter.Outbound.Search;

public interface IHttpSearchClient
{
    Task<string> SearchVC(string vcName);
    Task<string> SearchCapitalists(string vcName);
    Task<string> SearchEvidences(string capitalistName);
}
