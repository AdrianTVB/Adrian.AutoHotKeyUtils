using Recycler.Configuration;
using Recycler.Models.UpBank;

namespace Recycler.Models.DebtRecycling;

public record AnalysisInput(
    List<UpTransaction> Transactions,
    long InitialBalanceCents,
    DebtRecyclingOptions Options);
