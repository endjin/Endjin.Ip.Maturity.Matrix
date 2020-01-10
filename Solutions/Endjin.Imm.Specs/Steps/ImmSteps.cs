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
        private IpMaturityMatrix? imm;

        private Dictionary<Guid, RuleEvaluation>? results;

        public void AppendImmLine(string line) => this.immText.AppendLine(line);

        [Given("I have a rule named '(.*)' with id '(.*)' and DataType '(.*)'")]
        public void GivenIHaveARuleNamedWithIdAndDataType(string ruleName, string id, string dataType, Table table)
        {
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

        [When("I evaluate the IMM")]
        public void WhenIEvaluateTheImm()
        {
            this.imm = IpMaturityMatrix.FromYaml(this.immText.ToString());

            var evaluationEngine = new EvaluationEngine(this.ruleSet!);
            this.results = evaluationEngine.Evaluate(this.imm, this.evaluationContext).ToDictionary(r => r.Rule.Id);
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
