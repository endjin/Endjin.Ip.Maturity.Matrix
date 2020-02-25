namespace Endjin.Imm.Domain
{
    public class RuleEvaluation
    {
        public RuleEvaluation(RuleAssertion ruleAssertion, decimal percentage, long score)
        {
            RuleAssertion = ruleAssertion;
            Percentage = percentage;
            Score = score;
        }

        public RuleAssertion RuleAssertion { get; }

        public decimal Percentage { get; }

        public long Score { get; }
    }
}