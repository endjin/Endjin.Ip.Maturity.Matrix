using Endjin.Imm.Contracts;
using Endjin.Imm.Domain;
using System;
using System.Linq;

namespace Endjin.Imm.Processing
{
    /// <summary>
    /// Calculates the score for rules with <see cref="DataType.Framework"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This enables us to define measures that indicate how recent a version of a framework some code targets,
    /// and for that score to drop automatically when newer frameworks come out. This means we capture the staleness
    /// of this aspects of IP by (without needing to update every single component's IMM each time a new version of
    /// say .NET ships. It allows us to define rules like this:
    /// </para>
    /// <code><![CDATA[
    /// -
    ///     Name: 'Framework Version'
    ///     Id: 6c0402b3-f0e3-4bd7-83fe-04bb6dca7924
    ///     DataType: Age
    ///     Measures:
    ///         -
    ///             Score: 3
    ///             Framework: 'netcoreapp3.1'
    ///             Description: 'Using the most current LTS version'
    ///         -
    ///             Score: 2
    ///             Framework: 'netcoreapp2.1'
    ///             Description: 'Using a LTS version'
    ///         -
    ///             Score: 1
    ///             Framework: '*'
    ///             Description: 'Using an unsupported version'
    ///         -
    ///             Score: 0
    ///             Description: None
    /// ]]></code>
    /// <para>
    /// The <c>Framework</c> entries in each measure are a string identifying a particular version of a framework.
    /// For example, with .NET this will be a target framework moniker (TFM) such as netcoreapp3.1. The rule supports
    /// multiple frameworks, so it might also be node14. (There's no requirement for exclusivity - we can define multiple
    /// "most current" entries to support multiple different frameworks. However, the expectation is that any particular
    /// piece of IP will declare support for one particular framework.) So IMMs will contain something like this
    /// </para>
    /// <code><![CDATA[
    /// -
    ///     Name: 'Framework Version'
    ///     Id: 6c0402b3-f0e3-4bd7-83fe-04bb6dca7924
    ///     Measures:
    ///         -
    ///             Framework: netcoreapp3.1
    /// ]]></code>
    /// <para>
    /// This enables the score to change over time automatically. If this entry is evaluated on 2020-01-10, the score
    /// will be 3 because .NET Core 3.1 is the current LTS version on that date. Once .NET Core 6.0 ships, the score
    /// will drop automatically to 2.
    /// </para>
    /// </remarks>
    internal class FrameworkRuleCalculator : DiscreteRuleCalculatorBase
    {
        public FrameworkRuleCalculator(IRuleDefinitionRepository rdr)
            : base(rdr)
        {
        }

        protected override long ScoreFromApplicableMeasures(CalculationContext context)
        {
            // We need to handle two cases.
            // 1) Most IMMs should specify a framework for rules of this type.
            // 2) Because we used not to support self-aging properties, older IMMs just put a Score.
            if (context.ApplicableMeasureAssertions.OfType<FrameworkMeasureAssertion>().SingleOrDefault() is FrameworkMeasureAssertion frameworkMeasure)
            {
                // There was a Framework, so we can do this properly.
                foreach (MeasureDefinition measureDefinition in context.ApplicableMeasureDefinitions)
                {
                    if (measureDefinition is not FrameworkMeasureDefinition fmd)
                    {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                        // The "ruleAssertion" argument name here is defined in IRuleCalculator.Score
                        throw new ArgumentException(
                            "Rule measure definition's Framework is not set, so it should not be used with " + nameof(FrameworkRuleCalculator),
                            "ruleAssertion");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
                    }

                    if (fmd.Framework == "*" ||
                        fmd.Framework.Equals(frameworkMeasure.Framework, StringComparison.OrdinalIgnoreCase))
                    {
                        return measureDefinition.Score;
                    }
                }
            }
            else if (context.ApplicableMeasureAssertions.SingleOrDefault(m => m?.Score != 0) is MeasureAssertion scoreMeasure)
            {
                // The IMM did not specify a Framework for this, but there is a Score entry, so we need
                // to return that.
                return scoreMeasure.Score;
            }

            // No Score or Framework, so see if the rule definition includes an entry saying what to do if
            // the Framework is unknown.
            if (context.ApplicableMeasureDefinitions.SingleOrDefault(m => m is not FrameworkMeasureDefinition) is MeasureDefinition measureDefForWhenFrameworkNotPresent)
            {
                return measureDefForWhenFrameworkNotPresent.Score;
            }

            // In the absence of other instructions you get no points.
            return 0;
        }
    }
}