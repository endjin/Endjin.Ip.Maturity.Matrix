using Endjin.Imm.Contracts;
using Endjin.Imm.Domain;
using Endjin.Imm.Tests.TestData;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Endjin.Imm.Tests;

/// <summary>
/// A project's IMM and the rule definitions are fetched from the right place on GitHub, for the
/// requested branch, so that badges reflect the content that the project and the rule set
/// maintainers have published.
/// </summary>
[TestClass]
public class GitHubSourceLocationTests
{
    private static readonly TimeSpan OneHour = TimeSpan.FromHours(1);

    [TestMethod]
    [DataRow("endjin", "Endjin.Ip.Maturity.Model", "master")]
    [DataRow("corvus-dotnet", "Corvus.Monitoring", "main")]
    [DataRow("endjin", "Endjin.Ip.Maturity.Model", "feature/dotnet-10-modernisation")]
    [DataRow("endjin", "Endjin.Ip.Maturity.Model", "v1.2.3")]
    public async Task GetIpMaturityModelAsync_ForProjectAndBranch_FetchesImmYamlFromTheRawGitHubContentOfThatBranch(
        string owner,
        string project,
        string branch)
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingWith(
            () => StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured));

        // Act
        IpMaturityModel imm = await fixture.ImmSource.GetIpMaturityModelAsync(owner, project, branch);

        // Assert
        CapturedRequest request = fixture.Handler.Requests.ShouldHaveSingleItem();
        request.Uri.ShouldBe(new Uri($"https://raw.githubusercontent.com/{owner}/{project}/{branch}/imm.yaml"));
        RuleAssertion rule = imm.Rules.ShouldHaveSingleItem();
        rule.Id.ShouldBe(Guid.Parse(RuleSetYaml.SharedEngineeringStandardsRuleId));
        rule.Measures.ShouldHaveSingleItem().Score.ShouldBe(1);
    }

    [TestMethod]
    [DataRow("master")]
    [DataRow("v2")]
    [DataRow("feature/new-rules")]
    public async Task GetRuleDefinitionRepositoryAsync_ForBranch_FetchesRuleSetYamlFromTheRuleDefinitionsRepository(string branch)
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingWith(
            () => StubHttpMessageHandler.Ok(RuleSetYaml.DeploymentWithOptionalMeasure));

        // Act
        IRuleDefinitionRepository repository = await fixture.RuleDefinitionSource.GetRuleDefinitionRepositoryAsync(branch);

        // Assert
        CapturedRequest request = fixture.Handler.Requests.ShouldHaveSingleItem();
        request.Uri.ShouldBe(new Uri($"https://raw.githubusercontent.com/endjin/Endjin.Ip.Maturity.Matrix.RuleDefinitions/{branch}/RuleSet.yaml"));
        repository.GetAll().Select(r => r.Name).ShouldBe(["Deployment", "Shared Engineering Standards"]);
    }

    [TestMethod]
    public async Task GetRuleDefinitionRepositoryAsync_FetchedRuleSet_ResolvesTheDefinitionForAnAssertedRule()
    {
        // Arrange
        using ImmSourceFixture fixture = ImmSourceFixture.RespondingWith(
            () => StubHttpMessageHandler.Ok(RuleSetYaml.DeploymentWithOptionalMeasure));
        RuleAssertion assertion = IpMaturityModel.FromYaml(ImmYaml.SharedEngineeringStandardsConfigured).Rules.Single();

        // Act
        IRuleDefinitionRepository repository = await fixture.RuleDefinitionSource.GetRuleDefinitionRepositoryAsync("master");
        RuleDefinition definition = repository.GetDefinitionFor(assertion);

        // Assert
        definition.Name.ShouldBe("Shared Engineering Standards");
        definition.DataType.ShouldBe(DataType.Discrete);
        definition.Measures.Select(m => m.Description).ShouldBe(["None", "Configured"]);
    }

    [TestMethod]
    public async Task GetIpMaturityModelAsync_DifferentBranches_AreFetchedAndCachedIndependently()
    {
        // Arrange
        using ImmSourceFixture fixture = new((request, _) => Task.FromResult(
            request.RequestUri!.AbsolutePath.Contains("/release/", StringComparison.Ordinal)
                ? StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsNone, maxAge: OneHour)
                : StubHttpMessageHandler.Ok(ImmYaml.SharedEngineeringStandardsConfigured, maxAge: OneHour)));

        // Act
        IpMaturityModel master = await fixture.ImmSource.GetIpMaturityModelAsync("endjin", "Project", "master");
        IpMaturityModel release = await fixture.ImmSource.GetIpMaturityModelAsync("endjin", "Project", "release");
        IpMaturityModel masterAgain = await fixture.ImmSource.GetIpMaturityModelAsync("endjin", "Project", "master");
        IpMaturityModel releaseAgain = await fixture.ImmSource.GetIpMaturityModelAsync("endjin", "Project", "release");

        // Assert
        fixture.Handler.Requests.Select(r => r.Uri.AbsolutePath).ShouldBe(
        [
            "/endjin/Project/master/imm.yaml",
            "/endjin/Project/release/imm.yaml",
        ]);
        master.Rules.Single().Measures.Single().Score.ShouldBe(1);
        release.Rules.Single().Measures.Single().Score.ShouldBe(0);
        masterAgain.ShouldBeSameAs(master);
        releaseAgain.ShouldBeSameAs(release);
    }

    [TestMethod]
    public void AddImmSources_CalledTwice_RegistersEachSourceOnce()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddImmSources();
        services.AddImmSources();

        // Assert
        services.Count(s => s.ServiceType == typeof(IIpMaturityModelSource)).ShouldBe(1);
        services.Count(s => s.ServiceType == typeof(IRuleDefinitionRepositorySource)).ShouldBe(1);
    }
}
