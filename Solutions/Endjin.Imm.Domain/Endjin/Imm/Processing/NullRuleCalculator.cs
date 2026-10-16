namespace Endjin.Imm.Processing
{
    using System.Linq;
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;

    /// <summary>
    /// The calculator for a rule whose <see cref="DataType"/> is not recognised. Such a rule scores
    /// nothing, but it still counts towards the maximum possible total, as its highest-scoring
    /// applicable measure.
    /// </summary>
    public class NullRuleCalculator : IRuleCalculator
    {
        private readonly IRuleDefinitionRepository ruleDefinitions;

        public NullRuleCalculator(IRuleDefinitionRepository rdr)
        {
            this.ruleDefinitions = rdr;
        }
        public decimal Percentage(RuleAssertion ruleAssertion, IEvaluationContext context)
        {
            return 0m;
        }

        public long Score(RuleAssertion ruleAssertion, IEvaluationContext context)
        {
            return 0;
        }

        public RuleEvaluation Evaluate(RuleAssertion ruleAssertion, IEvaluationContext context)
        {
            // Opt-outs are applied as in RuleCalculatorBase: only measures that allow it can be opted out of.
            var optedOut = ruleAssertion.Measures.Where(m => m.OptOut).Select(m => m.Description).ToHashSet();
            long maximumScore = this.ruleDefinitions.GetDefinitionFor(ruleAssertion).Measures
                .Where(md => !md.CanOptOut || !optedOut.Contains(md.Description))
                .Select(md => md.Score)
                .DefaultIfEmpty(0)
                .Max();

            return new RuleEvaluation(ruleAssertion, 0m, 0, maximumScore);
        }
    }
}