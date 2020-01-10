using TechTalk.SpecFlow;

namespace Endjin.Imm.Specs.Steps
{
    [Binding]
    public class FrameworkNameRuleSteps
    {
        private readonly ImmSteps immSteps;

        public FrameworkNameRuleSteps(ImmSteps immSteps)
        {
            this.immSteps = immSteps;
        }

        [Given("my IMM has an entry named '(.*)' with id '(.*)' with a Framework of '(.*)'")]
        public void GivenMyIMMHasAnEntryNamedWithIdWithAFrameworkOf(string name, string id, string framework)
        {
            this.immSteps.AppendImmLine($"-");
            this.immSteps.AppendImmLine($"    Name: '{name}'");
            this.immSteps.AppendImmLine($"    Id: '{id}'");
            this.immSteps.AppendImmLine($"    Measures:");
            this.immSteps.AppendImmLine($"        -");
            this.immSteps.AppendImmLine($"            Framework: {framework}");
        }
    }
}