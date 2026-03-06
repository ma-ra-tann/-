namespace InvestorList.Domain.Models;

public enum InterestStatus
{
    Interested,
    Unknown,
    NotInterested
}

public class FinancialModelInterest
{
    public InterestStatus Status { get; set; } = InterestStatus.Unknown;
    private readonly List<Evidence> _evidences = [];

    public IReadOnlyList<Evidence> Evidences => _evidences;

    public void AddEvidence(Evidence evidence)
    {
        _evidences.Add(evidence);
    }
}
