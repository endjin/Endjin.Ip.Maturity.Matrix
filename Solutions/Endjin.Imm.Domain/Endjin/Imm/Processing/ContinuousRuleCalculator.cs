namespace Endjin.Imm.Processing
{
    using System.Collections.Generic;
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;

    public class ContinuousRuleCalculator : RuleCalculatorBase
    {
        public ContinuousRuleCalculator(IRuleDefinitionRepository rdr)
            : base(rdr)
        {
        }

        protected override long ScoreFromApplicableMeasures(CalculationContext context)
           => SumOfScores(context.ApplicableMeasureAssertions);

        protected override long MaximumFromApplicableMeasures(CalculationContext context)
            => SumOfScores(context.ApplicableMeasureDefinitions);

        // Equivalent to Enumerable.Sum(x => x.Score) (including overflow checking), but indexed,
        // so it does not allocate an enumerator.
        private static long SumOfScores(IList<MeasureAssertion> measures)
        {
            long sum = 0;
            for (int i = 0; i < measures.Count; i++)
            {
                sum = checked(sum + measures[i].Score);
            }

            return sum;
        }

        private static long SumOfScores(IList<MeasureDefinition> measures)
        {
            long sum = 0;
            for (int i = 0; i < measures.Count; i++)
            {
                sum = checked(sum + measures[i].Score);
            }

            return sum;
        }
    }
}
