using Endjin.Imm.Domain;
using Endjin.Imm.Processing;
using Endjin.Imm.Tests.TestData;
using Shouldly;

namespace Endjin.Imm.Tests;

/// <summary>
/// A discrete rule describes levels of maturity, of which a project is at exactly one, so the
/// project scores the level it asserts out of the highest level the rule defines. A rule whose
/// data type is not recognised scores nothing, but still counts towards the maximum.
/// </summary>
[TestClass]
public class DiscreteRuleTests
{
    private const string TestingRuleId = "3e4d6c1a-6f60-4d8f-a8c3-0bd7f1b1e5a2";

    private const string TestingRuleSet = $"""
        -
            Name: 'Automated Testing'
            Id: '{TestingRuleId}'
            DataType: Discrete
            Measures:
                -
                    Score: 0
                    Description: None
                -
                    Score: 1
                    Description: 'Some tests'
                -
                    Score: 2
                    Description: 'Tests for every feature'
                -
                    Score: 3
                    Description: 'Tests run on every commit'
        """;

    [TestMethod]
    [DataRow(0, "None", 0)]
    [DataRow(1, "Some tests", 33)]
    [DataRow(2, "Tests for every feature", 67)]
    [DataRow(3, "Tests run on every commit", 100)]
    public void Evaluate_AssertedLevel_ScoresThatLevelOutOfTheHighestLevel(int score, string description, int percentage)
    {
        // Arrange
        string immYaml = $"""
            -
                Name: 'Automated Testing'
                Id: '{TestingRuleId}'
                Measures:
                    -
                        Score: {score}
                        Description: '{description}'
            """;

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(TestingRuleSet, immYaml);

        // Assert
        RuleEvaluation rule = evaluation.ForRule(TestingRuleId);
        rule.Score.ShouldBe(score);
        rule.Percentage.ShouldBe(percentage);
        evaluation.TotalScore.ShouldBe(score);
        evaluation.MaximumPossibleTotalScore.ShouldBe(3);
    }

    [TestMethod]
    public void Evaluate_DiscreteRuleWithTwoMeasureAssertions_ThrowsImmEvaluationExceptionNamingTheRule()
    {
        // Arrange
        const string immYaml = $"""
            -
                Name: 'Automated Testing'
                Id: '{TestingRuleId}'
                Measures:
                    -
                        Score: 1
                        Description: 'Some tests'
                    -
                        Score: 2
                        Description: 'Tests for every feature'
            """;

        // Act
        ImmEvaluationException exception = Should.Throw<ImmEvaluationException>(() => ImmEvaluator.Evaluate(TestingRuleSet, immYaml));

        // Assert
        exception.RuleId.ShouldBe(Guid.Parse(TestingRuleId));
        exception.Message.ShouldContain(TestingRuleId);
        exception.Message.ShouldContain("Automated Testing");
        exception.InnerException.ShouldBeOfType<InvalidOperationException>();
    }

    [TestMethod]
    public void Evaluate_NoLevelAsserted_ThrowsImmEvaluationExceptionNamingTheRule()
    {
        // Arrange
        const string immYaml = $"""
            -
                Name: 'Automated Testing'
                Id: '{TestingRuleId}'
                Measures: []
            """;

        // Act
        ImmEvaluationException exception = Should.Throw<ImmEvaluationException>(() => ImmEvaluator.Evaluate(TestingRuleSet, immYaml));

        // Assert
        exception.RuleId.ShouldBe(Guid.Parse(TestingRuleId));
        exception.InnerException.ShouldBeOfType<InvalidOperationException>();
    }

    [TestMethod]
    public void Evaluate_RuleWithUnrecognisedDataType_ScoresZeroButCountsTowardsTheMaximum()
    {
        // Arrange
        const string ruleSetYaml = $"""
            -
                Name: 'Automated Testing'
                Id: '{TestingRuleId}'
                DataType: 99
                Measures:
                    -
                        Score: 0
                        Description: None
                    -
                        Score: 3
                        Description: 'Tests run on every commit'
            -
                Name: 'Shared Engineering Standards'
                Id: '{RuleSetYaml.SharedEngineeringStandardsRuleId}'
                DataType: Discrete
                Measures:
                    -
                        Score: 0
                        Description: None
                    -
                        Score: 1
                        Description: Configured
            """;
        const string immYaml = $"""
            -
                Name: 'Automated Testing'
                Id: '{TestingRuleId}'
                Measures:
                    -
                        Score: 3
                        Description: 'Tests run on every commit'
            -
                Name: 'Shared Engineering Standards'
                Id: '{RuleSetYaml.SharedEngineeringStandardsRuleId}'
                Measures:
                    -
                        Score: 1
                        Description: Configured
            """;

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(ruleSetYaml, immYaml);

        // Assert
        RuleEvaluation unrecognised = evaluation.ForRule(TestingRuleId);
        unrecognised.Score.ShouldBe(0);
        unrecognised.Percentage.ShouldBe(0);
        evaluation.ForRule(RuleSetYaml.SharedEngineeringStandardsRuleId).Score.ShouldBe(1);
        evaluation.TotalScore.ShouldBe(1);
        evaluation.MaximumPossibleTotalScore.ShouldBe(4);
    }
}
