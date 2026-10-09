namespace Recycler.Configuration;

public class DebtRecyclingOptions
{
    public const string SectionName = "DebtRecycling";

    public List<string> InterestDescriptionPatterns { get; set; } = ["Interest"];
    public List<string> RedrawDescriptionPatterns { get; set; } = ["Redraw"];
}
