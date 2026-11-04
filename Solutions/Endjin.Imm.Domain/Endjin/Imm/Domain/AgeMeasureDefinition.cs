namespace Endjin.Imm.Domain
{
    using NodaTime;
    using NodaTime.Text;

    public class AgeMeasureDefinition : MeasureDefinition
    {
        private string age = "";
        private ParsedAge? parsedAge;

        public string Age
        {
            get => this.age;
            set
            {
                this.age = value;
                this.parsedAge = null;
            }
        }

        /// <summary>
        /// Gets the result of parsing the ISO8601 period that follows the leading <c>&lt;</c> or
        /// <c>&gt;</c> in <see cref="Age"/>.
        /// </summary>
        /// <returns>
        /// The parse result. Reading its <see cref="ParseResult{T}.Value"/> throws if the text
        /// could not be parsed, so a malformed <see cref="Age"/> fails each time it is evaluated
        /// (and not when the rule set is loaded).
        /// </returns>
        /// <remarks>
        /// The result is cached, because rule definitions are loaded once and evaluated many
        /// times. The cache remembers the text it was parsed from, so a stale result is never
        /// used, even if <see cref="Age"/> is changed concurrently with a call to this method.
        /// </remarks>
        internal ParseResult<Period> GetAgePeriod()
        {
            string ageText = this.age;
            ParsedAge? cached = this.parsedAge;
            if (cached is null || !ReferenceEquals(cached.Text, ageText))
            {
                cached = new ParsedAge(ageText, PeriodPattern.NormalizingIso.Parse(ageText[1..]));
                this.parsedAge = cached;
            }

            return cached.Result;
        }

        private sealed record ParsedAge(string Text, ParseResult<Period> Result);
    }
}
