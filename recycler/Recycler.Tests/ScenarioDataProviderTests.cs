using Recycler.Scenarios;
using Xunit;

namespace Recycler.Tests;

public class ScenarioDataProviderTests
{
    [Theory]
    [InlineData("A", 49)]
    [InlineData("B", 75)]
    [InlineData("C", 52)]
    public void Scenario_has_expected_transaction_count(string key, int expectedCount)
    {
        var scenario = ScenarioDataProvider.GetScenario(key);
        Assert.Equal(expectedCount, scenario.Transactions.Count);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("B")]
    [InlineData("C")]
    public void All_transaction_ids_are_unique(string key)
    {
        var scenario = ScenarioDataProvider.GetScenario(key);
        var ids = scenario.Transactions.Select(t => t.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Theory]
    [InlineData("A")]
    [InlineData("B")]
    [InlineData("C")]
    public void All_transactions_have_valid_structure(string key)
    {
        var scenario = ScenarioDataProvider.GetScenario(key);
        foreach (var tx in scenario.Transactions)
        {
            Assert.False(string.IsNullOrEmpty(tx.Id));
            Assert.False(string.IsNullOrEmpty(tx.Attributes.Description));
            Assert.Equal("AUD", tx.Attributes.Amount.CurrencyCode);
            Assert.Equal("SETTLED", tx.Attributes.Status);
        }
    }

    [Theory]
    [InlineData("A")]
    [InlineData("B")]
    [InlineData("C")]
    public void Scenario_name_and_description_are_set(string key)
    {
        var scenario = ScenarioDataProvider.GetScenario(key);
        Assert.False(string.IsNullOrEmpty(scenario.Name));
        Assert.False(string.IsNullOrEmpty(scenario.Description));
    }

    [Fact]
    public void Unknown_scenario_throws()
    {
        Assert.Throws<ArgumentException>(() => ScenarioDataProvider.GetScenario("X"));
    }

    [Fact]
    public void Case_insensitive_lookup()
    {
        var lower = ScenarioDataProvider.GetScenario("a");
        var upper = ScenarioDataProvider.GetScenario("A");
        Assert.Equal(lower.Name, upper.Name);
    }

    [Fact]
    public void ScenarioB_balance_shrinks_over_time()
    {
        // Repayment ($1,000/mo) exceeds interest (~$833/mo on $100k at 10%), so balance should shrink
        var scenario = ScenarioDataProvider.GetScenario("B");
        long balance = scenario.Transactions.Sum(t => (long)t.Attributes.Amount.ValueInBaseUnits);
        Assert.True(balance > -10000000,
            $"Expected balance less negative than -$100,000 but was {balance / 100m:F2}");
    }

    [Fact]
    public void Core_transactions_sum_to_expected_balance()
    {
        // Scenario A = core (31 txns) + 1 redraw + 16 extrapolated
        // Core balance: drawdowns(-$464k) + initial repayment(+$44k) + monthly repayments - monthly interest + $0.03
        var scenario = ScenarioDataProvider.GetScenario("A");

        // Core data is the first 31 items in the list (added by BuildCoreTransactions before redraws/extrapolation)
        var coreTransactions = scenario.Transactions.Take(31).ToList();

        long coreBalance = coreTransactions.Sum(t => (long)t.Attributes.Amount.ValueInBaseUnits);

        // Expected: -$397,441.97 (approximately -39744197 cents)
        Assert.InRange(coreBalance, -39800000, -39700000);
    }
}
