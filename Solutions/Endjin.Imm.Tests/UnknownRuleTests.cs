using Endjin.Imm.Domain;
using Endjin.Imm.Tests.TestData;
using Shouldly;

namespace Endjin.Imm.Tests;

/// <summary>
/// An IP Maturity Model can only be evaluated against rules that the rule set defines, so a
/// rule id that the rule set does not know about is reported rather than silently scored.
/// </summary>
[TestClass]
public class UnknownRuleTests
{
    [TestMethod]
    public void Evaluate_ImmReferencesARuleIdNotInTheRuleSet_ThrowsUnknownRuleExceptionNamingTheRuleId()
    {
        // Arrange
        const string unknownRuleId = "0b2c6a3e-58a4-4d2c-9a55-5c1f0f4f8f11";
        const string immYaml = $"""
            -
                Name: 'Shared Engineering Standards'
                Id: '{RuleSetYaml.SharedEngineeringStandardsRuleId}'
                Measures:
                    -
                        Score: 1
                        Description: Configured
            -
                Name: 'A rule from a newer rule set'
                Id: '{unknownRuleId}'
                Measures:
                    -
                        Score: 1
            """;

        // Act
        UnknownRuleException exception = Should.Throw<UnknownRuleException>(
            () => ImmEvaluator.Evaluate(RuleSetYaml.DeploymentWithOptionalMeasure, immYaml));

        // Assert
        exception.RuleId.ShouldBe(Guid.Parse(unknownRuleId));
        exception.Message.ShouldBe($"No definition found for rule id {unknownRuleId}");
    }
}
