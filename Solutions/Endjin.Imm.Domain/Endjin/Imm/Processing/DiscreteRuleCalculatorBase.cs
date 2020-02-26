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

        protected override long MaximumFromApplicableMeasures(CalculationContext context)
            => context.ApplicableMeasureDefinitions.Max(x => x.Score);
    }
}