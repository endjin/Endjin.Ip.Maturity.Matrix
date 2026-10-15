namespace Endjin.Ip.Maturity.Model.Host
{
    #region Using Directives

    using Endjin.Badger;
    using Endjin.Imm.Domain;

    using System;
    using System.Net;
    using System.Text;

    #endregion

    /// <summary>
    /// Renders the content returned by the <see cref="Imm"/> badge endpoints from an evaluated IMM.
    /// </summary>
    /// <remarks>
    /// This is the work each endpoint does once the rule set and IMM have been fetched and evaluated,
    /// separated from the Functions plumbing so that it can be tested and benchmarked directly.
    /// </remarks>
    public static class ImmBadges
    {
        /// <summary>
        /// Gets the badge colour for a score.
        /// </summary>
        /// <param name="percentage">The score, as a percentage of the maximum possible score.</param>
        /// <returns>The <see cref="ColorScheme"/> colour.</returns>
        public static string ColourForPercentage(decimal percentage)
        {
            return percentage switch
            {
                < 33 => ColorScheme.Red,
                < 66 => ColorScheme.Yellow,
                < 100 => ColorScheme.YellowGreen,
                _ => ColorScheme.Green,
            };
        }

        /// <summary>
        /// Gets the total score of an evaluation as a percentage of the maximum possible total score.
        /// </summary>
        /// <param name="evaluationResult">The evaluation.</param>
        /// <returns>The percentage, or 0 when there is no maximum to score against.</returns>
        public static decimal TotalPercentage(ImmEvaluation evaluationResult)
        {
            // When a project has opted out of every rule there is no maximum to score against, so treat that as 0%
            // rather than dividing by zero.
            return evaluationResult.MaximumPossibleTotalScore == 0
                ? 0M
                : 100M * evaluationResult.TotalScore / evaluationResult.MaximumPossibleTotalScore;
        }

        /// <summary>
        /// Renders the total score badge.
        /// </summary>
        /// <param name="evaluationResult">The evaluation.</param>
        /// <returns>The SVG.</returns>
        public static string RenderTotalBadge(ImmEvaluation evaluationResult)
        {
            return BadgePainter.DrawSVG(
                "IMM",
                $"{evaluationResult.TotalScore} / {evaluationResult.MaximumPossibleTotalScore}",
                ColourForPercentage(TotalPercentage(evaluationResult)),
                Style.Flat);
        }

        /// <summary>
        /// Renders the badge for a single rule.
        /// </summary>
        /// <param name="result">The rule's evaluation.</param>
        /// <param name="definitionName">
        /// The rule's name in the rule set, shown when the IMM does not name the rule.
        /// </param>
        /// <returns>The SVG.</returns>
        public static string RenderRuleBadge(RuleEvaluation result, string? definitionName = null)
        {
            return BadgePainter.DrawSVG(
                WebUtility.HtmlEncode(result.RuleAssertion.Name ?? definitionName ?? string.Empty),
                $"{result.Percentage}%",
                ColourForPercentage(result.Percentage),
                Style.Flat);
        }

        /// <summary>
        /// Renders a badge that says why a score could not be shown, in place of the score.
        /// </summary>
        /// <param name="subject">
        /// The text for the subject (left-hand) segment: "IMM" for the total, or the rule's name.
        /// </param>
        /// <param name="message">The text for the status (right-hand) segment, such as "imm.yaml not found".</param>
        /// <returns>The SVG.</returns>
        /// <remarks>
        /// Error badges are always light grey, a colour no score uses (red means a low score).
        /// </remarks>
        public static string RenderErrorBadge(string subject, string message)
        {
            return BadgePainter.DrawSVG(
                WebUtility.HtmlEncode(subject),
                WebUtility.HtmlEncode(message),
                ColorScheme.LightGrey,
                Style.Flat);
        }

        /// <summary>
        /// Renders the page shown by showbadges in place of the badges when they cannot be shown.
        /// </summary>
        /// <param name="org">The GitHub organisation.</param>
        /// <param name="project">The GitHub repository.</param>
        /// <param name="rulesObjectName">The branch or object name the rule definitions were requested from.</param>
        /// <param name="projectObjectName">The branch or object name the project's IMM was requested from.</param>
        /// <param name="message">What went wrong.</param>
        /// <param name="url">The URL that was requested from GitHub, if the problem is with one.</param>
        /// <param name="hint">What to try next, if there is a suggestion.</param>
        /// <returns>The HTML.</returns>
        public static string RenderShowBadgesErrorPage(
            string org,
            string project,
            string rulesObjectName,
            string projectObjectName,
            string message,
            string? url,
            string? hint)
        {
            var sb = new StringBuilder(1024);

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"utf-8\" />");
            sb.Append("    <title>IP Maturity Matrix for ");
            sb.Append(WebUtility.HtmlEncode(org));
            sb.Append('/');
            sb.Append(WebUtility.HtmlEncode(project));
            sb.Append(" (");
            sb.Append(WebUtility.HtmlEncode(projectObjectName));
            if (rulesObjectName != "master")
            {
                sb.Append(", rules: ");
                sb.Append(WebUtility.HtmlEncode(rulesObjectName));
            }

            sb.AppendLine(")</title>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.Append("  <p>");
            sb.Append(WebUtility.HtmlEncode(message));
            sb.AppendLine("</p>");
            if (url is not null)
            {
                sb.Append("  <p>URL requested from GitHub: <code>");
                sb.Append(WebUtility.HtmlEncode(url));
                sb.AppendLine("</code></p>");
            }

            if (hint is not null)
            {
                sb.Append("  <p>");
                sb.Append(WebUtility.HtmlEncode(hint));
                sb.AppendLine("</p>");
            }

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        /// <summary>
        /// Renders the showbadges page: the total score badge, then a badge for each evaluated rule,
        /// each requested from the same branches as the page.
        /// </summary>
        /// <param name="org">The GitHub organisation.</param>
        /// <param name="project">The GitHub repository.</param>
        /// <param name="rulesObjectName">The branch or object name the rule definitions were read from.</param>
        /// <param name="projectObjectName">The branch or object name the project's IMM was read from.</param>
        /// <param name="evaluationResult">The evaluation.</param>
        /// <returns>The HTML.</returns>
        public static string RenderShowBadgesPage(
            string org,
            string project,
            string rulesObjectName,
            string projectObjectName,
            ImmEvaluation evaluationResult)
        {
            // Each path segment and query value is URL-encoded, and each complete URL is then
            // HTML-encoded (so '&' becomes '&amp;') because it is written into an attribute.
            // URL encoding leaves only ASCII letters, digits, '-', '.', '_', '~' and '%' in each encoded
            // value, which HTML encoding does not change, so the prefix and query string can be
            // HTML-encoded once and reused for every badge URL, and a rule id (a GUID) needs neither.
            string badgeUrlPrefix = WebUtility.HtmlEncode(
                $"/api/imm/github/{Uri.EscapeDataString(org)}/{Uri.EscapeDataString(project)}/");
            string badgeQueryString = WebUtility.HtmlEncode(
                $"?cache=false&definitionsBranch={Uri.EscapeDataString(rulesObjectName)}&projectBranch={Uri.EscapeDataString(projectObjectName)}");

            int ruleCount = evaluationResult.RuleEvaluations.Count;
            int badgeUrlLength = badgeUrlPrefix.Length + badgeQueryString.Length + 41;
            var sb = new StringBuilder(640 + org.Length + project.Length + projectObjectName.Length + rulesObjectName.Length + ((ruleCount + 1) * (badgeUrlLength + 110)));

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"utf-8\" />");
            sb.Append("    <title>IP Maturity Matrix for ");
            sb.Append(WebUtility.HtmlEncode(org));
            sb.Append('/');
            sb.Append(WebUtility.HtmlEncode(project));
            sb.Append(" (");
            sb.Append(WebUtility.HtmlEncode(projectObjectName));
            if (rulesObjectName != "master")
            {
                sb.Append(", rules: ");
                sb.Append(WebUtility.HtmlEncode(rulesObjectName));
            }

            sb.AppendLine(")</title>");
            sb.AppendLine("<body>");

            sb.AppendLine("  <div>");
            sb.AppendLine("  <p>Total score</p>");

            sb.Append("    <img src=\"");
            sb.Append(badgeUrlPrefix);
            sb.Append("total");
            sb.Append(badgeQueryString);
            sb.AppendLine("\" />");

            sb.AppendLine("  </div>");

            sb.AppendLine("  <div>");
            sb.AppendLine("  <p>Rules</p>");

            sb.AppendLine("  <table>");
            sb.AppendLine("    <tbody>");

            foreach (RuleEvaluation evaluation in evaluationResult.RuleEvaluations)
            {
                sb.AppendLine("      <tr>");
                sb.AppendLine("        <td>");
                sb.Append("          <img src=\"");
                sb.Append(badgeUrlPrefix);
                sb.Append("rule/");
                sb.Append($"{evaluation.RuleAssertion.Id}");
                sb.Append(badgeQueryString);
                sb.AppendLine("\" />");
                sb.AppendLine("        </td>");
                sb.AppendLine("      </tr>");
            }

            sb.AppendLine("    </tbody>");
            sb.AppendLine("  </table>");
            sb.AppendLine("  </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}
