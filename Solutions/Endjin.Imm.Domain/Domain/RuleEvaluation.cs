namespace Endjin.Imm.Domain
{
    public class RuleEvaluation
    {
        public RuleEvaluation(RuleAssertion rule, decimal percentage, long score)
        {
            Rule = rule;
            Percentage = percentage;
            Score = score;
        }

        public RuleAssertion Rule { get; }

        public decimal Percentage { get; }

        public long Score { get; }
    }
}