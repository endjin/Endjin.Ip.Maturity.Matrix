using Endjin.Imm.Domain;
using Endjin.Imm.Tests.TestData;
using Shouldly;

namespace Endjin.Imm.Tests;

/// <summary>
/// Measures that do not apply to a project can be excluded from its IP Maturity Matrix without
/// distorting its scores. The 'Multi-tenanted - as a Service' measure of the Deployment rule can
/// be opted out of.
/// </summary>
[TestClass]
public class OptionalMeasureTests
{
    [TestMethod]
    public void Evaluate_AllMeasuresPresentWithFullMarks_ScoresFullMarks()
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
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(RuleSetYaml.DeploymentWithOptionalMeasure, immYaml);

        // Assert
        RuleEvaluation deployment = evaluation.ForRule(RuleSetYaml.DeploymentRuleId);
        deployment.Score.ShouldBe(3);
        deployment.Percentage.ShouldBe(100);
        RuleEvaluation sharedEngineeringStandards = evaluation.ForRule(RuleSetYaml.SharedEngineeringStandardsRuleId);
        sharedEngineeringStandards.Score.ShouldBe(1);
        sharedEngineeringStandards.Percentage.ShouldBe(100);
        evaluation.TotalScore.ShouldBe(4);
        evaluation.MaximumPossibleTotalScore.ShouldBe(4);
    }

    // Templated cannot be opted out of, so the opt-out is ignored for the maximum: the rule's percentage and the
    // total's maximum must agree that it still counts.
    [TestMethod]
    public void Evaluate_OptOutOfAMeasureThatCannotBeOptedOutOf_StillCountsTowardsTheRuleAndTotalMaximum()
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
                        Description: Templated
                        OptOut: true
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
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(RuleSetYaml.DeploymentWithOptionalMeasure, immYaml);

        // Assert
        RuleEvaluation deployment = evaluation.ForRule(RuleSetYaml.DeploymentRuleId);
        deployment.Score.ShouldBe(2);
        deployment.MaximumScore.ShouldBe(3);
        deployment.Percentage.ShouldBe(67);
        evaluation.TotalScore.ShouldBe(3);
        evaluation.MaximumPossibleTotalScore.ShouldBe(4);
    }

    [TestMethod]
    public void Evaluate_OptionalMeasureNotScored_CountsAgainstThePercentageAndMaximum()
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
                Name: 'Shared Engineering Standards'
                Id: '{RuleSetYaml.SharedEngineeringStandardsRuleId}'
                Measures:
                    -
                        Score: 1
                        Description: Configured
            """;

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(RuleSetYaml.DeploymentWithOptionalMeasure, immYaml);

        // Assert
        RuleEvaluation deployment = evaluation.ForRule(RuleSetYaml.DeploymentRuleId);
        deployment.Score.ShouldBe(2);
        deployment.Percentage.ShouldBe(67);
        RuleEvaluation sharedEngineeringStandards = evaluation.ForRule(RuleSetYaml.SharedEngineeringStandardsRuleId);
        sharedEngineeringStandards.Score.ShouldBe(1);
        sharedEngineeringStandards.Percentage.ShouldBe(100);
        evaluation.TotalScore.ShouldBe(3);
        evaluation.MaximumPossibleTotalScore.ShouldBe(4);
    }

    [TestMethod]
    public void Evaluate_OptionalMeasureOptedOutAndRemainingMeasuresScored_ScoresFullPercentageWithReducedMaximum()
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
                        Description: 'Multi-tenanted - as a Service'
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
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(RuleSetYaml.DeploymentWithOptionalMeasure, immYaml);

        // Assert
        RuleEvaluation deployment = evaluation.ForRule(RuleSetYaml.DeploymentRuleId);
        deployment.Score.ShouldBe(2);
        deployment.Percentage.ShouldBe(100);
        RuleEvaluation sharedEngineeringStandards = evaluation.ForRule(RuleSetYaml.SharedEngineeringStandardsRuleId);
        sharedEngineeringStandards.Score.ShouldBe(1);
        sharedEngineeringStandards.Percentage.ShouldBe(100);
        evaluation.TotalScore.ShouldBe(3);
        evaluation.MaximumPossibleTotalScore.ShouldBe(3);
    }

    [TestMethod]
    public void Evaluate_NoOptOutAndSomeMeasuresMissing_ScoresAgainstFullMaximum()
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
                Name: 'Shared Engineering Standards'
                Id: '{RuleSetYaml.SharedEngineeringStandardsRuleId}'
                Measures:
                    -
                        Score: 1
                        Description: Configured
            """;

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(RuleSetYaml.DeploymentWithOptionalMeasure, immYaml);

        // Assert
        RuleEvaluation deployment = evaluation.ForRule(RuleSetYaml.DeploymentRuleId);
        deployment.Score.ShouldBe(1);
        deployment.Percentage.ShouldBe(33);
        RuleEvaluation sharedEngineeringStandards = evaluation.ForRule(RuleSetYaml.SharedEngineeringStandardsRuleId);
        sharedEngineeringStandards.Score.ShouldBe(1);
        sharedEngineeringStandards.Percentage.ShouldBe(100);
        evaluation.TotalScore.ShouldBe(2);
        evaluation.MaximumPossibleTotalScore.ShouldBe(4);
    }

    [TestMethod]
    public void Evaluate_OptionalMeasureOptedOutAndSomeMeasuresMissing_ScoresAgainstReducedMaximum()
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
                        Description: 'Multi-tenanted - as a Service'
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
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(RuleSetYaml.DeploymentWithOptionalMeasure, immYaml);

        // Assert
        RuleEvaluation deployment = evaluation.ForRule(RuleSetYaml.DeploymentRuleId);
        deployment.Score.ShouldBe(1);
        deployment.Percentage.ShouldBe(50);
        RuleEvaluation sharedEngineeringStandards = evaluation.ForRule(RuleSetYaml.SharedEngineeringStandardsRuleId);
        sharedEngineeringStandards.Score.ShouldBe(1);
        sharedEngineeringStandards.Percentage.ShouldBe(100);
        evaluation.TotalScore.ShouldBe(2);
        evaluation.MaximumPossibleTotalScore.ShouldBe(3);
    }

    [TestMethod]
    public void Evaluate_NoOptOutAndNoPoints_ScoresZeroAgainstFullMaximum()
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
                        Score: 0
                        Description: None
            """;

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(RuleSetYaml.DeploymentWithOptionalMeasure, immYaml);

        // Assert
        RuleEvaluation deployment = evaluation.ForRule(RuleSetYaml.DeploymentRuleId);
        deployment.Score.ShouldBe(0);
        deployment.Percentage.ShouldBe(0);
        RuleEvaluation sharedEngineeringStandards = evaluation.ForRule(RuleSetYaml.SharedEngineeringStandardsRuleId);
        sharedEngineeringStandards.Score.ShouldBe(0);
        sharedEngineeringStandards.Percentage.ShouldBe(0);
        evaluation.TotalScore.ShouldBe(0);
        evaluation.MaximumPossibleTotalScore.ShouldBe(4);
    }

    [TestMethod]
    public void Evaluate_OptionalMeasureOptedOutAndNoPoints_ScoresZeroAgainstReducedMaximum()
    {
        // Arrange
        const string immYaml = $"""
            -
                Name: 'Deployment'
                Id: '{RuleSetYaml.DeploymentRuleId}'
                Measures:
                    -
                        Description: 'Multi-tenanted - as a Service'
                        OptOut: true
            -
                Name: 'Shared Engineering Standards'
                Id: '{RuleSetYaml.SharedEngineeringStandardsRuleId}'
                Measures:
                    -
                        Score: 0
                        Description: None
            """;

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(RuleSetYaml.DeploymentWithOptionalMeasure, immYaml);

        // Assert
        RuleEvaluation deployment = evaluation.ForRule(RuleSetYaml.DeploymentRuleId);
        deployment.Score.ShouldBe(0);
        deployment.Percentage.ShouldBe(0);
        RuleEvaluation sharedEngineeringStandards = evaluation.ForRule(RuleSetYaml.SharedEngineeringStandardsRuleId);
        sharedEngineeringStandards.Score.ShouldBe(0);
        sharedEngineeringStandards.Percentage.ShouldBe(0);
        evaluation.TotalScore.ShouldBe(0);
        evaluation.MaximumPossibleTotalScore.ShouldBe(3);
    }

    // TODO: Check can't opt out when opt out not acceptable!
    // Implement as part of https://github.com/endjin/Endjin.Ip.Maturity.Model/issues/9
}
