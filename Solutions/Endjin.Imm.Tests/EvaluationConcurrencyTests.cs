using System.Collections.Concurrent;
using Endjin.Imm.Contracts;
using Endjin.Imm.Domain;
using Endjin.Imm.Processing;
using Endjin.Imm.Repository;
using Endjin.Imm.Tests.TestData;
using NodaTime;
using Shouldly;

namespace Endjin.Imm.Tests;

/// <summary>
/// One evaluation engine (and the rule definitions and calculators it shares between evaluations)
/// can be used by many requests at once.
/// </summary>
[TestClass]
public class EvaluationConcurrencyTests
{
    private const string RuleSet = $"""
        {RuleSetYaml.DateOfLastIpReview}
        {RuleSetYaml.FrameworkVersion}
        {RuleSetYaml.DeploymentWithOptionalMeasure}
        """;

    private static readonly string[] Imms =
    [
        ImmWith("2019-11-30", "netcoreapp6.0", deploymentOptOut: false, sharedEngineeringStandardsScore: 1),
        ImmWith("2019-10-15", "netcoreapp3.1", deploymentOptOut: true, sharedEngineeringStandardsScore: 0),
        ImmWith("2018-01-01", "node99", deploymentOptOut: false, sharedEngineeringStandardsScore: 0),
        ImmWith("2019-09-04", "NODE10", deploymentOptOut: true, sharedEngineeringStandardsScore: 1),
    ];

    [TestMethod]
    public void Evaluate_OneEngineUsedFromManyThreads_GivesTheSameResultsAsSequentialEvaluation()
    {
        // Arrange
        const int iterations = 2000;
        EvaluationEngine engine = new(new RuleDefinitionRepository(IpMaturityModelRuleset.FromYaml(RuleSet)));
        IEvaluationContext context = new FixedDateContext(new LocalDate(2019, 12, 3));
        IpMaturityModel[] imms = [.. Imms.Select(IpMaturityModel.FromYaml)];
        string[] expected = [.. imms.Select(imm => Describe(engine.Evaluate(imm, context)))];
        ConcurrentBag<string> mismatches = [];

        // Act
        Parallel.For(0, iterations, i =>
        {
            int which = i % imms.Length;
            string actual = Describe(engine.Evaluate(imms[which], context));
            if (actual != expected[which])
            {
                mismatches.Add($"Iteration {i}: expected '{expected[which]}' but got '{actual}'");
            }
        });

        // Assert
        mismatches.ShouldBeEmpty();
        expected.Distinct().Count().ShouldBe(imms.Length);
    }

    private static string Describe(ImmEvaluation evaluation) =>
        string.Join(
            "; ",
            evaluation.RuleEvaluations.Select(r => $"{r.RuleAssertion.Id}={r.Score}/{r.Percentage}%")) +
        $"; total={evaluation.TotalScore}/{evaluation.MaximumPossibleTotalScore}";

    private static string ImmWith(string date, string framework, bool deploymentOptOut, int sharedEngineeringStandardsScore)
    {
        string multiTenanted = deploymentOptOut
            ? """
                    -
                        Description: 'Multi-tenanted - as a Service'
                        OptOut: true
            """
            : """
                    -
                        Score: 1
                        Description: 'Multi-tenanted - as a Service'
            """;

        return $"""
            -
                Name: 'Date of Last IP Review'
                Id: '{RuleSetYaml.DateOfLastIpReviewRuleId}'
                Measures:
                    -
                        Date: {date}
            -
                Name: 'Framework Version'
                Id: '{RuleSetYaml.FrameworkVersionRuleId}'
                Measures:
                    -
                        Framework: {framework}
            -
                Name: 'Deployment'
                Id: '{RuleSetYaml.DeploymentRuleId}'
                Measures:
                    -
                        Score: 1
                        Description: 'Scripted and Documented'
            {multiTenanted}
            -
                Name: 'Shared Engineering Standards'
                Id: '{RuleSetYaml.SharedEngineeringStandardsRuleId}'
                Measures:
                    -
                        Score: {sharedEngineeringStandardsScore}
                        Description: {(sharedEngineeringStandardsScore == 1 ? "Configured" : "None")}
            """;
    }

    private sealed class FixedDateContext(LocalDate evaluationReferenceDate) : IEvaluationContext
    {
        public LocalDate EvaluationReferenceDate { get; } = evaluationReferenceDate;
    }
}
