namespace Endjin.Imm.Processing
{
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;

    public class NullRuleCalculator : IRuleCalculator
    {
        public decimal Percentage(RuleAssertion ruleAssertion, IEvaluationContext context)
        {
            return 0m;
        }

        public long Score(RuleAssertion ruleAssertion, IEvaluationContext context)
        {
            return 0;
        }
    }
}