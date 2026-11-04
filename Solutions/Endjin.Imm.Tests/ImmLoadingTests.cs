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
    public void FromYaml_RuleWithAgeProperty_IgnoresTheAgeProperty()
    {
        // Arrange
        // Some published imm.yaml files (for example endjin/dotnet-adr) put an Age key on the
        // rule itself. A rule assertion has no such property, so it is ignored like any other
        // unrecognised key.
        const string immYaml = $"""
            -
                Name: 'Date of Last IP Review'
                Id: '{RuleSetYaml.DateOfLastIpReviewRuleId}'
                Age: <P1M
                Measures:
                    -
                        Score: 3
                        Date: 2026-09-01
            """;

        // Act
        IpMaturityModel imm = IpMaturityModel.FromYaml(immYaml);

        // Assert
        RuleAssertion rule = imm.Rules.ShouldHaveSingleItem();
        rule.Name.ShouldBe("Date of Last IP Review");
        rule.Id.ShouldBe(Guid.Parse(RuleSetYaml.DateOfLastIpReviewRuleId));
        AgeMeasureAssertion measure = rule.Measures.ShouldHaveSingleItem().ShouldBeOfType<AgeMeasureAssertion>();
        measure.Score.ShouldBe(3);
        measure.Date.ShouldBe("2026-09-01");
    }

    [TestMethod]
    [DataRow("Name", "Id", "OptOut", "Measures", DisplayName = "Pascal case")]
    [DataRow("name", "id", "optout", "measures", DisplayName = "Lower case")]
    [DataRow("NAME", "ID", "OPTOUT", "MEASURES", DisplayName = "Upper case")]
    [DataRow("'Name'", "\"Id\"", "'OptOut'", "\"Measures\"", DisplayName = "Quoted")]
    public void FromYaml_RuleKeysInAnyCase_LoadsEveryProperty(string name, string id, string optOut, string measures)
    {
        // Arrange
        string immYaml = $"""
            -
                {name}: 'Deployment'
                {id}: '{RuleSetYaml.DeploymentRuleId}'
                {optOut}: true
                {measures}:
                    -
                        Score: 1
                        Description: Manual
            """;

        // Act
        IpMaturityModel imm = IpMaturityModel.FromYaml(immYaml);

        // Assert
        RuleAssertion rule = imm.Rules.ShouldHaveSingleItem();
        rule.Name.ShouldBe("Deployment");
        rule.Id.ShouldBe(Guid.Parse(RuleSetYaml.DeploymentRuleId));
        rule.OptOut.ShouldBeTrue();
        MeasureAssertion measure = rule.Measures.ShouldHaveSingleItem();
        measure.Score.ShouldBe(1);
        measure.Description.ShouldBe("Manual");
    }

    [TestMethod]
    [DataRow("true", true)]
    [DataRow("yes", true)]
    [DataRow("on", true)]
    [DataRow("false", false)]
    [DataRow("no", false)]
    [DataRow("off", false)]
    public void FromYaml_RuleOptOutWord_LoadsTheOptOutFlag(string optOut, bool expected)
    {
        // Arrange
        string immYaml = $"- {{Name: Deployment, Id: '{RuleSetYaml.DeploymentRuleId}', OptOut: {optOut}, Measures: []}}";

        // Act
        IpMaturityModel imm = IpMaturityModel.FromYaml(immYaml);

        // Assert
        imm.Rules.ShouldHaveSingleItem().OptOut.ShouldBe(expected);
    }

    [TestMethod]
    [DataRow("Id: not-a-guid", DisplayName = "Invalid Id")]
    [DataRow("OptOut: maybe", DisplayName = "Invalid OptOut")]
    [DataRow("Name: {a: b}", DisplayName = "Mapping for Name")]
    [DataRow("Measures: {Score: 1}", DisplayName = "Mapping for Measures")]
    public void FromYaml_RulePropertyWithInvalidValue_ThrowsYamlException(string property)
    {
        // Arrange
        string immYaml = $"-\n    Name: 'Deployment'\n    {property}\n";

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
