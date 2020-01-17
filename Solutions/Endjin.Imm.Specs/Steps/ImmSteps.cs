using Endjin.Imm.Contracts;
using Endjin.Imm.Domain;
using Endjin.Imm.Processing;
using NodaTime;
using NodaTime.Text;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TechTalk.SpecFlow;

namespace Endjin.Imm.Specs.Steps
{
    [Binding]
    public class ImmSteps
    {
        private readonly TestEvaluationContext evaluationContext = new TestEvaluationContext();
        private readonly StringBuilder immText = new StringBuilder();
        private IpMaturityMatrixRuleset? ruleSet;
        private Table? ruleDefinitionTable;
        private IpMaturityMatrix? imm;

        private Dictionary<Guid, RuleEvaluation>? results;

        public void AppendImmLine(string line) => this.immText.AppendLine(line);

        private RuleDefinition RuleDefinition => this.ruleSet!.Rules.Single();

        [Given("I have a rule named '(.*)' with id '(.*)' and DataType '(.*)'")]
        public void GivenIHaveARuleNamedWithIdAndDataType(string ruleName, string id, string dataType, Table table)
        {
            Assert.IsNull(this.ruleDefinitionTable, "Multiple rule setups not supported by these test bindings");
            this.ruleDefinitionTable = table;

            var yaml = new StringBuilder();
            yaml.AppendLine($"-");
            yaml.AppendLine($"    Name: '{ruleName}'");
            yaml.AppendLine($"    Id: '{id}'");
            yaml.AppendLine($"    DataType: {dataType}");
            yaml.AppendLine($"    Measures:");
            foreach (TableRow row in table.Rows)
            {
                yaml.AppendLine("        -");
                foreach (string column in table.Header)
                {
                    string value = row[column];
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        yaml.AppendLine($"            {column}: {(value.Contains(' ') ? $"'{value}'" : value)}");
                    }
                }
            }

            this.ruleSet = IpMaturityMatrixRuleset.FromYaml(yaml.ToString());
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

        [Given("my IMM has an entry named '(.*)' with id '(.*)' with a Score of (.*) and description of '(.*)'")]
        public void GivenMyImmHasAnEntryNamedWithIdWithAScoreOf(string name, string id, int score, string description)
        {
            this.immText.AppendLine($"-");
            this.immText.AppendLine($"    Name: '{name}'");
            this.immText.AppendLine($"    Id: '{id}'");
            this.immText.AppendLine($"    Measures:");
            this.immText.AppendLine($"        -");
            this.immText.AppendLine($"            Score: {score}");
            this.immText.AppendLine($"            Description: '{description}'");
        }

        [When("I load the rules")]
        public void WhenILoadTheRules()
        {
            // We rely on a Given step calling GivenIHaveARuleNamedWithIdAndDataType
            // And that actually does the parsing, so all we do here is verify that that has happened.
            Assert.IsNotNull(this.ruleSet, "No rule was set up");
        }

        [When("I evaluate the IMM")]
        public void WhenIEvaluateTheImm()
        {
            this.imm = IpMaturityMatrix.FromYaml(this.immText.ToString());

            var evaluationEngine = new EvaluationEngine(this.ruleSet!);
            this.results = evaluationEngine.Evaluate(this.imm, this.evaluationContext).ToDictionary(r => r.Rule.Id);
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
            Assert.AreEqual(this.ruleDefinitionTable!.RowCount, this.RuleDefinition.Measures.Count);
        }

        [Then("all the rule definition measure definitions with Framework property should be of type '(.*)'")]
        public void ThenAllTheRuleDefinitionMeasureDefinitionsShouldBeOfType(string typeName)
        {
            for (int i = 0; i < this.ruleDefinitionTable!.RowCount; ++i)
            {
                TableRow tableRow = this.ruleDefinitionTable!.Rows[i];
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
            for (int i = 0; i < this.ruleDefinitionTable!.RowCount; ++i)
            {
                TableRow tableRow = this.ruleDefinitionTable!.Rows[i];
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
            for (int i = 0; i < this.ruleDefinitionTable!.RowCount; ++i)
            {
                TableRow tableRow = this.ruleDefinitionTable!.Rows[i];
                MeasureDefinition measureDefinition = this.RuleDefinition.Measures[i];

                int scoreInYaml = Convert.ToInt32(tableRow["Score"]);
                Assert.AreEqual(scoreInYaml, measureDefinition.Score);
            }
        }

        [Then("all the rule definition measure definition descriptions should match the descriptions in the YAML")]
        public void ThenAllTheRuleDefinitionMeasureDefinitionDescriptionsShouldMatchTheScoresInTheYAML()
        {
            for (int i = 0; i < this.ruleDefinitionTable!.RowCount; ++i)
            {
                TableRow tableRow = this.ruleDefinitionTable!.Rows[i];
                MeasureDefinition measureDefinition = this.RuleDefinition.Measures[i];

                string descriptionInYaml = tableRow["Description"];
                Assert.AreEqual(descriptionInYaml, measureDefinition.Description);
            }
        }

        [Then("all the rule definition measure definitions with Framework property frameworks should match the frameworks in the YAML")]
        public void ThenAllTheRuleDefinitionMeasureDefinitionFrameworksShouldMatchTheScoresInTheYAML()
        {
            for (int i = 0; i < this.ruleDefinitionTable!.RowCount; ++i)
            {
                TableRow tableRow = this.ruleDefinitionTable!.Rows[i];
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
            for (int i = 0; i < this.ruleDefinitionTable!.RowCount; ++i)
            {
                TableRow tableRow = this.ruleDefinitionTable!.Rows[i];
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
            Assert.AreEqual(score, this.results![id].Score);
        }

        [Then("the percentage for the '(.*)' rule should be (.*)")]
        public void ThenThePercentageForTheRuleShouldBe(Guid id, decimal percentage)
        {
            Assert.AreEqual(percentage, this.results![id].Percentage);
        }

        private class TestEvaluationContext : IEvaluationContext
        {
            public LocalDate EvaluationReferenceDate { get; set; }
        }
    }
}
