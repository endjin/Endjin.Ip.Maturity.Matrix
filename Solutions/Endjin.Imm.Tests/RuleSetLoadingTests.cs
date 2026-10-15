using Endjin.Imm.Domain;
using Endjin.Imm.Tests.TestData;
using Shouldly;

namespace Endjin.Imm.Tests;

/// <summary>
/// Rule definitions can be loaded from YAML, so that an IP Maturity Matrix can be processed.
/// </summary>
[TestClass]
public class RuleSetLoadingTests
{
    [TestMethod]
    public void FromYaml_FrameworkRule_LoadsFrameworkMeasureDefinitions()
    {
        // Act
        IpMaturityMatrixRuleset ruleSet = IpMaturityMatrixRuleset.FromYaml(RuleSetYaml.FrameworkVersion);

        // Assert
        RuleDefinition rule = ruleSet.Rules.ShouldHaveSingleItem();
        rule.Name.ShouldBe("Framework Version");
        rule.Id.ShouldBe(Guid.Parse(RuleSetYaml.FrameworkVersionRuleId));
        rule.DataType.ShouldBe(DataType.Framework);
        rule.Measures.Count.ShouldBe(9);
        rule.Measures.Select(m => m.Score).ShouldBe(new long[] { 3, 2, 2, 3, 3, 2, 2, 1, 0 });
        rule.Measures.Select(m => m.Description).ShouldBe(new[]
        {
            "Using the most current LTS version",
            "Using a LTS version",
            "Using a LTS version",
            "Using the most current LTS version",
            "Using the most current LTS version",
            "Using a LTS version",
            "Using a LTS version",
            "Using an unsupported version",
            "None",
        });

        rule.Measures.Take(8).ShouldAllBe(m => m is FrameworkMeasureDefinition);
        rule.Measures.Take(8).Cast<FrameworkMeasureDefinition>().Select(m => m.Framework).ShouldBe(new[]
        {
            "netcoreapp6.0",
            "netcoreapp3.1",
            "netcoreapp2.1",
            "node14",
            "node12",
            "node10",
            "node8",
            "*",
        });
        rule.Measures[8].ShouldBeOfType<MeasureDefinition>();
    }

    [TestMethod]
    public void FromYaml_AgeRule_LoadsAgeMeasureDefinitions()
    {
        // Act
        IpMaturityMatrixRuleset ruleSet = IpMaturityMatrixRuleset.FromYaml(RuleSetYaml.DateOfLastIpReview);

        // Assert
        RuleDefinition rule = ruleSet.Rules.ShouldHaveSingleItem();
        rule.Name.ShouldBe("Date of Last IP Review");
        rule.Id.ShouldBe(Guid.Parse(RuleSetYaml.DateOfLastIpReviewRuleId));
        rule.DataType.ShouldBe(DataType.Age);
        rule.Measures.Count.ShouldBe(4);
        rule.Measures.Select(m => m.Score).ShouldBe(new long[] { 3, 2, 1, 0 });
        rule.Measures.Select(m => m.Description).ShouldBe(new[] { "< 1 month", "> 1 month", "> 3 months", "None" });

        rule.Measures.Take(3).ShouldAllBe(m => m is AgeMeasureDefinition);
        rule.Measures.Take(3).Cast<AgeMeasureDefinition>().Select(m => m.Age).ShouldBe(new[] { "<P1M", "<P3M", "*" });
        rule.Measures[3].ShouldBeOfType<MeasureDefinition>();
    }

    [TestMethod]
    public void FromYaml_MeasureWithUnknownProperty_IgnoresTheUnknownProperty()
    {
        // Arrange
        const string ruleSetYaml = $"""
            -
                Name: 'Shared Engineering Standards'
                Id: '{RuleSetYaml.SharedEngineeringStandardsRuleId}'
                DataType: Discrete
                Measures:
                    -
                        Score: 0
                        Description: None
                        Comment: 'No standards'
                    -
                        Score: 1
                        Description: Defined
                        Comment: 'Not referenced by the app'
            """;

        // Act
        IpMaturityMatrixRuleset ruleSet = IpMaturityMatrixRuleset.FromYaml(ruleSetYaml);

        // Assert
        RuleDefinition rule = ruleSet.Rules.ShouldHaveSingleItem();
        rule.Name.ShouldBe("Shared Engineering Standards");
        rule.Id.ShouldBe(Guid.Parse(RuleSetYaml.SharedEngineeringStandardsRuleId));
        rule.Measures.Count.ShouldBe(2);
        rule.Measures.Select(m => m.Score).ShouldBe(new long[] { 0, 1 });
        rule.Measures.Select(m => m.Description).ShouldBe(new[] { "None", "Defined" });
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("!!null")]
    public void FromYaml_NullMeasureEntry_LoadsDefaultMeasureDefinition(string nullMeasure)
    {
        // Arrange
        string ruleSetYaml = $"""
            -
                Name: 'Shared Engineering Standards'
                Id: '{RuleSetYaml.SharedEngineeringStandardsRuleId}'
                DataType: Discrete
                Measures:
                    -
                        Score: 1
                        Description: Configured
                    - {nullMeasure}
            """;

        // Act
        IpMaturityMatrixRuleset ruleSet = IpMaturityMatrixRuleset.FromYaml(ruleSetYaml);

        // Assert
        RuleDefinition rule = ruleSet.Rules.ShouldHaveSingleItem();
        rule.Measures.Count.ShouldBe(2);
        MeasureDefinition emptyMeasure = rule.Measures[1].ShouldBeOfType<MeasureDefinition>();
        emptyMeasure.Score.ShouldBe(0);
    }
}
