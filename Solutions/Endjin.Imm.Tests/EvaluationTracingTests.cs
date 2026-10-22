using System.Diagnostics;
using Endjin.Imm.Domain;
using Endjin.Imm.Processing;
using Endjin.Imm.Tests.TestData;
using Shouldly;

namespace Endjin.Imm.Tests;

/// <summary>
/// Evaluating an IMM is traced as an <c>imm.evaluate</c> span recording how many rules were
/// evaluated (rules the project has opted out of are not), the score, and the maximum possible
/// score, so that the time a badge request spends scoring can be seen alongside the scores. An
/// IMM that cannot be evaluated marks the span as an error naming the exception's type.
/// </summary>
[TestClass]
public class EvaluationTracingTests
{
    [TestMethod]
    public void Evaluate_ImmScoringEveryRule_TracesTheRuleCountScoreAndMaximumScore()
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
        using ActivityCapture capture = new(ImmTelemetry.ActivitySourceName);

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(RuleSetYaml.OptionalDeploymentRule, immYaml);

        // Assert
        Activity evaluate = capture.Activities.ShouldHaveSingleItem();
        evaluate.OperationName.ShouldBe("imm.evaluate");
        evaluate.Kind.ShouldBe(ActivityKind.Internal);
        evaluate.ParentSpanId.ShouldBe(capture.Root.SpanId);
        evaluate.GetTagItem("imm.rule_count").ShouldBe(2);
        evaluate.GetTagItem("imm.score").ShouldBe(4L);
        evaluate.GetTagItem("imm.max_score").ShouldBe(4L);
        evaluate.GetTagItem("imm.score").ShouldBe(evaluation.TotalScore);
        evaluate.GetTagItem("imm.max_score").ShouldBe(evaluation.MaximumPossibleTotalScore);
        evaluate.Status.ShouldBe(ActivityStatusCode.Unset);
    }

    [TestMethod]
    public void Evaluate_RuleOptedOut_TracesOnlyTheRulesEvaluated()
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
        using ActivityCapture capture = new(ImmTelemetry.ActivitySourceName);

        // Act
        ImmEvaluator.Evaluate(RuleSetYaml.OptionalDeploymentRule, immYaml);

        // Assert
        Activity evaluate = capture.Single("imm.evaluate");
        evaluate.GetTagItem("imm.rule_count").ShouldBe(1);
        evaluate.GetTagItem("imm.score").ShouldBe(1L);
        evaluate.GetTagItem("imm.max_score").ShouldBe(1L);
    }

    [TestMethod]
    public void Evaluate_RuleCannotBeEvaluated_TracesAnErrorNamingTheExceptionType()
    {
        // Arrange
        const string immYaml = $"""
            -
                Name: 'Date of Last IP Review'
                Id: '{RuleSetYaml.DateOfLastIpReviewRuleId}'
                Measures:
                    -
                        Date: 'last tuesday'
            """;
        using ActivityCapture capture = new(ImmTelemetry.ActivitySourceName);

        // Act
        Should.Throw<ImmEvaluationException>(() => ImmEvaluator.Evaluate(RuleSetYaml.DateOfLastIpReview, immYaml));

        // Assert
        Activity evaluate = capture.Single("imm.evaluate");
        evaluate.Status.ShouldBe(ActivityStatusCode.Error);
        evaluate.GetTagItem("error.type").ShouldBe(typeof(ImmEvaluationException).FullName);
        evaluate.GetTagItem("imm.score").ShouldBeNull();
    }
}
