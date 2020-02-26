namespace Endjin.Imm.App
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    using Endjin.Badger;
    using Endjin.Imm.Domain;
    using Endjin.Imm.Processing;
    using Endjin.Imm.Repository;

    public static class Program
    {
        private static async Task Main()
        {
            var client = new HttpClient();
            HttpResponseMessage ruleSetResponse = await client.GetAsync(IpMaturityMatrixRuleset.RuleSetDefinitionsUrl).ConfigureAwait(false);
            ruleSetResponse.EnsureSuccessStatusCode();
            string yamlRuleText = await ruleSetResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            var yamlRules = IpMaturityMatrixRuleset.FromYaml(yamlRuleText);
            var immProjectScore = IpMaturityMatrix.FromYaml(File.ReadAllText("imm-default.yaml"));

            var evaluationEngine = new EvaluationEngine(new RuleDefinitionRepository(yamlRules));

            var col1 = new StringBuilder();
            var col2 = new StringBuilder();

            ImmEvaluation evalutionResult = evaluationEngine.Evaluate(immProjectScore);
            foreach (var result in evalutionResult.RuleEvaluations)
            {
                Console.WriteLine($"{result.RuleAssertion.Name} {result.Percentage}% Score: {result.Score}");

#pragma warning disable RCS1197 // Optimize StringBuilder.Append/AppendLine call.
                col1.AppendLine($"<tspan x='30' dy='1.5em'>{WebUtility.HtmlEncode(result.RuleAssertion.Name)}</tspan>");
                col2.AppendLine($"<tspan x='310' dy='1.5em'>{result.Percentage}%</tspan>");
#pragma warning restore RCS1197 // Optimize StringBuilder.Append/AppendLine call.

                File.WriteAllText($"imm-{result.RuleAssertion.Id}.svg", BadgePainter.DrawSVG(WebUtility.HtmlEncode(result.RuleAssertion.Name!), $"{result.Percentage}%", ColorScheme.Red, Style.FlatSquare));
            }

            Console.WriteLine($"{evalutionResult.TotalScore} / {evalutionResult.MaximumPossibleTotalScore}");

            File.WriteAllText("imm.svg", BadgePainter.DrawSVG("IMM", $"{evalutionResult.TotalScore} / {evalutionResult.MaximumPossibleTotalScore}", ColorScheme.Red, Style.Flat));
            File.WriteAllText("imm-table.svg", string.Format(Resources.Table, col1.ToString(), col2.ToString()));
        }
    }
}