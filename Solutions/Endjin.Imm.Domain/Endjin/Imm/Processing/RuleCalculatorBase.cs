namespace Endjin.Imm.Processing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;

    /// <summary>
    /// Implements common behaviour for rule calculations (i.e., opt-out handling, and percentage
    /// calculations).
    /// </summary>
    public abstract class RuleCalculatorBase : IRuleCalculator
    {
        private readonly IRuleDefinitionRepository ruleDefinitions;

        protected RuleCalculatorBase(IRuleDefinitionRepository rdr)
        {
            this.ruleDefinitions = rdr;
        }

        /// <inheritdoc/>
        public decimal Percentage(RuleAssertion ruleAssertion, IEvaluationContext context)
        {
            return this.Calculate(ruleAssertion, context).Percentage;
        }

        /// <inheritdoc/>
        public long Score(RuleAssertion ruleAssertion, IEvaluationContext context)
        {
            return this.ScoreFromApplicableMeasures(this.MakeContext(ruleAssertion, context));
        }

        /// <inheritdoc/>
        public RuleEvaluation Evaluate(RuleAssertion ruleAssertion, IEvaluationContext context)
        {
            (decimal percentage, long score) = this.Calculate(ruleAssertion, context);
            return new RuleEvaluation(ruleAssertion, percentage, score);
        }

        /// <summary>
        /// Determine the score from a set of measures.
        /// </summary>
        /// <param name="context">The CalculationContext.</param>
        /// <returns>
        /// The score achieved by the specified set of measures.
        /// </returns>
        protected abstract long ScoreFromApplicableMeasures(CalculationContext context);

        /// <summary>
        /// Determines the maximum score possible from a particular set of measures.
        /// </summary>
        /// <param name="context">The CalculationContext.</param>
        /// <returns>
        /// The maximum possible score that can be achieved with the specified set of measures,
        /// taking opt-outs into account.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The correct calculation is different for cumulative rules and discrete ones, so we must
        /// defer to the derived class to get the correct strategy for the rule in question.
        /// </para>
        /// </remarks>
        protected abstract long MaximumFromApplicableMeasures(CalculationContext context);

        private (decimal Percentage, long Score) Calculate(RuleAssertion ruleAssertion, IEvaluationContext context)
        {
            CalculationContext calculationContext = this.MakeContext(ruleAssertion, context);

            // The maximum is determined before the score (as it always has been), so when both
            // would fail, the same exception surfaces.
            var higestScore = this.MaximumFromApplicableMeasures(calculationContext);
            var score = this.ScoreFromApplicableMeasures(calculationContext);

            return (Math.Round(Convert.ToDecimal(score / Convert.ToDecimal(higestScore)) * 100), score);
        }

        private CalculationContext MakeContext(RuleAssertion ruleAssertion, IEvaluationContext context)
        {
            // Fast path: when the assertion opts out of nothing, the filtering below would just
            // copy both measure lists, so we use the lists as they are.
            IList<MeasureAssertion> measureAssertions = ruleAssertion.Measures;
            if (measureAssertions is not null && !AnyOptedOut(measureAssertions))
            {
                var definition = this.ruleDefinitions.GetDefinitionFor(ruleAssertion);
                if (definition.Measures is IList<MeasureDefinition> measureDefinitions)
                {
                    return new CalculationContext(context, definition, ruleAssertion, measureDefinitions, measureAssertions);
                }
            }

            return this.MakeFilteredContext(ruleAssertion, context);
        }

        // Separate from MakeContext because the lambdas here capture a local, and the compiler
        // allocates the closure on entry to the scope declaring it, even on the fast path.
        private CalculationContext MakeFilteredContext(RuleAssertion ruleAssertion, IEvaluationContext context)
        {
            // TODO: Validate any opt outs? Reject attempts to opt out of a non-optional rule.
            // That would need to be done as part of https://github.com/endjin/Endjin.Ip.Maturity.Model/issues/9
            List<string> optedOutMeasures = ruleAssertion.Measures.Where(m => m.OptOut).Select(m => m.Description).ToList();
            var ruleDefinition = this.ruleDefinitions.GetDefinitionFor(ruleAssertion);

            return new CalculationContext(
                context,
                ruleDefinition,
                ruleAssertion,
                ruleDefinition.Measures
                    .Where(md => !md.CanOptOut || !optedOutMeasures.Contains(md.Description))
                    .ToList(),
                ruleAssertion.Measures
                    .Where(m => !m.OptOut)
                    .ToList());
        }

        /// <summary>
        /// Determines whether any of a rule assertion's measures is an opt-out.
        /// </summary>
        /// <param name="measureAssertions">The rule assertion's measures.</param>
        /// <returns><c>true</c> if any measure has <see cref="MeasureAssertion.OptOut"/> set.</returns>
        internal static bool AnyOptedOut(IList<MeasureAssertion> measureAssertions)
        {
            // Indexed rather than foreach to avoid allocating an enumerator. A null entry fails
            // with a NullReferenceException, just as it does in the LINQ filter.
            for (int i = 0; i < measureAssertions.Count; i++)
            {
                if (measureAssertions[i].OptOut)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Provides the information required to evaluate a particular rule assertion. (Includes
        /// the assertion and corresponding definition, and the measure assertions and definitions
        /// filtered based on any measure opt-outs specified by the assertion. When the assertion
        /// opts out of nothing, these are the original, unfiltered lists, so calculators must not
        /// modify them.)
        /// </summary>
        protected class CalculationContext
        {
            public CalculationContext(
                IEvaluationContext evaluationContext,
                RuleDefinition definition,
                RuleAssertion assertion,
                IList<MeasureDefinition> applicableMeasureDefinitions,
                IList<MeasureAssertion> applicableMeasureAssertions)
            {
                EvaluationContext = evaluationContext;
                Assertion = assertion;
                Definition = definition;
                ApplicableMeasureDefinitions = applicableMeasureDefinitions;
                ApplicableMeasureAssertions = applicableMeasureAssertions;
            }

            /// <summary>
            /// Gets the assertion for which to perform the calculation.
            /// </summary>
            /// <remarks>
            /// This is the original rule assertion as deserialized from the IMM YAML. In
            /// most cases, you will refer to <see cref="ApplicableMeasureAssertions"/>, which has
            /// been filtered based on any opt-outs specified by the assertion.
            /// </remarks>
            public RuleAssertion Assertion { get; }

            /// <summary>
            /// Gets the measures definitions application for this evaluation. When the rule
            /// assertion opts out of certain measures, they will not be included here.
            /// </summary>
            public IList<MeasureDefinition> ApplicableMeasureDefinitions { get; }

            /// <summary>
            /// Gets the asserted measures for this evaluation. This does not include entries in
            /// the YAML that were present only to declare that the measure is being opted out of.
            /// </summary>
            public IList<MeasureAssertion> ApplicableMeasureAssertions { get; }

            /// <summary>
            /// Gets the rule definition associated with the rule for which to perform the
            /// calculation.
            /// </summary>
            /// <remarks>
            /// This is the original rule definition as deserialized from the rule set YAML. In
            /// most cases, you will refer to <see cref="ApplicableMeasureDefinitions"/>, which has
            /// been filtered based on any opt-outs specified by the assertion.
            /// </remarks>
            public RuleDefinition Definition { get; }

            /// <summary>
            /// Gets contextual information that may be required for successful evaluation.
            /// </summary>
            public IEvaluationContext EvaluationContext { get; }
        }
    }
}
