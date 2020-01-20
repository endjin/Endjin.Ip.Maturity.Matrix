namespace Endjin.Imm.Processing
{
    using System.Linq;
    using Endjin.Imm.Contracts;

    public class DiscreteRuleCalculator : DiscreteRuleCalculatorBase
    {
        public DiscreteRuleCalculator(IRuleDefinitionRepository rdr)
            : base(rdr)
        {
        }

        protected override long ScoreFromApplicableMeasures(CalculationContext context)
            => context.ApplicableMeasureAssertions.Single().Score;
    }
}