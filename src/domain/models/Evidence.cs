namespace InvestorList.Domain.Models;

public enum EvidenceType
{
    Portfolio,
    Statement,
    Article,
    Talk
}

public class Evidence
{
    public EvidenceType Type { get; }
    public string Summary { get; }
    public string SourceUrl { get; }

    public Evidence(EvidenceType type, string summary, string sourceUrl)
    {
        Type = type;
        Summary = summary;
        SourceUrl = sourceUrl;
    }
}
