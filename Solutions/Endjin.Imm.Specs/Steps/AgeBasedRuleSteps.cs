using TechTalk.SpecFlow;

namespace Endjin.Imm.Specs.Steps
{
    [Binding]
    public class AgeBasedRuleSteps
    {
        private readonly ImmSteps immSteps;

        public AgeBasedRuleSteps(ImmSteps immSteps)
        {
            this.immSteps = immSteps;
        }

        [Given("my IMM has an entry named '(.*)' with id '(.*)' with a Date of '(.*)'")]
        public void GivenMyImmHasAnEntryNamedWithIdWithADateOf(string name, string id, string date)
        {
            this.immSteps.AppendImmLine($"-");
            this.immSteps.AppendImmLine($"    Name: '{name}'");
            this.immSteps.AppendImmLine($"    Id: '{id}'");
            this.immSteps.AppendImmLine($"    Measures:");
            this.immSteps.AppendImmLine($"        -");
            this.immSteps.AppendImmLine($"            Date: {date}");
        }

        [Given("my IMM has an entry named '(.*)' with id '(.*)' with no Date")]
        public void GivenMyImmHasAnEntryNamedWithIdWithNoDate(string name, string id)
        {
            this.immSteps.AppendImmLine($"-");
            this.immSteps.AppendImmLine($"    Name: '{name}'");
            this.immSteps.AppendImmLine($"    Id: '{id}'");
            this.immSteps.AppendImmLine($"    Measures:");
            this.immSteps.AppendImmLine($"        -");
        }
    }
}