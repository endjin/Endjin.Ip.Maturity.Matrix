using Endjin.Imm.Domain;
using Endjin.Imm.Tests.TestData;
using Shouldly;

namespace Endjin.Imm.Tests;

/// <summary>
/// A project's IP Maturity Model can be loaded from its <c>imm.yaml</c>, including the shorthand
/// forms people write by hand: a measure entry with no content, and a file with no rules yet.
/// </summary>
[TestClass]
public class ImmLoadingTests
{
    [TestMethod]
    [DataRow("")]
    [DataRow("~")]
    [DataRow("null")]
    [DataRow("Null")]
    [DataRow("NULL")]
    [DataRow("!!null")]
    [DataRow("!!null ~")]
    public void FromYaml_NullMeasureEntry_LoadsAnEmptyMeasureAssertion(string nullMeasure)
    {
        // Arrange
        string immYaml = $"""
            -
                Name: 'Shared Engineering Standards'
                Id: '{RuleSetYaml.SharedEngineeringStandardsRuleId}'
                Measures:
                    -
                        Score: 1
                        Description: Configured
                    - {nullMeasure}
            """;

        // Act
        IpMaturityModel imm = IpMaturityModel.FromYaml(immYaml);

        // Assert
        RuleAssertion rule = imm.Rules.ShouldHaveSingleItem();
        rule.Measures.Count.ShouldBe(2);
        MeasureAssertion emptyMeasure = rule.Measures[1].ShouldBeOfType<MeasureAssertion>();
        emptyMeasure.Score.ShouldBe(0);
        emptyMeasure.Description.ShouldBe("");
        emptyMeasure.OptOut.ShouldBeFalse();
    }

    [TestMethod]
    public void FromYaml_QuotedNullWord_IsAnErrorRatherThanAnEmptyMeasure()
    {
        // Arrange
        // Only a plain (unquoted) null is a null; a quoted 'null' is a string, which is not a measure.
        const string immYaml = $"""
            -
                Name: 'Shared Engineering Standards'
                Id: '{RuleSetYaml.SharedEngineeringStandardsRuleId}'
                Measures:
                    - 'null'
            """;

        // Act & Assert
        Should.Throw<YamlDotNet.Core.YamlException>(() => IpMaturityModel.FromYaml(immYaml));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("# This project has not assessed itself yet.")]
    public void FromYaml_DocumentWithNoRules_LoadsAnImmWithNoRules(string immYaml)
    {
        // Act
        IpMaturityModel imm = IpMaturityModel.FromYaml(immYaml);

        // Assert
        imm.Rules.ShouldNotBeNull();
        imm.Rules.ShouldBeEmpty();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("# No rules have been defined yet.")]
    public void FromYaml_RuleSetWithNoRules_LoadsARuleSetWithNoRules(string ruleSetYaml)
    {
        // Act
        IpMaturityModelRuleset ruleSet = IpMaturityModelRuleset.FromYaml(ruleSetYaml);

        // Assert
        ruleSet.Rules.ShouldNotBeNull();
        ruleSet.Rules.ShouldBeEmpty();
    }

    [TestMethod]
    public void Evaluate_ImmWithNoRules_ScoresZeroOutOfZero()
    {
        // Act
        ImmEvaluation evaluation = ImmEvaluator.Evaluate(RuleSetYaml.DeploymentWithOptionalMeasure, "");

        // Assert
        evaluation.RuleEvaluations.ShouldBeEmpty();
        evaluation.TotalScore.ShouldBe(0);
        evaluation.MaximumPossibleTotalScore.ShouldBe(0);
    }
}
