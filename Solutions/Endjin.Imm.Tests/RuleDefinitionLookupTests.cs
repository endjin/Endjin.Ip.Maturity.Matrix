using Endjin.Imm.Domain;
using Endjin.Imm.Repository;
using Shouldly;

namespace Endjin.Imm.Tests;

/// <summary>
/// Each rule in an IP Maturity Model is matched to its definition in the rule set by its id.
/// </summary>
[TestClass]
public class RuleDefinitionLookupTests
{
    private static readonly Guid RuleId = Guid.Parse("0f6b1d0e-5b6e-4a43-9a43-2f1d7c1d2a01");
    private static readonly Guid OtherRuleId = Guid.Parse("7d0a7e55-8f2b-4d0e-8a0c-6e1e5c3d9b02");

    [TestMethod]
    public void GetDefinitionFor_KnownRuleId_ReturnsItsDefinition()
    {
        // Arrange
        RuleDefinition other = new() { Id = OtherRuleId, Name = "Other" };
        RuleDefinition expected = new() { Id = RuleId, Name = "Expected" };
        RuleDefinitionRepository repository = new(RuleSetOf(other, expected));

        // Act
        RuleDefinition definition = repository.GetDefinitionFor(new RuleAssertion { Id = RuleId });

        // Assert
        definition.ShouldBeSameAs(expected);
    }

    [TestMethod]
    public void GetDefinitionFor_DuplicateRuleIds_ReturnsTheFirstDefinition()
    {
        // Arrange
        RuleDefinition first = new() { Id = RuleId, Name = "First" };
        RuleDefinition second = new() { Id = RuleId, Name = "Second" };
        RuleDefinitionRepository repository = new(RuleSetOf(first, second));

        // Act
        RuleDefinition definition = repository.GetDefinitionFor(new RuleAssertion { Id = RuleId });

        // Assert
        definition.ShouldBeSameAs(first);
    }

    [TestMethod]
    public void GetDefinitionFor_UnknownRuleId_ThrowsUnknownRuleException()
    {
        // Arrange
        RuleDefinitionRepository repository = new(RuleSetOf(new RuleDefinition { Id = OtherRuleId }));

        // Act
        UnknownRuleException exception = Should.Throw<UnknownRuleException>(
            () => repository.GetDefinitionFor(new RuleAssertion { Id = RuleId }));

        // Assert
        exception.RuleId.ShouldBe(RuleId);
        exception.Message.ShouldBe($"No definition found for rule id {RuleId}");
        exception.ShouldBeAssignableTo<ArgumentException>();
    }

    [TestMethod]
    public void GetDefinitionFor_RuleSetWithNoRules_ThrowsUnknownRuleExceptionNamingTheRuleId()
    {
        // Arrange
        RuleDefinitionRepository repository = new(RuleSetOf());

        // Act
        UnknownRuleException exception = Should.Throw<UnknownRuleException>(
            () => repository.GetDefinitionFor(new RuleAssertion { Id = RuleId }));

        // Assert
        exception.RuleId.ShouldBe(RuleId);
        exception.Message.ShouldBe($"No definition found for rule id {RuleId}");
    }

    [TestMethod]
    public void GetDefinitionFor_RuleDefinedBeforeANullEntry_ReturnsItsDefinition()
    {
        // Arrange
        RuleDefinition expected = new() { Id = RuleId };
        RuleDefinitionRepository repository = new(RuleSetOf(expected, null!, new RuleDefinition { Id = OtherRuleId }));

        // Act
        RuleDefinition definition = repository.GetDefinitionFor(new RuleAssertion { Id = RuleId });

        // Assert
        definition.ShouldBeSameAs(expected);
    }

    [TestMethod]
    public void GetDefinitionFor_RuleDefinedAfterANullEntry_ThrowsNullReferenceExceptionAsTheLinearSearchDid()
    {
        // Arrange
        RuleDefinitionRepository repository = new(RuleSetOf(new RuleDefinition { Id = OtherRuleId }, null!, new RuleDefinition { Id = RuleId }));

        // Act / Assert
        Should.Throw<NullReferenceException>(() => repository.GetDefinitionFor(new RuleAssertion { Id = RuleId }));
    }

    [TestMethod]
    public void GetAll_Always_ReturnsTheRuleSetsRules()
    {
        // Arrange
        IpMaturityModelRuleset ruleSet = RuleSetOf(new RuleDefinition { Id = RuleId });
        RuleDefinitionRepository repository = new(ruleSet);

        // Act
        IList<RuleDefinition> all = repository.GetAll();

        // Assert
        all.ShouldBeSameAs(ruleSet.Rules);
    }

    private static IpMaturityModelRuleset RuleSetOf(params RuleDefinition[] definitions)
    {
        IpMaturityModelRuleset ruleSet = new();
        foreach (RuleDefinition definition in definitions)
        {
            ruleSet.Rules.Add(definition);
        }

        return ruleSet;
    }
}
