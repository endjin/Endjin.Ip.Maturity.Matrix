namespace Endjin.Imm.Domain
{
    using System;

    /// <summary>
    /// Thrown when an IMM refers to a rule id that the rule set does not define.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This usually means that the IMM was written against a different version of the rule set
    /// (a different <c>definitionsBranch</c>), or that the id in the IMM is mistyped. It derives
    /// from <see cref="ArgumentException"/>, which is what was thrown before this type existed.
    /// </para>
    /// </remarks>
    public class UnknownRuleException : ArgumentException
    {
        /// <summary>
        /// Creates an <see cref="UnknownRuleException"/>.
        /// </summary>
        /// <param name="ruleId">The rule id that the rule set does not define.</param>
        public UnknownRuleException(Guid ruleId)
            : base("No definition found for rule id " + ruleId)
        {
            this.RuleId = ruleId;
        }

        /// <summary>
        /// Gets the rule id that the rule set does not define.
        /// </summary>
        public Guid RuleId { get; }
    }
}
