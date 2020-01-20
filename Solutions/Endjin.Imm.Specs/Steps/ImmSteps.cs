using Endjin.Imm.Contracts;
using Endjin.Imm.Domain;
using Endjin.Imm.Processing;
using NodaTime;
using NodaTime.Text;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TechTalk.SpecFlow;

namespace Endjin.Imm.Specs.Steps
{
    [Binding]
    public class ImmSteps
    {
        private readonly TestEvaluationContext evaluationContext = new TestEvaluationContext();
        private readonly StringBuilder ruleSetText = new StringBuilder();
        private readonly StringBuilder immText = new StringBuilder();
        private readonly Dictionary<string, Table> ruleDefinitionTables = new Dictionary<string, Table>();
        private IpMaturityMatrixRuleset? ruleSet;
        private IpMaturityMatrix? imm;

        private ImmEvaluation? evaluationResult;
        private Dictionary<Guid, RuleEvaluation>? ruleEvaluations;

        public void AppendImmLine(string line) => this.immText.AppendLine(line);

        private RuleDefinition RuleDefinition => this.ruleSet!.Rules.Single();
        private Table RuleDefinitionTable => this.ruleDefinitionTables.Values.Single();

        [Given("I have a rule named '(.*)' with id '(.*)' and DataType '(.*)'")]
        public void GivenIHaveARuleNamedWithIdAndDataType(string ruleName, string id, string dataType, Table table)
        {
            this.AddRule(ruleName, id, dataType, table, isOptional: false);
        }

        [Given("I have an optional rule named '(.*)' with id '(.*)' and DataType '(.*)'")]
        public void GivenIHaveAnOptionalRuleNamedWithIdAndDataType(string ruleName, string id, string dataType, Table table)
        {
            this.AddRule(ruleName, id, dataType, table, isOptional: true);
        }

        [Given("the reference date for evaluation purposes is '(.*)'")]
        public void GivenTheReferenceDateForEvaluationPurposesIs(string date)
        {
            this.evaluationContext.EvaluationReferenceDate = LocalDatePattern.Iso.Parse(date).Value;
        }

        [Given("my IMM does not have the relevant rule")]
        public void GivenMyImmDoesNotHaveTheRelevantRule()
        {
            // We don't actually need to do anything.
        }

        [Given(@"my IMM has an entry named '(.*)' with id '(.*)' with a Score of ([\d]*) and description of '(.*)'")]
        public void GivenMyImmHasAnEntryNamedWithIdWithAScoreOf(string name, string id, int score, string description)
        {
            this.AddImmEntry(name, id, new { Score = score, Description = description });
        }

        [Given("my IMM has an entry named '(.*)' with id '(.*)' and these measures")]
        public void GivenMyIMMHasAnEntryNamedWithId(string name, string id, Table table)
        {
            this.AddImmEntry(name, id, table);
        }

        [Given("my IMM has opted out of the entry named '(.*)' with id '(.*)'")]
        public void GivenMyIMMHasOptedOutOfTheEntryNamedWithId(string name, string id)
        {
            this.immText.AppendLine($"-");
            this.immText.AppendLine($"    Name: '{name}'");
            this.immText.AppendLine($"    Id: '{id}'");
            this.immText.AppendLine($"    OptOut: true");
        }

        [Given("I load the rules")]
        [When("I load the rules")]
        public void WhenILoadTheRules()
        {
            this.ruleSet = IpMaturityMatrixRuleset.FromYaml(this.ruleSetText.ToString());
        }

        [When("I evaluate the IMM")]
        public void WhenIEvaluateTheImm()
        {
            this.imm = IpMaturityMatrix.FromYaml(this.immText.ToString());

            var evaluationEngine = new EvaluationEngine(this.ruleSet!);
            this.evaluationResult = evaluationEngine.Evaluate(this.imm, this.evaluationContext);
            this.ruleEvaluations = this.evaluationResult.RuleEvaluations.ToDictionary(r => r.Rule.Id);
        }

        [Then("the rule definition name should be '(.*)'")]
        public void ThenTheRuleNameShouldBe(string name)
        {
            Assert.AreEqual(name, this.RuleDefinition.Name);
        }

        [Then("the rule definition id should be '(.*)'")]
        public void ThenTheRuleIdShouldBe(Guid id)
        {
            Assert.AreEqual(id, this.RuleDefinition.Id);
        }

        [Then("the rule definition should have the same number of measures as were in the YAML")]
        public void ThenTheRuleDefinitionShouldHaveTheSameNumberOfMeasuresAsWereInTheYAML()
        {
            Assert.AreEqual(this.RuleDefinitionTable!.RowCount, this.RuleDefinition.Measures.Count);
        }

        [Then("all the rule definition measure definitions with Framework property should be of type '(.*)'")]
        public void ThenAllTheRuleDefinitionMeasureDefinitionsShouldBeOfType(string typeName)
        {
            for (int i = 0; i < this.RuleDefinitionTable!.RowCount; ++i)
            {
                TableRow tableRow = this.RuleDefinitionTable!.Rows[i];
                MeasureDefinition measureDefinition = this.RuleDefinition.Measures[i];

                if (!string.IsNullOrWhiteSpace(tableRow["Framework"]))
                {
                    Assert.AreEqual(typeName, measureDefinition.GetType().FullName, measureDefinition.Description);
                }
            }
        }

        [Then("all the rule definition measure definitions with Age property should be of type '(.*)'")]
        public void ThenAllTheRuleDefinitionMeasureDefinitionsWithAgePropertyShouldBeOfType(string typeName)
        {
            for (int i = 0; i < this.RuleDefinitionTable!.RowCount; ++i)
            {
                TableRow tableRow = this.RuleDefinitionTable!.Rows[i];
                MeasureDefinition measureDefinition = this.RuleDefinition.Measures[i];

                if (!string.IsNullOrWhiteSpace(tableRow["Age"]))
                {
                    Assert.AreEqual(typeName, measureDefinition.GetType().FullName, measureDefinition.Description);
                }
            }
        }

        [Then("all the rule definition measure definition scores should match the scores in the YAML")]
        public void ThenAllTheRuleDefinitionMeasureDefinitionScoresShouldMatchTheScoresInTheYAML()
        {
            for (int i = 0; i < this.RuleDefinitionTable!.RowCount; ++i)
            {
                TableRow tableRow = this.RuleDefinitionTable!.Rows[i];
                MeasureDefinition measureDefinition = this.RuleDefinition.Measures[i];

                int scoreInYaml = Convert.ToInt32(tableRow["Score"]);
                Assert.AreEqual(scoreInYaml, measureDefinition.Score);
            }
        }

        [Then("all the rule definition measure definition descriptions should match the descriptions in the YAML")]
        public void ThenAllTheRuleDefinitionMeasureDefinitionDescriptionsShouldMatchTheScoresInTheYAML()
        {
            for (int i = 0; i < this.RuleDefinitionTable!.RowCount; ++i)
            {
                TableRow tableRow = this.RuleDefinitionTable!.Rows[i];
                MeasureDefinition measureDefinition = this.RuleDefinition.Measures[i];

                string descriptionInYaml = tableRow["Description"];
                Assert.AreEqual(descriptionInYaml, measureDefinition.Description);
            }
        }

        [Then("all the rule definition measure definitions with Framework property frameworks should match the frameworks in the YAML")]
        public void ThenAllTheRuleDefinitionMeasureDefinitionFrameworksShouldMatchTheScoresInTheYAML()
        {
            for (int i = 0; i < this.RuleDefinitionTable!.RowCount; ++i)
            {
                TableRow tableRow = this.RuleDefinitionTable!.Rows[i];
                if (!string.IsNullOrWhiteSpace(tableRow["Framework"]))
                {
                    var measureDefinition = (FrameworkMeasureDefinition)this.RuleDefinition.Measures[i];

                    string frameworkInYaml = tableRow["Framework"].Trim('\'');
                    Assert.AreEqual(frameworkInYaml, measureDefinition.Framework);
                }
            }
        }

        [Then("all the rule definition measure definitions with Age property frameworks should match the frameworks in the YAML")]
        public void ThenAllTheRuleDefinitionMeasureDefinitionsWithAgePropertyFrameworksShouldMatchTheFrameworksInTheYAML()
        {
            for (int i = 0; i < this.RuleDefinitionTable!.RowCount; ++i)
            {
                TableRow tableRow = this.RuleDefinitionTable!.Rows[i];
                if (!string.IsNullOrWhiteSpace(tableRow["Age"]))
                {
                    var measureDefinition = (AgeMeasureDefinition)this.RuleDefinition.Measures[i];

                    string ageInYaml = tableRow["Age"].Trim('\'');
                    Assert.AreEqual(ageInYaml, measureDefinition.Age);
                }
            }
        }

        [Then("the score for the '(.*)' rule should be (.*)")]
        public void ThenTheScoreForTheRuleShouldBe(Guid id, int score)
        {
            Assert.AreEqual(score, this.ruleEvaluations![id].Score);
        }

        [Then("the percentage for the '(.*)' rule should be (.*)")]
        public void ThenThePercentageForTheRuleShouldBe(Guid id, decimal percentage)
        {
            Assert.AreEqual(percentage, this.ruleEvaluations![id].Percentage);
        }

        [Then("the evalution total score should be (.*)")]
        public void ThenTheEvalutionTotalScoreShouldBe(int totalScore)
        {
            Assert.AreEqual(totalScore, this.evaluationResult!.TotalScore);
        }

        [Then("the evalution maximum possible score should be (.*)")]
        public void ThenTheEvalutionMaximumPossibleScoreShouldBe(int maximumPossibleTotalScore)
        {
            Assert.AreEqual(maximumPossibleTotalScore, this.evaluationResult!.MaximumPossibleTotalScore);
        }

        private void AddRule(string ruleName, string id, string dataType, Table table, bool isOptional)
        {
            this.ruleDefinitionTables.Add(ruleName, table);

            this.ruleSetText.AppendLine($"-");
            this.ruleSetText.AppendLine($"    Name: '{ruleName}'");
            this.ruleSetText.AppendLine($"    Id: '{id}'");
            this.ruleSetText.AppendLine($"    DataType: {dataType}");
            if (isOptional)
            {
                this.ruleSetText.AppendLine($"    CanOptOut: true");
            }
            AddMeasuresFromTable(this.ruleSetText, table);
        }

        private void AddImmEntry(string name, string id, Table measures)
        {
            this.AddImmEntryStart(name, id);
            AddMeasuresFromTable(this.immText, measures);
        }

        private void AddImmEntry(string name, string id, params object[] measures)
        {
            this.AddImmEntryStart(name, id);
            AddMeasures(
                this.immText,
                measures.Select(m => m.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(m)!.ToString()!)));
        }

        private void AddImmEntryStart(string name, string id)
        {
            this.immText.AppendLine($"-");
            this.immText.AppendLine($"    Name: '{name}'");
            this.immText.AppendLine($"    Id: '{id}'");
        }

        private static void AddMeasuresFromTable(StringBuilder target, Table table)
        {
            AddMeasures(target, table.Rows);
        }

        /// <summary>
        /// Builds the Measures section for either a Rule Definition, or a Rule Assertion.
        /// </summary>
        /// <param name="target">The string builder to which to write the measures.</param>
        /// <param name="measures">
        /// A collection with one entry per measure. Each entry is a list of the properties to
        /// emit for the measure, typically expressed as a dictionary, but any sequence of
        /// key/value pairs will do.
        /// </param>
        /// <remarks>
        /// <para>
        /// Some tests want to supply different properties for different measures. For example:
        /// </para>
        /// <code><![CDATA[
        /// | Score | Description                   | OptOut |
        /// | 1     | Scripted and Documented       |        |
        /// | 1     | Templated                     |        |
        /// |       | Multi-tenanted - as a Service | true   |
        /// ]]></code>
        /// <para>
        /// Here, the intent is that all the measures specify a <c>Description</c>, but the first
        /// two have a <c>Score</c> and no <c>OptOut</c> whereas the final measure has an <c>OptOut</c>
        /// but not <c>Score</c>.
        /// </para>
        /// <para>
        /// For this reason, if a key/value pair in <c>measures</c> has an empty values, no property
        /// will be emitted for that entry.
        /// </para>
        /// </remarks>
        private static void AddMeasures(StringBuilder target, IEnumerable<IEnumerable<KeyValuePair<string, string>>> measures)
        {
            target.AppendLine($"    Measures:");
            foreach (IDictionary<string, string> row in measures)
            {
                target.AppendLine("        -");
                foreach ((string key, string value) in row)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        target.AppendLine($"            {key}: {(value.Contains(' ') ? $"'{value}'" : value)}");
                    }
                }
            }
        }

        private class TestEvaluationContext : IEvaluationContext
        {
            public LocalDate EvaluationReferenceDate { get; set; }
        }
    }
}
