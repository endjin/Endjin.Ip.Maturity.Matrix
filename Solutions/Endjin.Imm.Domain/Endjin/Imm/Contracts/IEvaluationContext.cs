namespace Endjin.Imm.Contracts
{
    using NodaTime;

    /// <summary>
    /// Supplies contextual information needed for correct evaluation of IMMs.
    /// </summary>
    /// <remarks>
    /// This enables us to avoid baking references to things like <c>DateTimeOffset.Now</c> for evaluation of
    /// age-related rules. This is useful for test purposes, and it also provides a single place to nail down things
    /// that might otherwise be smeared throughout the code (e.g., the decision of what constitutes today's date
    /// entails picking a time zone, and that sort of decision should be made just once).
    /// </remarks>
    public interface IEvaluationContext
    {
        /// <summary>
        /// Gets the date to use as the current date when evaluating age-based rules.
        /// </summary>
        LocalDate EvaluationReferenceDate { get; }
    }
}