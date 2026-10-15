using System.Globalization;
using Endjin.Imm.Domain;
using Endjin.Imm.Processing;
using Endjin.Imm.Repository;
using Endjin.Imm.Tests.TestData;
using NodaTime;
using Shouldly;

namespace Endjin.Imm.Tests;

/// <summary>
/// When no evaluation context is supplied (as is the case when the host renders a badge), age-based
/// rules are evaluated against today's date, so that scores drop as the IMM ages.
/// </summary>
[TestClass]
public class DefaultEvaluationDateTests
{
    // Each date sits well inside its scoring band, so the outcome is the same even if the test
    // runs across midnight.
    [TestMethod]
    [DataRow(0, 3)]
    [DataRow(2, 2)]
    [DataRow(12, 1)]
    public void Evaluate_WithoutAnEvaluationContext_AgesTheDateAgainstToday(int monthsAgo, int expectedScore)
    {
        // Arrange
        LocalDate today = LocalDate.FromDateTime(DateTime.Now);
        string date = today.PlusMonths(-monthsAgo).ToString("uuuu-MM-dd", CultureInfo.InvariantCulture);
        string immYaml = $"""
            -
                Name: 'Date of Last IP Review'
                Id: '{RuleSetYaml.DateOfLastIpReviewRuleId}'
                Measures:
                    -
                        Date: {date}
            """;
        EvaluationEngine engine = new(new RuleDefinitionRepository(IpMaturityMatrixRuleset.FromYaml(RuleSetYaml.DateOfLastIpReview)));

        // Act
        ImmEvaluation evaluation = engine.Evaluate(IpMaturityMatrix.FromYaml(immYaml));

        // Assert
        evaluation.ForRule(RuleSetYaml.DateOfLastIpReviewRuleId).Score.ShouldBe(expectedScore);
        evaluation.TotalScore.ShouldBe(expectedScore);
        evaluation.MaximumPossibleTotalScore.ShouldBe(3);
    }
}
