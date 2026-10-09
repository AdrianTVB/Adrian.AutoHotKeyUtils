using Recycler.Models.DebtRecycling;
using Recycler.Services;
using Recycler.Tests.Helpers;
using Xunit;

namespace Recycler.Tests;

public class DebtRecyclingCalculatorTests
{
    private readonly DebtRecyclingCalculator _calculator = new();

    private static DateTimeOffset Date(int year, int month, int day)
        => new(year, month, day, 0, 0, 0, TimeSpan.FromHours(11));

    [Fact]
    public void No_redraws_everything_is_non_deductible()
    {
        var transactions = new List<ClassifiedTransaction>
        {
            new() { Transaction = TransactionBuilder.Repayment(500000, Date(2025, 1, 15)), Type = TransactionType.Repayment },
            new() { Transaction = TransactionBuilder.Interest(150000, Date(2025, 1, 20)), Type = TransactionType.InterestCharge },
        };

        var result = _calculator.Calculate(50000000, transactions);

        foreach (var ct in result.Transactions)
        {
            Assert.Equal(0, ct.DeductibleAmountCents);
        }
    }

    [Fact]
    public void Single_redraw_proportional_split()
    {
        // $500k loan, $50k investment redraw = 10% deductible after redraw
        var transactions = new List<ClassifiedTransaction>
        {
            new() { Transaction = TransactionBuilder.Redraw(5000000, Date(2025, 1, 10)), Type = TransactionType.InvestmentRedraw },
            new() { Transaction = TransactionBuilder.Repayment(300000, Date(2025, 2, 1)), Type = TransactionType.Repayment },
            new() { Transaction = TransactionBuilder.Interest(180000, Date(2025, 2, 5)), Type = TransactionType.InterestCharge },
        };

        var result = _calculator.Calculate(50000000, transactions);

        // After redraw: ded=5000000, nonDed=50000000, total=55000000, ratio=5/55≈9.09%
        var repayment = result.Transactions[1];
        var interest = result.Transactions[2];

        // Repayment split: 300000 * 5000000/55000000 ≈ 27273
        Assert.Equal(27273, repayment.DeductibleAmountCents);
        Assert.Equal(300000 - 27273, repayment.NonDeductibleAmountCents);

        // After repayment: ded=5000000-27273=4972727, nonDed=50000000-272727=49727273, total=54700000
        // Interest split: 180000 * 4972727/54700000 ≈ 16364
        Assert.Equal(16364, interest.DeductibleAmountCents);
        Assert.Equal(180000 - 16364, interest.NonDeductibleAmountCents);
    }

    [Fact]
    public void Rounding_consistency_split_sums_to_total()
    {
        var transactions = new List<ClassifiedTransaction>
        {
            new() { Transaction = TransactionBuilder.Redraw(3333333, Date(2025, 1, 5)), Type = TransactionType.InvestmentRedraw },
            new() { Transaction = TransactionBuilder.Repayment(100000, Date(2025, 1, 15)), Type = TransactionType.Repayment },
            new() { Transaction = TransactionBuilder.Interest(77777, Date(2025, 1, 20)), Type = TransactionType.InterestCharge },
            new() { Transaction = TransactionBuilder.Repayment(100000, Date(2025, 2, 15)), Type = TransactionType.Repayment },
            new() { Transaction = TransactionBuilder.Interest(77777, Date(2025, 2, 20)), Type = TransactionType.InterestCharge },
        };

        var result = _calculator.Calculate(50000000, transactions);

        foreach (var ct in result.Transactions.Where(t =>
            t.Type is TransactionType.Repayment or TransactionType.InterestCharge))
        {
            long total = (long)Math.Abs(ct.Transaction.Attributes.Amount.ValueInBaseUnits);
            Assert.Equal(total, ct.DeductibleAmountCents + ct.NonDeductibleAmountCents);
        }
    }

    [Fact]
    public void Multiple_redraws_increase_deductible_ratio()
    {
        var transactions = new List<ClassifiedTransaction>
        {
            new() { Transaction = TransactionBuilder.Redraw(5000000, Date(2025, 1, 5)), Type = TransactionType.InvestmentRedraw },
            new() { Transaction = TransactionBuilder.Interest(180000, Date(2025, 1, 20)), Type = TransactionType.InterestCharge },
            new() { Transaction = TransactionBuilder.Redraw(5000000, Date(2025, 2, 5)), Type = TransactionType.InvestmentRedraw },
            new() { Transaction = TransactionBuilder.Interest(180000, Date(2025, 2, 20)), Type = TransactionType.InterestCharge },
        };

        var result = _calculator.Calculate(50000000, transactions);

        var interest1 = result.Transactions[1];
        var interest2 = result.Transactions[3];

        // Second interest charge should have higher deductible portion
        Assert.True(interest2.DeductibleAmountCents > interest1.DeductibleAmountCents);
    }

    [Fact]
    public void Interest_compounds_into_balance()
    {
        // After interest, total balance should increase
        var transactions = new List<ClassifiedTransaction>
        {
            new() { Transaction = TransactionBuilder.Redraw(5000000, Date(2025, 1, 5)), Type = TransactionType.InvestmentRedraw },
            new() { Transaction = TransactionBuilder.Interest(180000, Date(2025, 1, 20)), Type = TransactionType.InterestCharge },
        };

        var result = _calculator.Calculate(50000000, transactions);

        // After redraw: total = 55000000
        // After interest: total = 55000000 + 180000 = 55180000
        var month = result.MonthlyAmortisation.Single();
        Assert.Equal(55180000, month.DeductibleBalanceCents + month.NonDeductibleBalanceCents);
    }

    [Fact]
    public void Other_debit_allocated_entirely_to_non_deductible()
    {
        var transactions = new List<ClassifiedTransaction>
        {
            new() { Transaction = TransactionBuilder.Redraw(5000000, Date(2025, 1, 5)), Type = TransactionType.InvestmentRedraw },
            new() { Transaction = TransactionBuilder.Fee(10000, Date(2025, 1, 10)), Type = TransactionType.OtherDebit },
        };

        var result = _calculator.Calculate(50000000, transactions);

        var fee = result.Transactions.Single(t => t.Type == TransactionType.OtherDebit);
        Assert.Equal(0, fee.DeductibleAmountCents);
        Assert.Equal(10000, fee.NonDeductibleAmountCents);
    }

    [Fact]
    public void Zero_deductible_balance_no_division_by_zero()
    {
        // Before any redraw, ratio should be 0% deductible
        var transactions = new List<ClassifiedTransaction>
        {
            new() { Transaction = TransactionBuilder.Repayment(500000, Date(2025, 1, 15)), Type = TransactionType.Repayment },
            new() { Transaction = TransactionBuilder.Interest(150000, Date(2025, 1, 20)), Type = TransactionType.InterestCharge },
        };

        var result = _calculator.Calculate(50000000, transactions);

        var repayment = result.Transactions[0];
        Assert.Equal(0, repayment.DeductibleAmountCents);
        Assert.Equal(500000, repayment.NonDeductibleAmountCents);

        var interest = result.Transactions[1];
        Assert.Equal(0, interest.DeductibleAmountCents);
        Assert.Equal(150000, interest.NonDeductibleAmountCents);
    }

    [Fact]
    public void Monthly_amortisation_groups_by_month()
    {
        var transactions = new List<ClassifiedTransaction>
        {
            new() { Transaction = TransactionBuilder.Repayment(300000, Date(2025, 1, 1)), Type = TransactionType.Repayment },
            new() { Transaction = TransactionBuilder.Interest(150000, Date(2025, 1, 20)), Type = TransactionType.InterestCharge },
            new() { Transaction = TransactionBuilder.Repayment(300000, Date(2025, 2, 1)), Type = TransactionType.Repayment },
            new() { Transaction = TransactionBuilder.Interest(148000, Date(2025, 2, 20)), Type = TransactionType.InterestCharge },
        };

        var result = _calculator.Calculate(50000000, transactions);

        Assert.Equal(2, result.MonthlyAmortisation.Count);
        Assert.Equal(1, result.MonthlyAmortisation[0].Month);
        Assert.Equal(2, result.MonthlyAmortisation[1].Month);
        Assert.Equal(300000, result.MonthlyAmortisation[0].RepaymentCents);
        Assert.Equal(150000, result.MonthlyAmortisation[0].InterestCents);
    }

    [Fact]
    public void FY_summary_boundaries_july_is_next_fy()
    {
        var transactions = new List<ClassifiedTransaction>
        {
            new() { Transaction = TransactionBuilder.Interest(150000, Date(2025, 6, 30)), Type = TransactionType.InterestCharge },
            new() { Transaction = TransactionBuilder.Interest(150000, Date(2025, 7, 1)), Type = TransactionType.InterestCharge },
        };

        var result = _calculator.Calculate(50000000, transactions);

        Assert.Equal(2, result.FinancialYearSummaries.Count);
        // June 2025 → FY 2024-25 (startYear=2024)
        Assert.Equal(2024, result.FinancialYearSummaries[0].StartYear);
        // July 2025 → FY 2025-26 (startYear=2025)
        Assert.Equal(2025, result.FinancialYearSummaries[1].StartYear);
    }

    [Fact]
    public void FY_deductible_interest_totals_match_transaction_splits()
    {
        var transactions = new List<ClassifiedTransaction>
        {
            new() { Transaction = TransactionBuilder.Redraw(5000000, Date(2024, 12, 1)), Type = TransactionType.InvestmentRedraw },
            new() { Transaction = TransactionBuilder.Interest(150000, Date(2025, 1, 20)), Type = TransactionType.InterestCharge },
            new() { Transaction = TransactionBuilder.Interest(150000, Date(2025, 3, 20)), Type = TransactionType.InterestCharge },
            new() { Transaction = TransactionBuilder.Interest(150000, Date(2025, 7, 20)), Type = TransactionType.InterestCharge },
        };

        var result = _calculator.Calculate(50000000, transactions);

        // FY 2024-25 has two interest charges (Jan and Mar)
        var fy2024 = result.FinancialYearSummaries.Single(f => f.StartYear == 2024);
        var fy2024Transactions = result.Transactions
            .Where(t => t.Type == TransactionType.InterestCharge)
            .Where(t =>
            {
                var d = t.Transaction.Attributes.CreatedAt.LocalDateTime;
                int fy = d.Month >= 7 ? d.Year : d.Year - 1;
                return fy == 2024;
            })
            .ToList();

        Assert.Equal(
            fy2024Transactions.Sum(t => t.DeductibleAmountCents),
            fy2024.DeductibleInterestCents);
        Assert.Equal(
            fy2024Transactions.Sum(t => t.NonDeductibleAmountCents),
            fy2024.NonDeductibleInterestCents);
    }

    [Fact]
    public void Worked_example_end_to_end()
    {
        // Loan: $500,000
        // 1. Jan 5: Investment redraw $50,000
        // 2. Feb 1: Repayment $3,000
        // 3. Feb 5: Interest $1,800
        // 4. Mar 1: Repayment $3,000
        // 5. Mar 5: Interest $1,795

        long initialBalance = 50000000; // $500,000

        var transactions = new List<ClassifiedTransaction>
        {
            new() { Transaction = TransactionBuilder.Redraw(5000000, Date(2025, 1, 5)), Type = TransactionType.InvestmentRedraw },
            new() { Transaction = TransactionBuilder.Repayment(300000, Date(2025, 2, 1)), Type = TransactionType.Repayment },
            new() { Transaction = TransactionBuilder.Interest(180000, Date(2025, 2, 5)), Type = TransactionType.InterestCharge },
            new() { Transaction = TransactionBuilder.Repayment(300000, Date(2025, 3, 1)), Type = TransactionType.Repayment },
            new() { Transaction = TransactionBuilder.Interest(179500, Date(2025, 3, 5)), Type = TransactionType.InterestCharge },
        };

        var result = _calculator.Calculate(initialBalance, transactions);

        // Step 1: Redraw - ded=5000000, nonDed=50000000, total=55000000
        var redraw = result.Transactions[0];
        Assert.Equal(5000000, redraw.DeductibleAmountCents);

        // Step 2: Repayment $3,000 — ratio=5000000/55000000≈9.0909%
        // dedPortion = Round(300000 * 5000000 / 55000000) = Round(27272.727...) = 27273
        var repay1 = result.Transactions[1];
        Assert.Equal(27273, repay1.DeductibleAmountCents);
        Assert.Equal(272727, repay1.NonDeductibleAmountCents);

        // After: ded=4972727, nonDed=49727273, total=54700000

        // Step 3: Interest $1,800 — ratio=4972727/54700000≈9.0908%
        // dedPortion = Round(180000 * 4972727 / 54700000) = Round(16363.5...) = 16364
        var interest1 = result.Transactions[2];
        Assert.Equal(16364, interest1.DeductibleAmountCents);
        Assert.Equal(163636, interest1.NonDeductibleAmountCents);

        // After: ded=4972727+16364=4989091, nonDed=49727273+163636=49890909, total=54880000

        // Step 4: Repayment $3,000 — ratio=4989091/54880000≈9.0909%
        // dedPortion = Round(300000 * 4989091 / 54880000) = Round(27272.7...) = 27273
        var repay2 = result.Transactions[3];
        Assert.Equal(27273, repay2.DeductibleAmountCents);
        Assert.Equal(272727, repay2.NonDeductibleAmountCents);

        // After: ded=4989091-27273=4961818, nonDed=49890909-272727=49618182, total=54580000

        // Step 5: Interest $1,795 — ratio=4961818/54580000≈9.0909%
        // dedPortion = Round(179500 * 4961818 / 54580000) = Round(16318.1...) = 16318
        var interest2 = result.Transactions[4];
        Assert.Equal(16318, interest2.DeductibleAmountCents);
        Assert.Equal(179500 - 16318, interest2.NonDeductibleAmountCents);

        // Verify FY summary: all in FY 2024-25 (Jan-Mar 2025)
        Assert.Single(result.FinancialYearSummaries);
        var fy = result.FinancialYearSummaries[0];
        Assert.Equal(2024, fy.StartYear);
        Assert.Equal(180000 + 179500, fy.TotalInterestCents);
        Assert.Equal(16364 + 16318, fy.DeductibleInterestCents);

        // Verify monthly amortisation
        Assert.Equal(3, result.MonthlyAmortisation.Count);
        Assert.Equal(2025, result.MonthlyAmortisation[0].Year);
        Assert.Equal(1, result.MonthlyAmortisation[0].Month);
    }
}
