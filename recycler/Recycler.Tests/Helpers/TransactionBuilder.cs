using Recycler.Models.UpBank;

namespace Recycler.Tests.Helpers;

public static class TransactionBuilder
{
    private static int _counter;

    public static UpTransaction Create(
        int amountCents,
        string description = "Transaction",
        DateTimeOffset? createdAt = null,
        string status = "SETTLED",
        string? id = null)
    {
        return new UpTransaction
        {
            Id = id ?? $"tx-{Interlocked.Increment(ref _counter)}",
            Attributes = new UpTransactionAttributes
            {
                Description = description,
                Amount = new MoneyObject
                {
                    CurrencyCode = "AUD",
                    Value = $"{amountCents / 100m:F2}",
                    ValueInBaseUnits = amountCents
                },
                Status = status,
                CreatedAt = createdAt ?? new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.FromHours(11))
            }
        };
    }

    public static UpTransaction Repayment(int amountCents, DateTimeOffset? createdAt = null, string description = "Loan Repayment")
        => Create(amountCents, description, createdAt);

    public static UpTransaction Interest(int amountCents, DateTimeOffset? createdAt = null, string description = "Interest Charged")
        => Create(-Math.Abs(amountCents), description, createdAt);

    public static UpTransaction Redraw(int amountCents, DateTimeOffset? createdAt = null, string description = "Redraw")
        => Create(-Math.Abs(amountCents), description, createdAt);

    public static UpTransaction Fee(int amountCents, DateTimeOffset? createdAt = null, string description = "Loan Fee")
        => Create(-Math.Abs(amountCents), description, createdAt);
}
