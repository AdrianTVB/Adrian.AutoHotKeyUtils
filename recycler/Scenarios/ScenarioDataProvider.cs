using Recycler.Models.UpBank;

namespace Recycler.Scenarios;

public record ScenarioData(
    string Name,
    string Description,
    List<UpTransaction> Transactions);

public static class ScenarioDataProvider
{
    private static int _counter;
    private static readonly TimeSpan Aest = TimeSpan.FromHours(11);

    public static ScenarioData GetScenario(string key) => key.ToUpperInvariant() switch
    {
        "A" => BuildScenarioA(),
        "B" => BuildScenarioB(),
        "C" => BuildScenarioC(),
        _ => throw new ArgumentException($"Unknown scenario: '{key}'. Valid options are A, B, C.")
    };

    private static ScenarioData BuildScenarioA()
    {
        var transactions = BuildCoreTransactions();

        // $200k lump sum deposit + investment redraw on Feb 15, 2026
        transactions.Add(Tx(20000000, "Lump Sum Deposit", D(2026, 2, 15)));
        transactions.Add(Tx(-20000000, "Redraw", D(2026, 2, 15)));

        // Extrapolate May–Dec 2026 (8 months)
        long balance = transactions.Sum(t => (long)t.Attributes.Amount.ValueInBaseUnits);
        transactions.AddRange(ExtrapolateMonthly(balance, 272600, 0.025m, 2026, 5, 8));

        return new ScenarioData(
            "Single $200k Redraw",
            "Lump sum $200k deposit + redraw (Feb 2026) — core data + extrapolated May–Dec 2026",
            transactions);
    }

    private static ScenarioData BuildScenarioB()
    {
        var transactions = new List<UpTransaction>
        {
            // Jul 1, 2024: Full $100k loan drawdown
            Tx(-10000000, "Loan Drawdown", D(2024, 7, 1)),
            // Jul 1, 2024: Lump sum $50k deposit (funds the recycling)
            Tx(5000000, "Lump Sum Deposit", D(2024, 7, 1)),
            // Jul 1, 2024: Investment redraw $50k (recycled half)
            Tx(-5000000, "Redraw", D(2024, 7, 1)),
        };

        // 36 months (Aug 2024 – Jul 2027): $1,000 repayment on 1st, interest on 15th at 10% annual
        long balance = -10000000;
        var baseDate = new DateTime(2024, 8, 1);
        for (int i = 0; i < 36; i++)
        {
            var d = baseDate.AddMonths(i);

            // Repayment on 1st
            transactions.Add(Tx(100000, "Loan Repayment",
                new DateTimeOffset(d.Year, d.Month, 1, 0, 0, 0, Aest)));
            balance += 100000;

            // Interest on 15th
            int interestCents = (int)Math.Round(Math.Abs(balance) * 0.10m / 12);
            transactions.Add(Tx(-interestCents, "Interest Charged",
                new DateTimeOffset(d.Year, d.Month, 15, 0, 0, 0, Aest)));
            balance -= interestCents;
        }

        return new ScenarioData(
            "Simple 50/50 Split",
            "50/50 split ($100k loan, lump sum $50k recycled, 3 years, 10% annual rate)",
            transactions);
    }

    private static ScenarioData BuildScenarioC()
    {
        var transactions = BuildCoreTransactions();

        // Redraw 1 (FY 2024-25): Lump sum $100k deposit + redraw
        transactions.Add(Tx(10000000, "Lump Sum Deposit", D(2025, 5, 15)));
        transactions.Add(Tx(-10000000, "Redraw", D(2025, 5, 15)));

        // Redraw 2 (FY 2025-26): Built-up redraw $80k (from accumulated overpayments, no deposit)
        transactions.Add(Tx(-8000000, "Redraw", D(2025, 10, 15)));

        // Redraw 3 (FY 2025-26): Lump sum $50k deposit + redraw
        transactions.Add(Tx(5000000, "Lump Sum Deposit", D(2026, 2, 15)));
        transactions.Add(Tx(-5000000, "Redraw", D(2026, 2, 15)));

        // Extrapolate May–Dec 2026 (8 months)
        long balance = transactions.Sum(t => (long)t.Attributes.Amount.ValueInBaseUnits);
        transactions.AddRange(ExtrapolateMonthly(balance, 272600, 0.025m, 2026, 5, 8));

        return new ScenarioData(
            "Three Redraws Across FYs",
            "Three redraws across FYs (2× lump sum, 1× built-up) — core data + extrapolated May–Dec 2026",
            transactions);
    }

    private static List<UpTransaction> BuildCoreTransactions()
    {
        return
        [
            // Drawdowns (Mar 20, 2025)
            Tx(-30000000, "Loan Drawdown", D(2025, 3, 20)),
            Tx(-15000000, "Loan Drawdown", D(2025, 3, 20)),
            Tx(-1400000, "Loan Drawdown", D(2025, 3, 20)),

            // Initial repayment (Mar 20, 2025)
            Tx(4400000, "Loan Repayment", D(2025, 3, 20)),

            // Monthly repayments (19th of each month, Apr 2025 – Apr 2026)
            Tx(282400, "Loan Repayment", D(2025, 4, 19)),
            Tx(272600, "Loan Repayment", D(2025, 5, 19)),
            Tx(272600, "Loan Repayment", D(2025, 6, 19)),
            Tx(272600, "Loan Repayment", D(2025, 7, 19)),
            Tx(272600, "Loan Repayment", D(2025, 8, 19)),
            Tx(272600, "Loan Repayment", D(2025, 9, 19)),
            Tx(272600, "Loan Repayment", D(2025, 10, 19)),
            Tx(272600, "Loan Repayment", D(2025, 11, 19)),
            Tx(272600, "Loan Repayment", D(2025, 12, 19)),
            Tx(272600, "Loan Repayment", D(2026, 1, 19)),
            Tx(272600, "Loan Repayment", D(2026, 2, 19)),
            Tx(272600, "Loan Repayment", D(2026, 3, 19)),
            Tx(266400, "Loan Repayment", D(2026, 4, 19)),

            // Monthly interest charges (20th of each month, Apr 2025 – Apr 2026)
            Tx(-130700, "Interest Charged", D(2025, 4, 20)),
            Tx(-112100, "Interest Charged", D(2025, 5, 20)),
            Tx(-108300, "Interest Charged", D(2025, 6, 20)),
            Tx(-105700, "Interest Charged", D(2025, 7, 20)),
            Tx(-103100, "Interest Charged", D(2025, 8, 20)),
            Tx(-100600, "Interest Charged", D(2025, 9, 20)),
            Tx(-98200, "Interest Charged", D(2025, 10, 20)),
            Tx(-95800, "Interest Charged", D(2025, 11, 20)),
            Tx(-93500, "Interest Charged", D(2025, 12, 20)),
            Tx(-91200, "Interest Charged", D(2026, 1, 20)),
            Tx(-89000, "Interest Charged", D(2026, 2, 20)),
            Tx(-84500, "Interest Charged", D(2026, 3, 20)),
            Tx(-78900, "Interest Charged", D(2026, 4, 20)),

            // Unusual positive interest adjustment
            Tx(3, "Interest Charged", D(2025, 6, 20)),
        ];
    }

    private static List<UpTransaction> ExtrapolateMonthly(
        long runningBalanceCents,
        int repaymentCents,
        decimal annualRate,
        int startYear,
        int startMonth,
        int monthCount)
    {
        var transactions = new List<UpTransaction>();
        var balance = runningBalanceCents;
        var baseDate = new DateTime(startYear, startMonth, 1);

        for (int i = 0; i < monthCount; i++)
        {
            var d = baseDate.AddMonths(i);

            // Repayment on 19th
            transactions.Add(Tx(repaymentCents, "Loan Repayment",
                new DateTimeOffset(d.Year, d.Month, 19, 0, 0, 0, Aest)));
            balance += repaymentCents;

            // Interest on 20th
            int interestCents = (int)Math.Round(Math.Abs(balance) * annualRate / 12);
            transactions.Add(Tx(-interestCents, "Interest Charged",
                new DateTimeOffset(d.Year, d.Month, 20, 0, 0, 0, Aest)));
            balance -= interestCents;
        }

        return transactions;
    }

    private static DateTimeOffset D(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, Aest);

    private static UpTransaction Tx(int amountCents, string description, DateTimeOffset createdAt) =>
        new()
        {
            Id = $"scenario-{Interlocked.Increment(ref _counter)}",
            Attributes = new UpTransactionAttributes
            {
                Description = description,
                Amount = new MoneyObject
                {
                    CurrencyCode = "AUD",
                    Value = $"{amountCents / 100m:F2}",
                    ValueInBaseUnits = amountCents
                },
                Status = "SETTLED",
                CreatedAt = createdAt
            }
        };
}
