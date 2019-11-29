namespace Endjin.Imm.Processing
{
    using Endjin.Imm.Contracts;
    using Endjin.Imm.Domain;
    using NodaTime;
    using NodaTime.Text;
    using System;
    using System.Linq;

    /// <summary>
    /// Calculates the score for rules with <see cref="DataType.Age"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This enables us to define measures that automatically drop over time, so that we can capture the staleness of
    /// aspects of IP by default. It allows us to define rules like this:
    /// </para>
    /// <code><![CDATA[
    /// -
    ///     Name: 'Date of Last IP Review'
    ///     Id: da4ed776-0365-4d8a-a297-c4e91a14d646
    ///     DataType: Age
    ///     Measures:
    ///         -
    ///             Score: 3
    ///             Age: <P1M
    ///             Description: '< 1 month'
    ///         -
    ///             Score: 2
    ///             Age: <P3M
    ///             Description: '> 1 month'
    ///         -
    ///             Score: 1
    ///             Age: *
    ///             Description: '> 3 months'
    ///         -
    ///             Score: 0
    ///             Description: None
    /// ]]></code>
    /// <para>
    /// The <c>Age</c> entries in each measure are in ISO8601 Duration format. This means that if we don't look at some
    /// IP for a while, its score should drop over time. If we use the simple approach where the IMM just states the
    /// score directly, that doesn't happen without intervention. But the whole point of these kinds of rules is that
    /// they are supposed to tell you when there has been no intervention lately. So they will be wrong exactly when
    /// you need them to be right! But when a rule is in this form, an IMM can specify a date, e.g.:
    /// </para>
    /// <code><![CDATA[
    /// -
    ///     Name: 'Date of Last IP Review'
    ///     Id: da4ed776-0365-4d8a-a297-c4e91a14d646
    ///     Measures:
    ///         -
    ///             Date: 2019-11-28
    /// ]]></code>
    /// <para>
    /// This enables the score to change over time automatically. If this entry is evaluated on 2019-11-29, the score
    /// will be 3 because the review occurred only 1 day ago, well within the 1 month specified. If the exact same
    /// entry is evaluated on 2020-01-01, the score will drop to 2, because the last review was now more than 1
    /// month ago, but still within the 3 month specified by the second measure in the rule definition. And if
    /// evaluated on 2020-06-06, the score will have dropped to just 1 if the IMM still has the date of 2019-11-28.
    /// </para>
    /// </remarks>
    internal class AgeRuleCalculator : IRuleCalculator
    {
        private readonly IRuleDefinitionRepository rdr;

        public AgeRuleCalculator(IRuleDefinitionRepository rdr)
        {
            this.rdr = rdr;
        }

        public decimal Percentage(Rule rule, IEvaluationContext context)
        {
            RuleDefinition definition = this.rdr.Get(rule);
            long higestScore = definition.Measures.Max(m => m.Score);

            return Math.Round((Convert.ToDecimal(this.Score(rule, context)) / Convert.ToDecimal(higestScore)) * 100);
        }

        public long Score(Rule rule, IEvaluationContext context)
        {
            RuleDefinition definition = this.rdr.Get(rule);

            // We need to handle two cases.
            // 1) Most IMMs should specify a Date.
            // 2) Because we used not to support self-aging properties, older IMMs just put a Score.
            if (rule.Measures?.SingleOrDefault(m => !string.IsNullOrWhiteSpace(m?.Date)) is Measure dateMeasure)
            {
                // There was a Date, so we can do this properly.
                LocalDate dateInImm = LocalDatePattern.Iso.Parse(dateMeasure.Date).Value;

                foreach (Measure rd in definition.Measures)
                {
                    // Need to work out which kind of rule: < or *
                    // If <, then parse the rule as Period in ISO8601 form, and 
                    if (LocalDateMatchesRule(dateInImm, context.EvaluationReferenceDate, rd.Age))
                    {
                        return rd.Score;
                    }
                }
            }
            else if (rule.Measures?.SingleOrDefault(m => m?.Score != 0) is Measure scoreMeasure)
            {
                // The IMM did not specify a Date for this, but there is a Score entry, so we need
                // to return that.
                return scoreMeasure.Score;
            }

            // No Score or Date, so see if the rule definition includes an entry saying what to do if
            // the Age is unknown.
            if (definition.Measures.SingleOrDefault(m => string.IsNullOrWhiteSpace(m.Age)) is Measure measureDefForWhenAgeNotPresent)
            {
                return measureDefForWhenAgeNotPresent.Score;
            }

            // In the absence of other instructions you get no points.
            return 0;
        }

        private static bool LocalDateMatchesRule(LocalDate dateInImm, LocalDate referenceDate, string ageText)
        {
            return ageText switch
            {
                "*" => true,
                string t when t[0] == '<' => DateWithinPeriod(dateInImm, referenceDate, ageText),
                string t when t[0] == '>' => !DateWithinPeriod(dateInImm, referenceDate, ageText),
                _ => false
            };

            static bool DateWithinPeriod(LocalDate dateInImm, LocalDate referenceDate, string ageText)
            {
                Period p = PeriodPattern.NormalizingIso.Parse(ageText.Substring(1)).Value;

                LocalDate dateOnWhichNoLongerWithinPeriod = dateInImm.Plus(p);
                return dateOnWhichNoLongerWithinPeriod > referenceDate;
            }
        }
    }
}