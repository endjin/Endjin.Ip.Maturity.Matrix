using Endjin.Imm.Domain;
using Endjin.Imm.Tests.TestData;
using Shouldly;

namespace Endjin.Imm.Tests;

/// <summary>
/// Rules that do not apply to a project can be excluded from its IP Maturity Model without
/// distorting its scores. The Deployment rule can be opted out of.
/// </summary>
[TestClass]
public class OptionalRuleTests
{
    [TestMethod]
    public void Evaluate_AllRulesPresentWithFullMarks_ScoresFullMarks()
    {
        // Arrange
        const string immYaml = $"""
            -
                Name: 'Deployment'
                Id: '{RuleSetYaml.DeploymentRuleId}'
                Measures:
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
                Id: '{RuleSetYaml.SharedEngineeringStandardsRuleId}'
                Measures:
                    -
                        Score: 1
                        Description: Configured
            """;

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(RuleSetYaml.OptionalDeploymentRule, immYaml);

        // Assert
        evaluation.TotalScore.ShouldBe(4);
        evaluation.MaximumPossibleTotalScore.ShouldBe(4);
    }

    [TestMethod]
    public void Evaluate_OptionalRuleScoresNothing_CountsAgainstTheMaximum()
    {
        // Arrange
        const string immYaml = $"""
            -
                Name: 'Deployment'
                Id: '{RuleSetYaml.DeploymentRuleId}'
                Measures:
                    -
                        Score: 0
                        Description: None
            -
                Name: 'Shared Engineering Standards'
                Id: '{RuleSetYaml.SharedEngineeringStandardsRuleId}'
                Measures:
                    -
                        Score: 1
                        Description: Configured
            """;

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(RuleSetYaml.OptionalDeploymentRule, immYaml);

        // Assert
        evaluation.TotalScore.ShouldBe(1);
        evaluation.MaximumPossibleTotalScore.ShouldBe(4);
    }

    [TestMethod]
    public void Evaluate_OptionalRuleOptedOut_ExcludesTheRuleFromTheMaximum()
    {
        // Arrange
        const string immYaml = $"""
            -
                Name: 'Deployment'
                Id: '{RuleSetYaml.DeploymentRuleId}'
                OptOut: true
            -
                Name: 'Shared Engineering Standards'
                Id: '{RuleSetYaml.SharedEngineeringStandardsRuleId}'
                Measures:
                    -
                        Score: 1
                        Description: Configured
            """;

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(RuleSetYaml.OptionalDeploymentRule, immYaml);

        // Assert
        evaluation.TotalScore.ShouldBe(1);
        evaluation.MaximumPossibleTotalScore.ShouldBe(1);
    }
}
