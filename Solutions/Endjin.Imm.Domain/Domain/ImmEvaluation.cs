namespace Endjin.Imm.Domain
{
    using System.Collections.Generic;

    /// <summary>
    /// The results of evaluation a project's IP Maturity Matrix.
    /// </summary>
    public class ImmEvaluation
    {
        /// <summary>
        /// Creates an <see cref="ImmEvaluation"/>.
        /// </summary>
        /// <param name="ruleEvaluations">Value for <see cref="RuleEvaluations"/>.</param>
        /// <param name="totalScore">Value for <see cref="TotalScore"/>.</param>
        /// <param name="maximumPossibleTotalScore">Value for <see cref="MaximumPossibleTotalScore"/>.</param>
        public ImmEvaluation(
            List<RuleEvaluation> ruleEvaluations,
            long totalScore,
            long maximumPossibleTotalScore)
        {
            this.MaximumPossibleTotalScore = maximumPossibleTotalScore;
            this.RuleEvaluations = ruleEvaluations;
            this.TotalScore = totalScore;
        }

        public long MaximumPossibleTotalScore { get; }
        public List<RuleEvaluation> RuleEvaluations { get; }
        public long TotalScore { get; }
    }
}