namespace Recycler.Models.DebtRecycling;

public class FinancialYearSummary
{
    public int StartYear { get; init; }
    public long TotalInterestCents { get; set; }
    public long DeductibleInterestCents { get; set; }
    public long NonDeductibleInterestCents { get; set; }
}
