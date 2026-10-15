namespace Endjin.Ip.Maturity.Matrix.Host.Tests.TestData;

/// <summary>
/// Rule sets and IMMs, in the YAML form published on GitHub, for exercising the badge functions.
/// </summary>
internal static class HostYaml
{
    public const string ScoredRuleId = "8c7d3a1e-0f4b-4b55-9d0e-6f2a7c1b9e31";

    public const string DeploymentRuleId = "edea4593-d2dd-485b-bc1b-aaaf18f098f9";

    public const string SharedEngineeringStandardsRuleId = "74e29f9b-6dca-4161-8fdd-b468a1eb185d";

    /// <summary>
    /// A Continuous rule worth 100 points in total, of which the IMM scores
    /// <paramref name="percentage"/>, so that both the rule's percentage and the IMM's total score
    /// as a percentage are exactly <paramref name="percentage"/>.
    /// </summary>
    /// <param name="percentage">The percentage to score, from 0 to 100.</param>
    /// <param name="ruleName">The rule's name.</param>
    /// <returns>The rule set and IMM YAML.</returns>
    public static (string RuleSetYaml, string ImmYaml) Scoring(int percentage, string ruleName = "Scored Rule") => (
        $"""
        -
            Name: '{ruleName}'
            Id: '{ScoredRuleId}'
            DataType: Continuous
            Measures:
                -
                    Score: {percentage}
                    Description: Achieved
                -
                    Score: {100 - percentage}
                    Description: Remaining
        """,
        $"""
        -
            Name: '{ruleName}'
            Id: '{ScoredRuleId}'
            Measures:
                -
                    Score: {percentage}
                    Description: Achieved
        """);

    /// <summary>
    /// A rule set with a Continuous Deployment rule (which can be opted out of) and a Discrete
    /// Shared Engineering Standards rule.
    /// </summary>
    public const string TwoRuleSet = $"""
        -
            Name: 'Deployment'
            Id: '{DeploymentRuleId}'
            DataType: Continuous
            CanOptOut: true
            Measures:
                -
                    Score: 0
                    Description: None
                -
                    Score: 1
                    Description: 'Scripted and Documented'
                -
                    Score: 1
                    Description: Templated
        -
            Name: 'Shared Engineering Standards'
            Id: '{SharedEngineeringStandardsRuleId}'
            DataType: Discrete
            Measures:
                -
                    Score: 0
                    Description: None
                -
                    Score: 1
                    Description: Configured
        """;

    /// <summary>
    /// An IMM that scores 2 of 2 for Deployment and 1 of 1 for Shared Engineering Standards.
    /// </summary>
    public const string BothRulesScored = $"""
        -
            Name: 'Deployment'
            Id: '{DeploymentRuleId}'
            Measures:
                -
                    Score: 1
                    Description: 'Scripted and Documented'
                -
                    Score: 1
                    Description: Templated
        -
            Name: 'Shared Engineering Standards'
            Id: '{SharedEngineeringStandardsRuleId}'
            Measures:
                -
                    Score: 1
                    Description: Configured
        """;

    /// <summary>
    /// An IMM that opts out of Deployment and scores 1 of 1 for Shared Engineering Standards.
    /// </summary>
    public const string DeploymentOptedOut = $"""
        -
            Name: 'Deployment'
            Id: '{DeploymentRuleId}'
            OptOut: true
        -
            Name: 'Shared Engineering Standards'
            Id: '{SharedEngineeringStandardsRuleId}'
            Measures:
                -
                    Score: 1
                    Description: Configured
        """;

    /// <summary>
    /// A rule set whose only rule can be opted out of.
    /// </summary>
    public const string OptionalRuleOnly = $"""
        -
            Name: 'Deployment'
            Id: '{DeploymentRuleId}'
            DataType: Continuous
            CanOptOut: true
            Measures:
                -
                    Score: 0
                    Description: None
                -
                    Score: 1
                    Description: 'Scripted and Documented'
        """;

    /// <summary>
    /// An IMM that opts out of the only rule in <see cref="OptionalRuleOnly"/>.
    /// </summary>
    public const string OptionalRuleOptedOut = $"""
        -
            Name: 'Deployment'
            Id: '{DeploymentRuleId}'
            OptOut: true
        """;
}
