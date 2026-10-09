using Recycler.Models.DebtRecycling;
using Recycler.Models.UpBank;

namespace Recycler.Services;

public static class ReportRenderer
{
    public static void DisplayTransactionTable(List<UpTransaction> txns)
    {
        Console.WriteLine($"{"Date",-12} {"Description",-30} {"Amount",12} {"Status",-8}");
        Console.WriteLine(new string('-', 64));

        foreach (var tx in txns.OrderBy(t => t.Attributes.CreatedAt))
        {
            var date = tx.Attributes.CreatedAt.DateTime.ToString("yyyy-MM-dd");
            var desc = tx.Attributes.Description.Length > 30
                ? tx.Attributes.Description[..27] + "..."
                : tx.Attributes.Description;
            var amount = tx.Attributes.Amount.Value;
            var status = tx.Attributes.Status;

            Console.WriteLine($"{date,-12} {desc,-30} {amount,12} {status,-8}");
        }

        Console.WriteLine(new string('-', 64));
        Console.WriteLine($"Total transactions: {txns.Count}");
    }

    public static void DisplayDebtRecyclingReports(DebtRecyclingResult result)
    {
        DisplayInterestTracking(result);
        DisplayMonthlyAmortisation(result);
        DisplayFinancialYearSummary(result);
    }

    private static void DisplayInterestTracking(DebtRecyclingResult result)
    {
        Console.WriteLine();
        Console.WriteLine("=== Deductible Interest Tracking ===");
        Console.WriteLine();
        Console.WriteLine($"{"Date",-12} {"Description",-28} {"Total Interest",15} {"Deductible",12} {"Non-Deductible",16} {"Ded%",6}");
        Console.WriteLine(new string('-', 91));

        foreach (var ct in result.Transactions.Where(t => t.Type == TransactionType.InterestCharge))
        {
            var date = ct.Transaction.Attributes.CreatedAt.DateTime.ToString("yyyy-MM-dd");
            var desc = ct.Transaction.Attributes.Description.Length > 28
                ? ct.Transaction.Attributes.Description[..25] + "..."
                : ct.Transaction.Attributes.Description;
            var total = (long)Math.Abs(ct.Transaction.Attributes.Amount.ValueInBaseUnits);
            var dedPct = total > 0 ? (double)ct.DeductibleAmountCents / total * 100 : 0;

            Console.WriteLine($"{date,-12} {desc,-28} {FormatCents(total),15} {FormatCents(ct.DeductibleAmountCents),12} {FormatCents(ct.NonDeductibleAmountCents),16} {dedPct,5:F1}%");
        }
    }

    private static void DisplayMonthlyAmortisation(DebtRecyclingResult result)
    {
        Console.WriteLine();
        Console.WriteLine("=== Monthly Amortisation Schedule ===");
        Console.WriteLine();
        Console.WriteLine($"{"Month",-10} {"Repayment",11} {"Interest",10} {"Redraws",10} {"Ded.Int",10} {"NonDed.Int",12} {"Ded.Bal",12} {"NonDed.Bal",12} {"Total Bal",12} {"Ded%",6}");
        Console.WriteLine(new string('-', 107));

        foreach (var m in result.MonthlyAmortisation)
        {
            var month = $"{m.Year}-{m.Month:D2}";
            var totalBal = m.DeductibleBalanceCents + m.NonDeductibleBalanceCents;
            var dedPct = totalBal > 0 ? (double)m.DeductibleBalanceCents / totalBal * 100 : 0;

            Console.WriteLine($"{month,-10} {FormatCents(m.RepaymentCents),11} {FormatCents(m.InterestCents),10} {FormatCents(m.RedrawCents),10} {FormatCents(m.DeductibleInterestCents),10} {FormatCents(m.NonDeductibleInterestCents),12} {FormatCents(m.DeductibleBalanceCents),12} {FormatCents(m.NonDeductibleBalanceCents),12} {FormatCents(totalBal),12} {dedPct,5:F1}%");
        }
    }

    private static void DisplayFinancialYearSummary(DebtRecyclingResult result)
    {
        Console.WriteLine();
        Console.WriteLine("=== Financial Year Tax Summary ===");
        Console.WriteLine();
        Console.WriteLine($"{"Financial Year",-16} {"Total Interest",15} {"Deductible Interest",20} {"Non-Deductible Interest",24}");
        Console.WriteLine(new string('-', 77));

        foreach (var fy in result.FinancialYearSummaries)
        {
            var fyLabel = $"FY {fy.StartYear}-{(fy.StartYear + 1) % 100:D2}";
            Console.WriteLine($"{fyLabel,-16} {FormatCents(fy.TotalInterestCents),15} {FormatCents(fy.DeductibleInterestCents),20} {FormatCents(fy.NonDeductibleInterestCents),24}");
            Console.WriteLine($"  → For your {fyLabel} tax return, claim {FormatCents(fy.DeductibleInterestCents)} as deductible interest.");
        }
    }

    private static string FormatCents(long cents) => $"${cents / 100m:N2}";
}
