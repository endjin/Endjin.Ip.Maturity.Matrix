namespace Endjin.Imm.Processing
{
    using System.Linq;
    using Endjin.Imm.Contracts;

    public abstract class DiscreteRuleCalculatorBase : RuleCalculatorBase
    {
        protected DiscreteRuleCalculatorBase(IRuleDefinitionRepository rdr)
            : base(rdr)
        {
        }

        // A rule with no applicable measures (all opted out, or none defined) has a maximum of 0.
        protected override long MaximumFromApplicableMeasures(CalculationContext context)
            => context.ApplicableMeasureDefinitions.Count == 0 ? 0 : context.ApplicableMeasureDefinitions.Max(x => x.Score);
    }
}