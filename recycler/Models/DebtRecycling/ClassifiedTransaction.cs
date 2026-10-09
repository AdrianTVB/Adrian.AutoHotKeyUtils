using Recycler.Models.UpBank;

namespace Recycler.Models.DebtRecycling;

public enum TransactionType
{
    Repayment,
    InterestCharge,
    InvestmentRedraw,
    OtherDebit
}

public class ClassifiedTransaction
{
    public required UpTransaction Transaction { get; init; }
    public required TransactionType Type { get; init; }
    public long DeductibleAmountCents { get; set; }
    public long NonDeductibleAmountCents { get; set; }
}
