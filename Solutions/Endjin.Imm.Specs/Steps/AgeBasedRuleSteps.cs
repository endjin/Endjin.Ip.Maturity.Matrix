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

// We used StringBuilder.AppendLine mainly because it's the neatest way to generate multiple lines of text.
// We're not in it for the efficiency, so we don't want Roslynator's helpful hint in this case.
#pragma warning disable RCS1197 // Optimize StringBuilder.Append/AppendLine call.

namespace Endjin.Imm.Specs.Steps
{
    [Binding]
    public class AgeBasedRuleSteps
    {
        private readonly TestEvaluationContext evaluationContext = new TestEvaluationContext();
        private readonly StringBuilder immText = new StringBuilder();
        private IpMaturityMatrixRuleset ruleSet;
        private IpMaturityMatrix imm;
        private Dictionary<Guid, RuleEvaluation> results;

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

        [Given("my IMM has an entry named '(.*)' with id '(.*)' with a Date of '(.*)'")]
        public void GivenMyImmHasAnEntryNamedWithIdWithADateOf(string name, string id, string date)
        {
            this.immText.AppendLine($"-");
            this.immText.AppendLine($"    Name: '{name}'");
            this.immText.AppendLine($"    Id: '{id}'");
            this.immText.AppendLine($"    Measures:");
            this.immText.AppendLine($"        -");
            this.immText.AppendLine($"            Date: {date}");
        }

        [Given("my IMM has an entry named '(.*)' with id '(.*)' with no Date")]
        public void GivenMyImmHasAnEntryNamedWithIdWithNoDate(string name, string id)
        {
            this.immText.AppendLine($"-");
            this.immText.AppendLine($"    Name: '{name}'");
            this.immText.AppendLine($"    Id: '{id}'");
            this.immText.AppendLine($"    Measures:");
            this.immText.AppendLine($"        -");
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

            var evaluationEngine = new EvaluationEngine(this.ruleSet);
            this.results = evaluationEngine.Evaluate(this.imm, this.evaluationContext).ToDictionary(r => r.Rule.Id);
        }

        [Then("the score for the '(.*)' rule should be (.*)")]
        public void ThenTheScoreForTheRuleShouldBe(Guid id, int score)
        {
            Assert.AreEqual(score, this.results[id].Score);
        }

        private class TestEvaluationContext : IEvaluationContext
        {
            public LocalDate EvaluationReferenceDate { get; set; }
        }
    }
}
