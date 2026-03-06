namespace InvestorList.Domain.Models;

public class VCFund
{
    public string Name { get; }
    public string WebsiteUrl { get; }
    public string InvestmentStage { get; }
    public string InvestmentTheme { get; }
    private readonly List<Capitalist> _capitalists = [];

    public IReadOnlyList<Capitalist> Capitalists => _capitalists;

    public VCFund(string name, string websiteUrl, string investmentStage, string investmentTheme)
    {
        Name = name;
        WebsiteUrl = websiteUrl;
        InvestmentStage = investmentStage;
        InvestmentTheme = investmentTheme;
    }

    public void AddCapitalist(Capitalist capitalist)
    {
        _capitalists.Add(capitalist);
    }
}
