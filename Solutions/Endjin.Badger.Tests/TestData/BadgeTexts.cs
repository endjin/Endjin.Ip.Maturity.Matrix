namespace Endjin.Badger.Tests.TestData;

/// <summary>
/// The texts badges show, as displayed (i.e., HTML-decoded).
/// </summary>
internal static class BadgeTexts
{
    /// <summary>
    /// Gets every rule name in the benchmark rule set (Solutions/Endjin.Imm.Benchmarks/Data/RuleSet.yaml),
    /// which also includes every rule in Solutions/Endjin.Imm.App/imm-default.yaml.
    /// </summary>
    public static IReadOnlyList<string> RuleNames { get; } =
    [
        "Shared Engineering Standards",
        "Coding Standards",
        "Executable Specifications",
        "Code Coverage",
        "Benchmarks",
        "Reference Documentation",
        "Design & Implementation Documentation",
        "How-to Documentation",
        "Date of Last IP Review",
        "Framework Version",
        "Associated Work Items",
        "Source Code Availability",
        "License",
        "Production Use",
        "Insights",
        "Packaging",
        "Deployment",
        "OpenChain",
    ];

    /// <summary>Gets the statuses of rule badges: every percentage from 0% to 100%.</summary>
    public static IReadOnlyList<string> Percentages { get; } = [.. Enumerable.Range(0, 101).Select(percent => $"{percent}%")];

    /// <summary>Gets representative statuses of the total badge: a score out of the maximum possible score.</summary>
    public static IReadOnlyList<string> Totals { get; } =
        ["0 / 0", "1 / 3", "3 / 46", "8 / 18", "22 / 46", "42 / 54", "46 / 46", "99 / 100", "121 / 300", "100 / 100"];

    /// <summary>Gets the subjects of the total badge and of error badges that cannot name the rule.</summary>
    public static IReadOnlyList<string> GenericSubjects { get; } = ["IMM", "IMM rule"];

    /// <summary>Gets the messages error badges show in place of a score (see the Host's Imm.cs).</summary>
    public static IReadOnlyList<string> ErrorMessages { get; } =
    [
        "imm.yaml not found",
        "rule set not found",
        "unavailable",
        "invalid imm.yaml",
        "unknown rule",
        "invalid rule id",
        "rule not found",
        "opted out",
    ];

    /// <summary>Gets every text above.</summary>
    public static IEnumerable<string> All => [.. RuleNames, .. Percentages, .. Totals, .. GenericSubjects, .. ErrorMessages];
}
