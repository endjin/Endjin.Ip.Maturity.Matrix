namespace Endjin.Imm.Processing
{
    using System;

    /// <summary>
    /// Thrown when a rule in an IMM cannot be evaluated because what the IMM asserts for it does
    /// not fit the rule's definition: for example, a discrete rule with more than one level
    /// asserted, or an Age rule whose date cannot be parsed.
    /// </summary>
    /// <remarks>
    /// The <see cref="Exception.InnerException"/> is the failure from the rule's calculator.
    /// </remarks>
    public class ImmEvaluationException : Exception
    {
        /// <summary>
        /// Creates an <see cref="ImmEvaluationException"/>.
        /// </summary>
        /// <param name="ruleId">The id of the rule that could not be evaluated.</param>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The failure from the rule's calculator.</param>
        public ImmEvaluationException(Guid ruleId, string message, Exception innerException)
            : base(message, innerException)
        {
            this.RuleId = ruleId;
        }

        /// <summary>
        /// Gets the id of the rule that could not be evaluated.
        /// </summary>
        public Guid RuleId { get; }
    }
}
