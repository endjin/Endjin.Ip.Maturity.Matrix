using Endjin.Imm.Domain;
using Endjin.Imm.Tests.TestData;
using Shouldly;

namespace Endjin.Imm.Tests;

/// <summary>
/// Age-related measures stay accurate even when the IP Maturity Matrix has not been updated in a
/// long while, so that stale aspects of the IP are detected. All evaluations use a reference date
/// of 2019-12-03.
/// </summary>
[TestClass]
public class AgeBasedRuleTests
{
    [TestMethod]
    [DataRow("2019-12-03")]
    [DataRow("2019-11-30")]
    [DataRow("2019-11-04")]
    public void Evaluate_DateLessThanOneMonthOld_ScoresThree(string date)
    {
        // Arrange
        string immYaml = ImmWithDate(date);

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(RuleSetYaml.DateOfLastIpReview, immYaml);

        // Assert
        evaluation.ForRule(RuleSetYaml.DateOfLastIpReviewRuleId).Score.ShouldBe(3);
        evaluation.TotalScore.ShouldBe(3);
        evaluation.MaximumPossibleTotalScore.ShouldBe(3);
    }

    // Note: the rule definition doesn't make it clear what we should do when the description was updated 1 month ago.
    // If we were using a precise timestamp this would be more or less irrelevant, because it almost never comes up.
    // But since we're using just dates, it does make a difference. We're arbitrarily handling the "one month ago today"
    // case as being past the 1 month old case
    [TestMethod]
    [DataRow("2019-11-03")]
    [DataRow("2019-11-02")]
    [DataRow("2019-10-15")]
    [DataRow("2019-09-04")]
    public void Evaluate_DateBetweenOneAndThreeMonthsOld_ScoresTwo(string date)
    {
        // Arrange
        string immYaml = ImmWithDate(date);

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(RuleSetYaml.DateOfLastIpReview, immYaml);

        // Assert
        evaluation.ForRule(RuleSetYaml.DateOfLastIpReviewRuleId).Score.ShouldBe(2);
        evaluation.TotalScore.ShouldBe(2);
        evaluation.MaximumPossibleTotalScore.ShouldBe(3);
    }

    [TestMethod]
    [DataRow("2019-09-03")]
    [DataRow("2019-09-01")]
    [DataRow("2019-01-01")]
    [DataRow("2018-12-04")]
    [DataRow("2018-12-03")]
    public void Evaluate_DateAtLeastThreeMonthsOld_ScoresOne(string date)
    {
        // Arrange
        string immYaml = ImmWithDate(date);

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(RuleSetYaml.DateOfLastIpReview, immYaml);

        // Assert
        evaluation.ForRule(RuleSetYaml.DateOfLastIpReviewRuleId).Score.ShouldBe(1);
        evaluation.TotalScore.ShouldBe(1);
        evaluation.MaximumPossibleTotalScore.ShouldBe(3);
    }

    [TestMethod]
    public void Evaluate_DateNotSpecified_ScoresZero()
    {
        // Arrange
        const string immYaml = $"""
            -
                Name: 'Date of Last IP Review'
                Id: '{RuleSetYaml.DateOfLastIpReviewRuleId}'
                Measures:
                    -
            """;

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(RuleSetYaml.DateOfLastIpReview, immYaml);

        // Assert
        evaluation.ForRule(RuleSetYaml.DateOfLastIpReviewRuleId).Score.ShouldBe(0);
        evaluation.TotalScore.ShouldBe(0);
        evaluation.MaximumPossibleTotalScore.ShouldBe(3);
    }

    [TestMethod]
    [DataRow(0, "None")]
    [DataRow(1, "> 3 months")]
    [DataRow(2, "> 1 month")]
    [DataRow(3, "< 1 month")]
    public void Evaluate_LegacyScoreWithoutDate_ScoresTheAssertedScore(int score, string description)
    {
        // Arrange
        // Descriptions are quoted because a plain YAML scalar cannot start with '>'.
        string immYaml = $"""
            -
                Name: 'Date of Last IP Review'
                Id: '{RuleSetYaml.DateOfLastIpReviewRuleId}'
                Measures:
                    -
                        Score: {score}
                        Description: '{description}'
            """;

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(RuleSetYaml.DateOfLastIpReview, immYaml);

        // Assert
        evaluation.ForRule(RuleSetYaml.DateOfLastIpReviewRuleId).Score.ShouldBe(score);
        evaluation.TotalScore.ShouldBe(score);
        evaluation.MaximumPossibleTotalScore.ShouldBe(3);
    }

    private static string ImmWithDate(string date) => $"""
        -
            Name: 'Date of Last IP Review'
            Id: '{RuleSetYaml.DateOfLastIpReviewRuleId}'
            Measures:
                -
                    Date: {date}
        """;
}
