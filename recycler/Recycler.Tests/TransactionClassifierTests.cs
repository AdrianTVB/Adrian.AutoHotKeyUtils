using Microsoft.Extensions.Options;
using Recycler.Configuration;
using Recycler.Models.DebtRecycling;
using Recycler.Services;
using Recycler.Tests.Helpers;
using Xunit;

namespace Recycler.Tests;

public class TransactionClassifierTests
{
    private static TransactionClassifier CreateClassifier(
        List<string>? interestPatterns = null,
        List<string>? redrawPatterns = null)
    {
        var options = new DebtRecyclingOptions
        {
            InterestDescriptionPatterns = interestPatterns ?? ["Interest"],
            RedrawDescriptionPatterns = redrawPatterns ?? ["Redraw"]
        };
        return new TransactionClassifier(Options.Create(options));
    }

    [Fact]
    public void Positive_amount_is_classified_as_repayment()
    {
        var classifier = CreateClassifier();
        var tx = TransactionBuilder.Repayment(500000);

        var (classified, _) = classifier.Classify([tx]);

        Assert.Single(classified);
        Assert.Equal(TransactionType.Repayment, classified[0].Type);
    }

    [Fact]
    public void Negative_amount_with_interest_description_is_interest_charge()
    {
        var classifier = CreateClassifier();
        var tx = TransactionBuilder.Interest(150000, description: "Interest Charged");

        var (classified, _) = classifier.Classify([tx]);

        Assert.Single(classified);
        Assert.Equal(TransactionType.InterestCharge, classified[0].Type);
    }

    [Fact]
    public void Interest_match_is_case_insensitive()
    {
        var classifier = CreateClassifier();
        var tx = TransactionBuilder.Interest(150000, description: "interest charged on loan");

        var (classified, _) = classifier.Classify([tx]);

        Assert.Single(classified);
        Assert.Equal(TransactionType.InterestCharge, classified[0].Type);
    }

    [Fact]
    public void Custom_interest_patterns_are_used()
    {
        var classifier = CreateClassifier(interestPatterns: ["Interest", "Loan Fee"]);
        var txInterest = TransactionBuilder.Interest(150000, description: "Interest Charged");
        var txFee = TransactionBuilder.Fee(5000, description: "Loan Fee Applied");

        var (classified, _) = classifier.Classify([txInterest, txFee]);

        Assert.Equal(2, classified.Count);
        Assert.All(classified, ct => Assert.Equal(TransactionType.InterestCharge, ct.Type));
    }

    [Fact]
    public void Redraw_matched_by_description_pattern()
    {
        var classifier = CreateClassifier();
        var tx = TransactionBuilder.Redraw(5000000, description: "Redraw");

        var (classified, _) = classifier.Classify([tx]);

        Assert.Single(classified);
        Assert.Equal(TransactionType.InvestmentRedraw, classified[0].Type);
    }

    [Fact]
    public void Redraw_match_is_case_insensitive()
    {
        var classifier = CreateClassifier();
        var tx = TransactionBuilder.Redraw(5000000, description: "redraw from home loan");

        var (classified, _) = classifier.Classify([tx]);

        Assert.Single(classified);
        Assert.Equal(TransactionType.InvestmentRedraw, classified[0].Type);
    }

    [Fact]
    public void Custom_redraw_patterns_are_used()
    {
        var classifier = CreateClassifier(redrawPatterns: ["Redraw", "Investment Transfer"]);
        var txRedraw = TransactionBuilder.Redraw(5000000, description: "Redraw");
        var txTransfer = TransactionBuilder.Redraw(3000000, description: "Investment Transfer Out");

        var (classified, _) = classifier.Classify([txRedraw, txTransfer]);

        Assert.Equal(2, classified.Count);
        Assert.All(classified, ct => Assert.Equal(TransactionType.InvestmentRedraw, ct.Type));
    }

    [Fact]
    public void Negative_amount_not_matching_interest_or_redraw_is_other_debit()
    {
        var classifier = CreateClassifier();
        var tx = TransactionBuilder.Fee(10000, description: "Annual Fee");

        var (classified, _) = classifier.Classify([tx]);

        Assert.Single(classified);
        Assert.Equal(TransactionType.OtherDebit, classified[0].Type);
    }
}
