namespace Endjin.Imm.Tests.TestData;

/// <summary>
/// Project IMM documents shared by the tests, in the same YAML form as a project's
/// <c>imm.yaml</c> that <see cref="Endjin.Imm.Domain.IpMaturityModel.FromYaml(string)"/> loads.
/// </summary>
internal static class ImmYaml
{
    /// <summary>
    /// An IMM that scores 1 for Shared Engineering Standards (see
    /// <see cref="RuleSetYaml.DeploymentWithOptionalMeasure"/>).
    /// </summary>
    public const string SharedEngineeringStandardsConfigured = $"""
        -
            Name: 'Shared Engineering Standards'
            Id: '{RuleSetYaml.SharedEngineeringStandardsRuleId}'
            Measures:
                -
                    Score: 1
                    Description: Configured
        """;

    /// <summary>
    /// An IMM that scores 0 for Shared Engineering Standards (see
    /// <see cref="RuleSetYaml.DeploymentWithOptionalMeasure"/>).
    /// </summary>
    public const string SharedEngineeringStandardsNone = $"""
        -
            Name: 'Shared Engineering Standards'
            Id: '{RuleSetYaml.SharedEngineeringStandardsRuleId}'
            Measures:
                -
                    Score: 0
                    Description: None
        """;
}
