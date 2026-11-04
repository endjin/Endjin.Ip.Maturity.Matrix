using Endjin.Imm.Domain;
using Endjin.Imm.Processing;
using Endjin.Imm.Tests.TestData;
using Shouldly;

namespace Endjin.Imm.Tests;

/// <summary>
/// Age-related measures stay accurate even when the IP Maturity Model has not been updated in a
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

    // A '>' age is the complement of the corresponding '<' age, so the boundary is handled the same way: a date
    // exactly three months ago today is past the 3 month mark.
    [TestMethod]
    [DataRow("2019-09-03")]
    [DataRow("2019-09-02")]
    [DataRow("2018-12-03")]
    public void Evaluate_GreaterThanAgeAndDateAtLeastThreeMonthsOld_ScoresTheGreaterThanMeasure(string date)
    {
        // Arrange
        string immYaml = ImmWithDate(date);

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(StaleAfterThreeMonthsRuleSet, immYaml);

        // Assert
        RuleEvaluation rule = evaluation.ForRule(RuleSetYaml.DateOfLastIpReviewRuleId);
        rule.Score.ShouldBe(1);
        rule.Percentage.ShouldBe(33);
        evaluation.MaximumPossibleTotalScore.ShouldBe(3);
    }

    [TestMethod]
    [DataRow("2019-12-03")]
    [DataRow("2019-11-03")]
    [DataRow("2019-09-04")]
    public void Evaluate_GreaterThanAgeAndDateLessThanThreeMonthsOld_SkipsTheGreaterThanMeasure(string date)
    {
        // Arrange
        string immYaml = ImmWithDate(date);

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(StaleAfterThreeMonthsRuleSet, immYaml);

        // Assert
        RuleEvaluation rule = evaluation.ForRule(RuleSetYaml.DateOfLastIpReviewRuleId);
        rule.Score.ShouldBe(3);
        rule.Percentage.ShouldBe(100);
        evaluation.MaximumPossibleTotalScore.ShouldBe(3);
    }

    [TestMethod]
    public void Evaluate_AgeWithoutAComparison_NeverMatches()
    {
        // Arrange
        const string ruleSetYaml = $"""
            -
                Name: 'Date of Last IP Review'
                Id: '{RuleSetYaml.DateOfLastIpReviewRuleId}'
                DataType: Age
                Measures:
                    -
                        Score: 3
                        Age: P1M
                        Description: 'Missing the < comparison'
                    -
                        Score: 1
                        Age: '*'
                        Description: 'Any date'
            """;

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(ruleSetYaml, ImmWithDate("2019-12-03"));

        // Assert
        evaluation.ForRule(RuleSetYaml.DateOfLastIpReviewRuleId).Score.ShouldBe(1);
    }

    [TestMethod]
    public void Evaluate_DateOlderThanEveryAgeAndNoFallback_ScoresZero()
    {
        // Arrange
        const string ruleSetYaml = $"""
            -
                Name: 'Date of Last IP Review'
                Id: '{RuleSetYaml.DateOfLastIpReviewRuleId}'
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
            """;

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(ruleSetYaml, ImmWithDate("2019-01-01"));

        // Assert
        RuleEvaluation rule = evaluation.ForRule(RuleSetYaml.DateOfLastIpReviewRuleId);
        rule.Score.ShouldBe(0);
        rule.Percentage.ShouldBe(0);
        evaluation.MaximumPossibleTotalScore.ShouldBe(3);
    }

    [TestMethod]
    public void Evaluate_DateNotSpecifiedAndRuleDefinesAFallbackMeasure_ScoresTheFallbackMeasure()
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
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(ReviewWithNotYetReviewedFallbackRuleSet, immYaml);

        // Assert
        RuleEvaluation rule = evaluation.ForRule(RuleSetYaml.DateOfLastIpReviewRuleId);
        rule.Score.ShouldBe(1);
        rule.Percentage.ShouldBe(33);
        evaluation.TotalScore.ShouldBe(1);
        evaluation.MaximumPossibleTotalScore.ShouldBe(3);
    }

    [TestMethod]
    [DataRow("'last tuesday'")]
    [DataRow("2019-13-01")]
    public void Evaluate_UnparsableDate_ThrowsImmEvaluationExceptionNamingTheRule(string date)
    {
        // Arrange
        string immYaml = ImmWithDate(date);

        // Act
        ImmEvaluationException exception = Should.Throw<ImmEvaluationException>(
            () => ImmEvaluator.Evaluate(RuleSetYaml.DateOfLastIpReview, immYaml));

        // Assert
        exception.RuleId.ShouldBe(Guid.Parse(RuleSetYaml.DateOfLastIpReviewRuleId));
        exception.InnerException.ShouldBeOfType<NodaTime.Text.UnparsableValueException>();
    }

    // Age measures are tried in order, and a date that matches none of them reaches the fallback measure (the one
    // without an Age). That is a rule set authoring error: the fallback is only for IMMs without a Date, so a rule
    // set must end its Age measures with '*' before the fallback.
    [TestMethod]
    public void Evaluate_DateMatchesNoAgeBeforeTheFallbackMeasure_ThrowsImmEvaluationExceptionNamingTheRule()
    {
        // Arrange
        string immYaml = ImmWithDate("2019-01-01");

        // Act
        ImmEvaluationException exception = Should.Throw<ImmEvaluationException>(
            () => ImmEvaluator.Evaluate(ReviewWithNotYetReviewedFallbackRuleSet, immYaml));

        // Assert
        exception.RuleId.ShouldBe(Guid.Parse(RuleSetYaml.DateOfLastIpReviewRuleId));
        ArgumentException cause = exception.InnerException.ShouldBeOfType<ArgumentException>();
        cause.ParamName.ShouldBe("ruleAssertion");
        cause.Message.ShouldStartWith("Rule definition's Age is not set, so it should not be used with AgeRuleCalculator");
    }

    /// <summary>
    /// An Age rule that uses a '>' age to mark a review as stale.
    /// </summary>
    private const string StaleAfterThreeMonthsRuleSet = $"""
        -
            Name: 'Date of Last IP Review'
            Id: '{RuleSetYaml.DateOfLastIpReviewRuleId}'
            DataType: Age
            Measures:
                -
                    Score: 1
                    Age: '>P3M'
                    Description: 'Stale'
                -
                    Score: 3
                    Age: '*'
                    Description: 'Recent'
        """;

    /// <summary>
    /// An Age rule with no '*' age, whose fallback measure scores 1 for an IMM that has no Date.
    /// </summary>
    private const string ReviewWithNotYetReviewedFallbackRuleSet = $"""
        -
            Name: 'Date of Last IP Review'
            Id: '{RuleSetYaml.DateOfLastIpReviewRuleId}'
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
                    Description: 'Not yet reviewed'
        """;

    private static string ImmWithDate(string date) => $"""
        -
            Name: 'Date of Last IP Review'
            Id: '{RuleSetYaml.DateOfLastIpReviewRuleId}'
            Measures:
                -
                    Date: {date}
        """;
}
