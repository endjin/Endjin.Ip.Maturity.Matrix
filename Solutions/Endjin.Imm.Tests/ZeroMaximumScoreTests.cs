using Endjin.Imm.Domain;
using Endjin.Imm.Tests.TestData;
using Shouldly;

namespace Endjin.Imm.Tests;

/// <summary>
/// A rule can leave a project nothing to score against: every measure that applies to the project
/// can be opted out of, or the rule set can define the rule with no measures at all. Such a rule
/// scores 0 out of 0, which is 0%, just as an IMM that opts out of every rule has a total of 0 out of 0.
/// </summary>
[TestClass]
public class ZeroMaximumScoreTests
{
    private const string RuleId = "5a0f3c6e-2d7b-4c1e-9b8a-1f4e6d2c3b70";

    [TestMethod]
    public void Evaluate_AllApplicableMeasuresOptedOut_ScoresZeroRatherThanDividingByZero()
    {
        // Arrange
        const string ruleSetYaml = $"""
            -
                Name: 'Hosting'
                Id: '{RuleId}'
                DataType: Continuous
                Measures:
                    -
                        Score: 1
                        Description: 'Scripted'
                        CanOptOut: true
                    -
                        Score: 1
                        Description: 'Templated'
                        CanOptOut: true
            """;
        const string immYaml = $"""
            -
                Name: 'Hosting'
                Id: '{RuleId}'
                Measures:
                    -
                        Description: 'Scripted'
                        OptOut: true
                    -
                        Description: 'Templated'
                        OptOut: true
            """;

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(ruleSetYaml, immYaml);

        // Assert
        RuleEvaluation rule = evaluation.ForRule(RuleId);
        rule.Score.ShouldBe(0);
        rule.MaximumScore.ShouldBe(0);
        rule.Percentage.ShouldBe(0m);
        evaluation.TotalScore.ShouldBe(0);
        evaluation.MaximumPossibleTotalScore.ShouldBe(0);
    }

    [TestMethod]
    [DataRow("Continuous")]
    [DataRow("Age")]
    [DataRow("Framework")]
    [DataRow("99")]
    public void Evaluate_RuleDefinitionWithNoMeasures_ScoresZeroRatherThanThrowing(string dataType)
    {
        // Arrange
        string ruleSetYaml = $"""
            -
                Name: 'Empty Rule'
                Id: '{RuleId}'
                DataType: {dataType}
                Measures: []
            """;
        const string immYaml = $"""
            -
                Name: 'Empty Rule'
                Id: '{RuleId}'
                Measures: []
            """;

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(ruleSetYaml, immYaml);

        // Assert
        RuleEvaluation rule = evaluation.ForRule(RuleId);
        rule.Score.ShouldBe(0);
        rule.MaximumScore.ShouldBe(0);
        rule.Percentage.ShouldBe(0m);
        evaluation.MaximumPossibleTotalScore.ShouldBe(0);
    }
}
