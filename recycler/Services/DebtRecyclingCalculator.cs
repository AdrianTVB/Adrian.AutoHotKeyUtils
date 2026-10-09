using Recycler.Models.DebtRecycling;

namespace Recycler.Services;

public class DebtRecyclingCalculator
{
    public DebtRecyclingResult Calculate(long initialBalanceCents, List<ClassifiedTransaction> transactions)
    {
        long nonDedBal = initialBalanceCents;
        long dedBal = 0;

        var sorted = transactions
            .OrderBy(t => t.Transaction.Attributes.CreatedAt)
            .ToList();

        var monthlyDict = new Dictionary<(int Year, int Month), AmortisationEntry>();
        var fyDict = new Dictionary<int, FinancialYearSummary>();

        foreach (var ct in sorted)
        {
            long totalBalance = dedBal + nonDedBal;
            long amountCents = (long)Math.Abs(ct.Transaction.Attributes.Amount.ValueInBaseUnits);
            var localDate = ct.Transaction.Attributes.CreatedAt.DateTime;
            var monthKey = (localDate.Year, localDate.Month);

            if (!monthlyDict.TryGetValue(monthKey, out var monthEntry))
            {
                monthEntry = new AmortisationEntry { Year = monthKey.Year, Month = monthKey.Month };
                monthlyDict[monthKey] = monthEntry;
            }

            switch (ct.Type)
            {
                case TransactionType.InvestmentRedraw:
                    dedBal += amountCents;
                    ct.DeductibleAmountCents = amountCents;
                    ct.NonDeductibleAmountCents = 0;
                    monthEntry.RedrawCents += amountCents;
                    break;

                case TransactionType.Repayment:
                    if (totalBalance > 0)
                    {
                        long dedPortion = (long)Math.Round((double)amountCents * dedBal / totalBalance);
                        long nonDedPortion = amountCents - dedPortion;
                        dedBal -= dedPortion;
                        nonDedBal -= nonDedPortion;
                        ct.DeductibleAmountCents = dedPortion;
                        ct.NonDeductibleAmountCents = nonDedPortion;
                    }
                    else
                    {
                        ct.DeductibleAmountCents = 0;
                        ct.NonDeductibleAmountCents = amountCents;
                    }
                    monthEntry.RepaymentCents += amountCents;
                    break;

                case TransactionType.InterestCharge:
                {
                    long dedPortion = 0;
                    long nonDedPortion = amountCents;

                    if (totalBalance > 0)
                    {
                        dedPortion = (long)Math.Round((double)amountCents * dedBal / totalBalance);
                        nonDedPortion = amountCents - dedPortion;
                    }

                    dedBal += dedPortion;
                    nonDedBal += nonDedPortion;
                    ct.DeductibleAmountCents = dedPortion;
                    ct.NonDeductibleAmountCents = nonDedPortion;

                    monthEntry.InterestCents += amountCents;
                    monthEntry.DeductibleInterestCents += dedPortion;
                    monthEntry.NonDeductibleInterestCents += nonDedPortion;

                    int fyStart = localDate.Month >= 7 ? localDate.Year : localDate.Year - 1;
                    if (!fyDict.TryGetValue(fyStart, out var fy))
                    {
                        fy = new FinancialYearSummary { StartYear = fyStart };
                        fyDict[fyStart] = fy;
                    }
                    fy.TotalInterestCents += amountCents;
                    fy.DeductibleInterestCents += dedPortion;
                    fy.NonDeductibleInterestCents += nonDedPortion;
                    break;
                }

                case TransactionType.OtherDebit:
                    nonDedBal += amountCents;
                    ct.DeductibleAmountCents = 0;
                    ct.NonDeductibleAmountCents = amountCents;
                    break;
            }

            monthEntry.DeductibleBalanceCents = dedBal;
            monthEntry.NonDeductibleBalanceCents = nonDedBal;
        }

        return new DebtRecyclingResult
        {
            Transactions = sorted,
            MonthlyAmortisation = monthlyDict.Values
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToList(),
            FinancialYearSummaries = fyDict.Values
                .OrderBy(f => f.StartYear)
                .ToList()
        };
    }
}

public class DebtRecyclingResult
{
    public required List<ClassifiedTransaction> Transactions { get; init; }
    public required List<AmortisationEntry> MonthlyAmortisation { get; init; }
    public required List<FinancialYearSummary> FinancialYearSummaries { get; init; }
}
