namespace Endjin.Imm.App
{
    using Endjin.Imm.Domain;
    using Endjin.Imm.Processing;

    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    public static class Program
    {
        static async Task Main(string[] args)
        {
            var client = new HttpClient();
            HttpResponseMessage ruleSetResponse = await client.GetAsync(IpMaturityMatrixRuleset.RuleSetDefinitionsUrl).ConfigureAwait(false);
            ruleSetResponse.EnsureSuccessStatusCode();
            string yamlRuleText = await ruleSetResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            var yamlRules = IpMaturityMatrixRuleset.FromYaml(yamlRuleText);
            var immProjectScore = IpMaturityMatrix.FromYaml(File.ReadAllText("imm-default.yaml"));

            var evaluationEngine = new EvaluationEngine(yamlRules);
            long totalScore = 0;

            var col1 = new StringBuilder();
            var col2 = new StringBuilder();
                        
            foreach (var result in evaluationEngine.Evaluate(immProjectScore))
            {
                Console.WriteLine($"{result.Rule.Name} {result.Percentage}% Score: {result.Score}");

                col1.AppendLine($"<tspan x='30' dy='1.5em'>{WebUtility.HtmlEncode(result.Rule.Name)}</tspan>");
                col2.AppendLine($"<tspan x='310' dy='1.5em'>{result.Percentage}%</tspan>");

                totalScore += result.Score;

                //File.WriteAllText($"imm-{result.Rule.Id}.svg", new BadgePainter().DrawSVG(WebUtility.HtmlEncode(result.Rule.Name), $"{result.Percentage}%", ColorScheme.Red, Style.FlatSquare));
            }

            Console.WriteLine($"{totalScore} / {evaluationEngine.MaximumScore()}");

            File.WriteAllText(@"imm.svg", new BadgePainter().DrawSVG("IMM", $"{totalScore} / {evaluationEngine.MaximumScore()}", ColorScheme.Red, Style.Flat));
            File.WriteAllText(@"imm-table.svg", string.Format(Resources.Table, col1.ToString(), col2.ToString()));
        }
    }
}