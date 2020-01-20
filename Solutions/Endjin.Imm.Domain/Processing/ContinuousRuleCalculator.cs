namespace Endjin.Imm.Processing
{
    using System.Linq;
    using Endjin.Imm.Contracts;

    public class ContinuousRuleCalculator : RuleCalculatorBase
    {
        public ContinuousRuleCalculator(IRuleDefinitionRepository rdr)
            : base(rdr)
        {
        }

        protected override long ScoreFromApplicableMeasures(CalculationContext context)
           => context.ApplicableMeasureAssertions.Sum(x => x.Score);

        protected override long MaximumFromApplicableMeasures(CalculationContext context)
            => context.ApplicableMeasureDefinitions.Sum(x => x.Score);
    }
}