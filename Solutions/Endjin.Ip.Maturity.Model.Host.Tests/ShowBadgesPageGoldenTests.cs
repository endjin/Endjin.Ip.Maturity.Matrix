using Endjin.Ip.Maturity.Model.Host.Tests.TestData;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace Endjin.Ip.Maturity.Model.Host.Tests;

/// <summary>
/// The showbadges page is generated text, so these tests pin down every character of it: any
/// change to how the page is built (for example to make it cheaper to produce) must leave the HTML
/// sent to the browser exactly as it was. Lines end with the platform's newline sequence.
/// </summary>
[TestClass]
public class ShowBadgesPageGoldenTests
{
    [TestMethod]
    public async Task GitHubImmAllBadges_TwoRuleSet_RendersExactlyTheExpectedHtml()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        string expected = $"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8" />
                <title>IP Maturity Matrix for endjin/Endjin.Ip.Maturity.Model (master)</title>
            <body>
              <div>
              <p>Total score</p>
                <img src="/api/imm/github/endjin/Endjin.Ip.Maturity.Model/total?cache=false&amp;definitionsBranch=master&amp;projectBranch=master" />
              </div>
              <div>
              <p>Rules</p>
              <table>
                <tbody>
                  <tr>
                    <td>
                      <img src="/api/imm/github/endjin/Endjin.Ip.Maturity.Model/rule/{HostYaml.DeploymentRuleId}?cache=false&amp;definitionsBranch=master&amp;projectBranch=master" />
                    </td>
                  </tr>
                  <tr>
                    <td>
                      <img src="/api/imm/github/endjin/Endjin.Ip.Maturity.Model/rule/{HostYaml.SharedEngineeringStandardsRuleId}?cache=false&amp;definitionsBranch=master&amp;projectBranch=master" />
                    </td>
                  </tr>
                </tbody>
              </table>
              </div>
            </body>
            </html>

            """.ReplaceLineEndings(Environment.NewLine);

        // Act
        IActionResult result = await fixture.Functions.GitHubImmAllBadges(
            ImmFunctionFixture.CreateRequest(), ImmFunctionFixture.Org, ImmFunctionFixture.Project);

        // Assert
        result.ShouldBeOfType<ContentResult>().Content.ShouldBe(expected);
    }

    [TestMethod]
    public async Task GitHubImmAllBadges_BranchNeedingEncoding_RendersExactlyTheExpectedHtml()
    {
        // Arrange
        ImmFunctionFixture fixture = new(HostYaml.TwoRuleSet, HostYaml.BothRulesScored);
        string expected = $"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8" />
                <title>IP Maturity Matrix for endjin/Endjin.Ip.Maturity.Model (x&lt;y, rules: a&amp;b)</title>
            <body>
              <div>
              <p>Total score</p>
                <img src="/api/imm/github/endjin/Endjin.Ip.Maturity.Model/total?cache=false&amp;definitionsBranch=a%26b&amp;projectBranch=x%3Cy" />
              </div>
              <div>
              <p>Rules</p>
              <table>
                <tbody>
                  <tr>
                    <td>
                      <img src="/api/imm/github/endjin/Endjin.Ip.Maturity.Model/rule/{HostYaml.DeploymentRuleId}?cache=false&amp;definitionsBranch=a%26b&amp;projectBranch=x%3Cy" />
                    </td>
                  </tr>
                  <tr>
                    <td>
                      <img src="/api/imm/github/endjin/Endjin.Ip.Maturity.Model/rule/{HostYaml.SharedEngineeringStandardsRuleId}?cache=false&amp;definitionsBranch=a%26b&amp;projectBranch=x%3Cy" />
                    </td>
                  </tr>
                </tbody>
              </table>
              </div>
            </body>
            </html>

            """.ReplaceLineEndings(Environment.NewLine);

        // Act
        IActionResult result = await fixture.Functions.GitHubImmAllBadges(
            ImmFunctionFixture.CreateRequest("?definitionsBranch=a%26b&projectBranch=x%3Cy"),
            ImmFunctionFixture.Org,
            ImmFunctionFixture.Project);

        // Assert
        result.ShouldBeOfType<ContentResult>().Content.ShouldBe(expected);
    }
}
