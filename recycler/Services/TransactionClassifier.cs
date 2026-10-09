using Microsoft.Extensions.Options;
using Recycler.Configuration;
using Recycler.Models.DebtRecycling;
using Recycler.Models.UpBank;

namespace Recycler.Services;

public class TransactionClassifier(IOptions<DebtRecyclingOptions> options)
{
    private readonly DebtRecyclingOptions _options = options.Value;

    public (List<ClassifiedTransaction> Transactions, List<string> Warnings) Classify(List<UpTransaction> transactions)
    {
        var classified = new List<ClassifiedTransaction>();
        var warnings = new List<string>();

        foreach (var tx in transactions)
        {
            var amountCents = tx.Attributes.Amount.ValueInBaseUnits;

            if (amountCents > 0)
            {
                classified.Add(new ClassifiedTransaction
                {
                    Transaction = tx,
                    Type = TransactionType.Repayment
                });
            }
            else if (amountCents < 0)
            {
                if (IsInterestCharge(tx.Attributes.Description))
                {
                    classified.Add(new ClassifiedTransaction
                    {
                        Transaction = tx,
                        Type = TransactionType.InterestCharge
                    });
                }
                else if (IsRedraw(tx.Attributes.Description))
                {
                    classified.Add(new ClassifiedTransaction
                    {
                        Transaction = tx,
                        Type = TransactionType.InvestmentRedraw
                    });
                }
                else
                {
                    classified.Add(new ClassifiedTransaction
                    {
                        Transaction = tx,
                        Type = TransactionType.OtherDebit
                    });
                }
            }
        }

        return (classified, warnings);
    }

    private bool IsInterestCharge(string description)
    {
        return _options.InterestDescriptionPatterns.Any(pattern =>
            description.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsRedraw(string description)
    {
        return _options.RedrawDescriptionPatterns.Any(pattern =>
            description.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}
