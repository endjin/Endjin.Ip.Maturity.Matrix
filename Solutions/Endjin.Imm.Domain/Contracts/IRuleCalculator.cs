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
    }
}