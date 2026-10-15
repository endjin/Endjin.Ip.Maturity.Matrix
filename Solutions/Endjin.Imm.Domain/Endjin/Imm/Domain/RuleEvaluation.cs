namespace Endjin.Imm.Domain
{
    public class RuleEvaluation
    {
        public RuleEvaluation(RuleAssertion ruleAssertion, decimal percentage, long score, long maximumScore)
        {
            RuleAssertion = ruleAssertion;
            Percentage = percentage;
            Score = score;
            MaximumScore = maximumScore;
        }

        public RuleAssertion RuleAssertion { get; }

        public decimal Percentage { get; }

        public long Score { get; }

        /// <summary>
        /// Gets the maximum score this rule assertion could achieve, taking into account the measures it
        /// opts out of (where the rule definition allows that). <see cref="Percentage"/> is
        /// <see cref="Score"/> as a proportion of this, and the IMM's maximum possible total score is the sum of
        /// this across its rules.
        /// </summary>
        public long MaximumScore { get; }
    }
}