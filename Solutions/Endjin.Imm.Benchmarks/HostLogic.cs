using System.Net;
using System.Text;
using Endjin.Badger;
using Endjin.Imm.Domain;

namespace Endjin.Imm.Benchmarks;

// The two helpers below are line-for-line copies of private logic in
// Endjin.Ip.Maturity.Matrix.Host/Imm.cs (GetColourSchemeForPercentage and the body of
// GitHubImmAllBadges), so that the request pipeline can be benchmarked without the Functions
// host. They are copies, and will drift if Imm.cs changes. The next iteration should extract this
// logic into a testable, public (or InternalsVisibleTo) type in the Host and benchmark that instead.
// Known quirks are preserved deliberately: the title writes the org twice, and the colour switch
// maps exactly 66% and anything from 99% to just under 100% to red.

/// <summary>
/// Copy of <c>Imm.GetColourSchemeForPercentage</c> from the Host.
/// </summary>
internal static class BadgeColour
{
    public static string ForPercentage(decimal percentage)
    {
        return percentage switch
        {
            var _ when percentage < 33 => ColorScheme.Red,
            var _ when percentage < 66 => ColorScheme.Yellow,
            var _ when percentage > 66
                    && percentage < 99 => ColorScheme.YellowGreen,
            100 => ColorScheme.Green,
            _ => ColorScheme.Red,
        };
    }
}

/// <summary>
/// Copy of the HTML generation in <c>Imm.GitHubImmAllBadges</c> from the Host.
/// </summary>
internal static class ShowBadgesHtml
{
    public static string Render(string org, string project, string rulesObjectName, string projectObjectName, ImmEvaluation evaluationResult)
    {
        string encodedRulesObjectName = WebUtility.UrlEncode(rulesObjectName);
        string encodedProjectObjectName = WebUtility.UrlEncode(projectObjectName);

        var sb = new StringBuilder();
        using (var sw = new StringWriter(sb))
        {
            sw.WriteLine("<!DOCTYPE html>");
            sw.WriteLine("<html>");
            sw.WriteLine("<head>");
            sw.WriteLine("    <meta charset=\"utf-8\" />");
            sw.Write("    <title>IP Maturity Matrix for");
            sw.Write(org);
            sw.Write("/");
            sw.Write(org);
            sw.Write(" (");
            sw.Write(projectObjectName);
            if (rulesObjectName != "master")
            {
                sw.Write(", rules: ");
                sw.Write(WebUtility.HtmlEncode(rulesObjectName));
            }
            sw.WriteLine(")</title>");
            sw.WriteLine("<body>");

            sw.WriteLine("  <div>");
            sw.WriteLine("  <p>Total score</p>");

            sw.Write("    <img src=\"/api/imm/github/");
            sw.Write(org);
            sw.Write("/");
            sw.Write(project);
            sw.Write("/total");
            sw.Write("?cache=false&definitionsBranch=");
            sw.Write(encodedRulesObjectName);
            sw.Write("&projectBranch=");
            sw.Write(encodedProjectObjectName);
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
                sw.Write("          <img src=\"/api/imm/github/");
                sw.Write(org);
                sw.Write("/");
                sw.Write(project);
                sw.Write("/rule/");
                sw.Write(evaluation.RuleAssertion.Id);
                sw.Write("?cache=false&definitionsBranch=");
                sw.Write(encodedRulesObjectName);
                sw.Write("&projectBranch=");
                sw.Write(encodedProjectObjectName);
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
