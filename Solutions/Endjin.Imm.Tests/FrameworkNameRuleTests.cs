using Endjin.Imm.Domain;
using Endjin.Imm.Processing;
using Endjin.Imm.Tests.TestData;
using Shouldly;

namespace Endjin.Imm.Tests;

/// <summary>
/// Supported framework version measures stay accurate even when the IP Maturity Matrix has not
/// been updated in a long while, so that stale aspects of the IP are detected.
/// </summary>
[TestClass]
public class FrameworkNameRuleTests
{
    // We support the idea of multiple current versions. This doesn't happen for .NET, but with node.js,
    // things get a little confused by the distinction between the "current" version and the latest LTS
    // version. Any new version becomes "current" for 6 months, and when that 6 months is up, if it's
    // an even-numbered release, it then comes the latest LTS version. So the "current" version is always
    // different from the "latest LTS".
    // Since the purpose of this score is to indicate freshness, we give top marks either to the current
    // version or the latest LTS.
    // For example, according to the release plan, by May 2020 node.js v14 will be the current version, but
    // it will not yet be the LTS version. The LTS version at that point will be node.js v12, while v10 will
    // still be an "active" LTS version.
    // There's also the notion of "active" vs "maintenance". When an LTS version gets to the end of its time,
    // its moves int "maintenance" mode. It's still supported at that point.
    [TestMethod]
    [DataRow("netcoreapp6.0")]
    [DataRow("node14")]
    [DataRow("node12")]
    public void Evaluate_MostCurrentVersion_ScoresThree(string framework)
    {
        // Arrange
        string immYaml = ImmWithFramework(framework);

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(RuleSetYaml.FrameworkVersion, immYaml);

        // Assert
        RuleEvaluation rule = evaluation.ForRule(RuleSetYaml.FrameworkVersionRuleId);
        rule.Score.ShouldBe(3);
        rule.Percentage.ShouldBe(100);
        evaluation.TotalScore.ShouldBe(3);
        evaluation.MaximumPossibleTotalScore.ShouldBe(3);
    }

    [TestMethod]
    [DataRow("netcoreapp3.1")]
    [DataRow("netcoreapp2.1")]
    [DataRow("node10")]
    [DataRow("node8")]
    [DataRow("NETCOREAPP3.1")]
    [DataRow("NetCoreApp2.1")]
    [DataRow("Node10")]
    public void Evaluate_SupportedLtsVersion_ScoresTwo(string framework)
    {
        // Arrange
        string immYaml = ImmWithFramework(framework);

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(RuleSetYaml.FrameworkVersion, immYaml);

        // Assert
        RuleEvaluation rule = evaluation.ForRule(RuleSetYaml.FrameworkVersionRuleId);
        rule.Score.ShouldBe(2);
        rule.Percentage.ShouldBe(67);
        evaluation.TotalScore.ShouldBe(2);
        evaluation.MaximumPossibleTotalScore.ShouldBe(3);
    }

    [TestMethod]
    [DataRow("netcoreapp2.0")]
    [DataRow("node13")]
    [DataRow("node11")]
    [DataRow("node7")]
    [DataRow("node6")]
    public void Evaluate_UnsupportedVersion_ScoresOne(string framework)
    {
        // Arrange
        string immYaml = ImmWithFramework(framework);

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(RuleSetYaml.FrameworkVersion, immYaml);

        // Assert
        RuleEvaluation rule = evaluation.ForRule(RuleSetYaml.FrameworkVersionRuleId);
        rule.Score.ShouldBe(1);
        rule.Percentage.ShouldBe(33);
        evaluation.TotalScore.ShouldBe(1);
        evaluation.MaximumPossibleTotalScore.ShouldBe(3);
    }

    [TestMethod]
    [DataRow(0, 0, "None")]
    [DataRow(1, 33, "Using an unsupported version")]
    [DataRow(2, 67, "Using a LTS version")]
    [DataRow(3, 100, "Using the most current LTS version")]
    public void Evaluate_LegacyScoreWithoutFramework_ScoresTheAssertedScore(int score, int percentage, string description)
    {
        // Arrange
        string immYaml = $"""
            -
                Name: 'Framework Version'
                Id: '{RuleSetYaml.FrameworkVersionRuleId}'
                Measures:
                    -
                        Score: {score}
                        Description: '{description}'
            """;

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(RuleSetYaml.FrameworkVersion, immYaml);

        // Assert
        RuleEvaluation rule = evaluation.ForRule(RuleSetYaml.FrameworkVersionRuleId);
        rule.Score.ShouldBe(score);
        rule.Percentage.ShouldBe(percentage);
        evaluation.TotalScore.ShouldBe(score);
        evaluation.MaximumPossibleTotalScore.ShouldBe(3);
    }

    [TestMethod]
    public void Evaluate_FrameworkNotSpecifiedAndRuleDefinesAFallbackMeasure_ScoresTheFallbackMeasure()
    {
        // Arrange
        const string immYaml = $"""
            -
                Name: 'Framework Version'
                Id: '{RuleSetYaml.FrameworkVersionRuleId}'
                Measures:
                    -
            """;

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(FrameworkWithUnknownFrameworkFallbackRuleSet, immYaml);

        // Assert
        RuleEvaluation rule = evaluation.ForRule(RuleSetYaml.FrameworkVersionRuleId);
        rule.Score.ShouldBe(1);
        rule.Percentage.ShouldBe(33);
        evaluation.TotalScore.ShouldBe(1);
        evaluation.MaximumPossibleTotalScore.ShouldBe(3);
    }

    [TestMethod]
    public void Evaluate_UnlistedFrameworkAndNoWildcardOrFallback_ScoresZero()
    {
        // Arrange
        const string ruleSetYaml = $"""
            -
                Name: 'Framework Version'
                Id: '{RuleSetYaml.FrameworkVersionRuleId}'
                DataType: Framework
                Measures:
                    -
                        Score: 3
                        Framework: net10.0
                        Description: 'Using the most current LTS version'
                    -
                        Score: 2
                        Framework: net8.0
                        Description: 'Using a LTS version'
            """;

        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(ruleSetYaml, ImmWithFramework("netcoreapp2.0"));

        // Assert
        RuleEvaluation rule = evaluation.ForRule(RuleSetYaml.FrameworkVersionRuleId);
        rule.Score.ShouldBe(0);
        rule.Percentage.ShouldBe(0);
        evaluation.MaximumPossibleTotalScore.ShouldBe(3);
    }

    // Framework measures are tried in order, and a framework that matches none of them reaches the fallback measure
    // (the one without a Framework). That is a rule set authoring error: the fallback is only for IMMs without a
    // Framework, so a rule set must end its Framework measures with '*' before the fallback.
    [TestMethod]
    public void Evaluate_FrameworkMatchesNothingBeforeTheFallbackMeasure_ThrowsImmEvaluationExceptionNamingTheRule()
    {
        // Arrange
        string immYaml = ImmWithFramework("netcoreapp2.0");

        // Act
        ImmEvaluationException exception = Should.Throw<ImmEvaluationException>(
            () => ImmEvaluator.Evaluate(FrameworkWithUnknownFrameworkFallbackRuleSet, immYaml));

        // Assert
        exception.RuleId.ShouldBe(Guid.Parse(RuleSetYaml.FrameworkVersionRuleId));
        ArgumentException cause = exception.InnerException.ShouldBeOfType<ArgumentException>();
        cause.ParamName.ShouldBe("ruleAssertion");
        cause.Message.ShouldStartWith("Rule measure definition's Framework is not set, so it should not be used with FrameworkRuleCalculator");
    }

    /// <summary>
    /// A Framework rule with no '*' framework, whose fallback measure scores 1 for an IMM that has
    /// no Framework.
    /// </summary>
    private const string FrameworkWithUnknownFrameworkFallbackRuleSet = $"""
        -
            Name: 'Framework Version'
            Id: '{RuleSetYaml.FrameworkVersionRuleId}'
            DataType: Framework
            Measures:
                -
                    Score: 3
                    Framework: net10.0
                    Description: 'Using the most current LTS version'
                -
                    Score: 2
                    Framework: net8.0
                    Description: 'Using a LTS version'
                -
                    Score: 1
                    Description: 'Framework not stated'
        """;

    private static string ImmWithFramework(string framework) => $"""
        -
            Name: 'Framework Version'
            Id: '{RuleSetYaml.FrameworkVersionRuleId}'
            Measures:
                -
                    Framework: {framework}
        """;
}
