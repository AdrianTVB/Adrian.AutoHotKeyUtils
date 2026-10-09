namespace Recycler.Models.DebtRecycling;

public class AmortisationEntry
{
    public int Year { get; init; }
    public int Month { get; init; }
    public long RepaymentCents { get; set; }
    public long InterestCents { get; set; }
    public long RedrawCents { get; set; }
    public long DeductibleInterestCents { get; set; }
    public long NonDeductibleInterestCents { get; set; }
    public long DeductibleBalanceCents { get; set; }
    public long NonDeductibleBalanceCents { get; set; }
}
