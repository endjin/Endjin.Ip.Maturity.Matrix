using Endjin.Imm.Contracts;
using Endjin.Imm.Domain;
using Endjin.Imm.Repository;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Endjin.Ip.Maturity.Model.Host.Tests.TestData;

/// <summary>
/// The badge functions with GitHub replaced by substitute sources that return the given rule set
/// and IMM, whichever branches are asked for. The rule set and IMM are real, so the functions
/// evaluate them exactly as they would content fetched from GitHub.
/// </summary>
internal sealed class ImmFunctionFixture
{
    public const string Org = "endjin";

    public const string Project = "Endjin.Ip.Maturity.Model";

    public ImmFunctionFixture(string ruleSetYaml, string immYaml)
    {
        this.RuleDefinitionSource
            .GetRuleDefinitionRepositoryAsync(Arg.Any<string>())
            .Returns(_ => new RuleDefinitionRepository(IpMaturityModelRuleset.FromYaml(ruleSetYaml)));
        this.ImmSource
            .GetIpMaturityModelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(_ => IpMaturityModel.FromYaml(immYaml));

        this.Functions = new Imm(this.RuleDefinitionSource, this.ImmSource, this.Logger);
    }

    /// <summary>
    /// Gets the logger the functions write to.
    /// </summary>
    public CapturingLogger<Imm> Logger { get; } = new();

    /// <summary>
    /// Gets the substitute for the rule definitions published on GitHub.
    /// </summary>
    public IRuleDefinitionRepositorySource RuleDefinitionSource { get; } = Substitute.For<IRuleDefinitionRepositorySource>();

    /// <summary>
    /// Gets the substitute for the project IMMs published on GitHub.
    /// </summary>
    public IIpMaturityModelSource ImmSource { get; } = Substitute.For<IIpMaturityModelSource>();

    /// <summary>
    /// Gets the functions under test.
    /// </summary>
    public Imm Functions { get; }

    /// <summary>
    /// Makes fetching the rule set fail, whichever branch is asked for.
    /// </summary>
    /// <param name="exception">The exception the fetch fails with.</param>
    public void RuleSetFailsWith(Exception exception) =>
        this.RuleDefinitionSource
            .GetRuleDefinitionRepositoryAsync(Arg.Any<string>())
            .Returns(_ => Task.FromException<IRuleDefinitionRepository>(exception));

    /// <summary>
    /// Makes fetching the IMM fail, whichever project and branch are asked for.
    /// </summary>
    /// <param name="exception">The exception the fetch fails with.</param>
    public void ImmFailsWith(Exception exception) =>
        this.ImmSource
            .GetIpMaturityModelAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(_ => Task.FromException<IpMaturityModel>(exception));

    /// <summary>
    /// Creates an HTTP request, as the Functions host would pass to a function.
    /// </summary>
    /// <param name="queryString">The query string, including the leading '?', or empty.</param>
    public static HttpRequest CreateRequest(string queryString = "")
    {
        DefaultHttpContext context = new();
        context.Request.QueryString = new QueryString(queryString);
        return context.Request;
    }
}
