namespace InvestorList.Domain.Models;

public class Capitalist
{
    public string Name { get; }
    public string Title { get; }
    public string InvestmentDomain { get; }
    public FinancialModelInterest FinancialModelInterest { get; } = new();

    public Capitalist(string name, string title, string investmentDomain)
    {
        Name = name;
        Title = title;
        InvestmentDomain = investmentDomain;
    }
}
