namespace Endjin.Imm.Tests.TestData;

/// <summary>
/// Rule set definitions shared by the evaluation tests, in the same YAML form as the published
/// rule set that <see cref="Endjin.Imm.Domain.IpMaturityMatrixRuleset.FromYaml(string)"/> loads.
/// </summary>
internal static class RuleSetYaml
{
    public const string DateOfLastIpReviewRuleId = "da4ed776-0365-4d8a-a297-c4e91a14d646";

    public const string FrameworkVersionRuleId = "6c0402b3-f0e3-4bd7-83fe-04bb6dca7924";

    public const string DeploymentRuleId = "edea4593-d2dd-485b-bc1b-aaaf18f098f9";

    public const string SharedEngineeringStandardsRuleId = "74e29f9b-6dca-4161-8fdd-b468a1eb185d";

    /// <summary>
    /// An Age rule whose score drops as the date in the IMM gets older.
    /// </summary>
    public const string DateOfLastIpReview = $"""
        -
            Name: 'Date of Last IP Review'
            Id: '{DateOfLastIpReviewRuleId}'
            DataType: Age
            Measures:
                -
                    Score: 3
                    Age: <P1M
                    Description: '< 1 month'
                -
                    Score: 2
                    Age: <P3M
                    Description: '> 1 month'
                -
                    Score: 1
                    Age: '*'
                    Description: '> 3 months'
                -
                    Score: 0
                    Description: None
        """;

    /// <summary>
    /// A Framework rule that scores the target framework named in the IMM.
    /// </summary>
    public const string FrameworkVersion = $"""
        -
            Name: 'Framework Version'
            Id: '{FrameworkVersionRuleId}'
            DataType: Framework
            Measures:
                -
                    Score: 3
                    Framework: netcoreapp6.0
                    Description: 'Using the most current LTS version'
                -
                    Score: 2
                    Framework: netcoreapp3.1
                    Description: 'Using a LTS version'
                -
                    Score: 2
                    Framework: netcoreapp2.1
                    Description: 'Using a LTS version'
                -
                    Score: 3
                    Framework: node14
                    Description: 'Using the most current LTS version'
                -
                    Score: 3
                    Framework: node12
                    Description: 'Using the most current LTS version'
                -
                    Score: 2
                    Framework: node10
                    Description: 'Using a LTS version'
                -
                    Score: 2
                    Framework: node8
                    Description: 'Using a LTS version'
                -
                    Score: 1
                    Framework: '*'
                    Description: 'Using an unsupported version'
                -
                    Score: 0
                    Description: None
        """;

    /// <summary>
    /// A Continuous Deployment rule in which the 'Multi-tenanted - as a Service' measure can be
    /// opted out of, plus a Discrete Shared Engineering Standards rule.
    /// </summary>
    public const string DeploymentWithOptionalMeasure = $"""
        -
            Name: 'Deployment'
            Id: '{DeploymentRuleId}'
            DataType: Continuous
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
                    Score: 1
                    Description: 'Multi-tenanted - as a Service'
                    CanOptOut: true
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
    /// A Continuous Deployment rule that can be opted out of as a whole, plus a Discrete Shared
    /// Engineering Standards rule.
    /// </summary>
    public const string OptionalDeploymentRule = $"""
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
                    Score: 1
                    Description: 'Multi-tenanted - as a Service'
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
}
