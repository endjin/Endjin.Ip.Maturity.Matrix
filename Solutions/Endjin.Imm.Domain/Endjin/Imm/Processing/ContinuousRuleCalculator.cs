namespace Endjin.Imm.Processing
{
    using System;
    using System.Collections.Generic;
    using Endjin.Imm.Contracts;

    public class ContinuousRuleCalculator : RuleCalculatorBase
    {
        public ContinuousRuleCalculator(IRuleDefinitionRepository rdr)
            : base(rdr)
        {
        }

        protected override long ScoreFromApplicableMeasures(CalculationContext context)
           => SumOfScores(context.ApplicableMeasureAssertions, static m => m.Score);

        protected override long MaximumFromApplicableMeasures(CalculationContext context)
            => SumOfScores(context.ApplicableMeasureDefinitions, static m => m.Score);

        // Equivalent to Enumerable.Sum(score) (including overflow checking), but indexed, so it does
        // not allocate an enumerator. The callers' static lambdas are cached, so they do not allocate either.
        private static long SumOfScores<TMeasure>(IList<TMeasure> measures, Func<TMeasure, long> score)
        {
            long sum = 0;
            for (int i = 0; i < measures.Count; i++)
            {
                sum = checked(sum + score(measures[i]));
            }

            return sum;
        }
    }
}
