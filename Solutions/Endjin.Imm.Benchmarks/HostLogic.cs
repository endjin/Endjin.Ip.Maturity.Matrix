using System.Net;
using System.Text;
using Endjin.Badger;
using Endjin.Imm.Domain;

namespace Endjin.Imm.Benchmarks;

// The helpers below are line-for-line copies of private logic in
// Endjin.Ip.Maturity.Matrix.Host/Imm.cs (GetColourSchemeForPercentage, the total percentage in
// GitHubImmTotalScore, and the body of GitHubImmAllBadges), so that the request pipeline can be
// benchmarked without the Functions host. They are copies, and will drift if Imm.cs changes. The
// next iteration should extract this logic into a testable, public (or InternalsVisibleTo) type in
// the Host and benchmark that instead.
// They match Imm.cs after the colour band fix (exactly 66% and 99.x% are no longer red), the
// showbadges title fix (org/project, HTML-encoded), the zero-maximum guard on the total percentage,
// and the showbadges image URL fix (each path segment and query value URL-encoded, and the whole
// URL HTML-encoded); the Host's behaviour is specified by Endjin.Ip.Maturity.Matrix.Host.Tests.

/// <summary>
/// Copy of <c>Imm.GetColourSchemeForPercentage</c> from the Host.
/// </summary>
internal static class BadgeColour
{
    public static string ForPercentage(decimal percentage)
    {
        return percentage switch
        {
            < 33 => ColorScheme.Red,
            < 66 => ColorScheme.Yellow,
            < 100 => ColorScheme.YellowGreen,
            _ => ColorScheme.Green,
        };
    }
}

/// <summary>
/// Copy of the total percentage calculation in <c>Imm.GitHubImmTotalScore</c> from the Host.
/// </summary>
internal static class TotalScore
{
    public static decimal Percentage(ImmEvaluation evaluationResult)
    {
        // When a project has opted out of every rule there is no maximum to score against, so treat that as 0%
        // rather than dividing by zero.
        return evaluationResult.MaximumPossibleTotalScore == 0
            ? 0M
            : 100M * evaluationResult.TotalScore / evaluationResult.MaximumPossibleTotalScore;
    }
}

/// <summary>
/// Copy of the HTML generation in <c>Imm.GitHubImmAllBadges</c> from the Host.
/// </summary>
internal static class ShowBadgesHtml
{
    public static string Render(string org, string project, string rulesObjectName, string projectObjectName, ImmEvaluation evaluationResult)
    {
        // Each path segment and query value is URL-encoded, and each complete URL is then
        // HTML-encoded (so '&' becomes '&amp;') because it is written into an attribute.
        string badgeUrlPrefix = $"/api/imm/github/{Uri.EscapeDataString(org)}/{Uri.EscapeDataString(project)}/";
        string badgeQueryString =
            $"?cache=false&definitionsBranch={Uri.EscapeDataString(rulesObjectName)}&projectBranch={Uri.EscapeDataString(projectObjectName)}";

        var sb = new StringBuilder();
        using (var sw = new StringWriter(sb))
        {
            sw.WriteLine("<!DOCTYPE html>");
            sw.WriteLine("<html>");
            sw.WriteLine("<head>");
            sw.WriteLine("    <meta charset=\"utf-8\" />");
            sw.Write("    <title>IP Maturity Matrix for ");
            sw.Write(WebUtility.HtmlEncode(org));
            sw.Write("/");
            sw.Write(WebUtility.HtmlEncode(project));
            sw.Write(" (");
            sw.Write(WebUtility.HtmlEncode(projectObjectName));
            if (rulesObjectName != "master")
            {
                sw.Write(", rules: ");
                sw.Write(WebUtility.HtmlEncode(rulesObjectName));
            }
            sw.WriteLine(")</title>");
            sw.WriteLine("<body>");

            sw.WriteLine("  <div>");
            sw.WriteLine("  <p>Total score</p>");

            sw.Write("    <img src=\"");
            sw.Write(WebUtility.HtmlEncode(badgeUrlPrefix + "total" + badgeQueryString));
            sw.WriteLine("\" />");

            sw.WriteLine("  </div>");

            sw.WriteLine("  <div>");
            sw.WriteLine("  <p>Rules</p>");

            sw.WriteLine("  <table>");
            sw.WriteLine("    <tbody>");

            foreach (var evaluation in evaluationResult.RuleEvaluations)
            {
                sw.WriteLine("      <tr>");
                sw.WriteLine("        <td>");
                sw.Write("          <img src=\"");
                sw.Write(WebUtility.HtmlEncode(
                    badgeUrlPrefix + "rule/" + Uri.EscapeDataString(evaluation.RuleAssertion.Id.ToString()) + badgeQueryString));
                sw.WriteLine("\" />");
                sw.WriteLine("        </td>");
                sw.WriteLine("      </tr>");
            }

            sw.WriteLine("    </tbody>");
            sw.WriteLine("  </table>");
            sw.WriteLine("  </div>");
            sw.WriteLine("</body>");
            sw.WriteLine("</html>");
        }

        return sb.ToString();
    }
}
