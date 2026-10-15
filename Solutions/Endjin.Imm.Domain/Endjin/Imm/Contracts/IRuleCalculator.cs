namespace Endjin.Imm.Contracts
{
    using Endjin.Imm.Domain;

    public interface IRuleCalculator
    {
        /// <summary>
        /// Calculates the proportion of the total possible score achieved for a particular rule,
        /// taking opt-outs into account.
        /// </summary>
        /// <param name="ruleAssertion">
        /// The assertion for which to calculation the percentage of the maximum score achieved.
        /// </param>
        /// <param name="context">The <see cref="IEvaluationContext"/>.</param>
        /// <returns>
        /// <para>
        /// A percentage representing what proportion of the maximum possible score for this rule
        /// has been achieved. (If the assertion states that particular measures have been opted
        /// out of, that lowers the maximum possible score, making it possible to achieve 100%
        /// without scoring that particular point.)
        /// </para>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This takes the <see cref="RuleAssertion"/> and not the <see cref="RuleDefinition"/>
        /// because the maximum possible score for a particular rule is not always fixed. In most
        /// cases it will be, but where measures have <see cref="MeasureDefinition.CanOptOut"/> set
        /// to <c>true</c>, the maximum possible score is lower.
        /// </para>
        /// </remarks>
        decimal Percentage(RuleAssertion ruleAssertion, IEvaluationContext context);

        /// <summary>
        /// Calculates the score achieved by a rule assertion.
        /// </summary>
        /// <param name="ruleAssertion"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        long Score(RuleAssertion ruleAssertion, IEvaluationContext context);

        /// <summary>
        /// Calculates both the score achieved by a rule assertion and the proportion of the total
        /// possible score that represents, taking opt-outs into account.
        /// </summary>
        /// <param name="ruleAssertion">The assertion to evaluate.</param>
        /// <param name="context">The <see cref="IEvaluationContext"/>.</param>
        /// <returns>
        /// A <see cref="RuleEvaluation"/> whose <see cref="RuleEvaluation.Percentage"/> and
        /// <see cref="RuleEvaluation.Score"/> are the values that <see cref="Percentage"/> and
        /// <see cref="Score"/> would return.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Implementations should override this to do the work only once. The default
        /// implementation simply calls <see cref="Percentage"/> and then <see cref="Score"/>.
        /// </para>
        /// </remarks>
        RuleEvaluation Evaluate(RuleAssertion ruleAssertion, IEvaluationContext context)
            => new(ruleAssertion, this.Percentage(ruleAssertion, context), this.Score(ruleAssertion, context));
    }
}