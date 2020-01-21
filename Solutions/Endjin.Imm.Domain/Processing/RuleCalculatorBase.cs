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
            var higestScore = this.MaximumFromApplicableMeasures(this.MakeContext(ruleAssertion, context));

            return Math.Round(Convert.ToDecimal(this.Score(ruleAssertion, context) / Convert.ToDecimal(higestScore)) * 100);
        }

        /// <inheritdoc/>
        public long Score(RuleAssertion ruleAssertion, IEvaluationContext context)
        {
            return this.ScoreFromApplicableMeasures(this.MakeContext(ruleAssertion, context));
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

        private CalculationContext MakeContext(RuleAssertion ruleAssertion, IEvaluationContext context)
        {
            // TODO: Validate any opt outs? Reject attempts to opt out of a non-optional rule.
            // That would need to be done as part of https://github.com/endjin/Endjin.Ip.Maturity.Matrix/issues/9
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
        /// Provides the information required to evaluate a particular rule assertion. (Includes
        /// the assertion and corresponding definition, and copies of the measure assertions and
        /// definitions filtered based on any measure opt-outs specified by the assertion.)
        /// </summary>
        protected class CalculationContext
        {
            public CalculationContext(
                IEvaluationContext evaluationContext,
                RuleDefinition definition,
                RuleAssertion assertion,
                List<MeasureDefinition> applicableMeasureDefinitions,
                List<MeasureAssertion> applicableMeasureAssertions)
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
            public List<MeasureDefinition> ApplicableMeasureDefinitions { get; }

            /// <summary>
            /// Gets the asserted measures for this evaluation. This does not include entries in
            /// the YAML that were present only to declare that the measure is being opted out of.
            /// </summary>
            public List<MeasureAssertion> ApplicableMeasureAssertions { get; }

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