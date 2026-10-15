using System.Globalization;
using System.Text;
using Endjin.Imm.Domain;

namespace Endjin.Imm.Tests.TestData;

/// <summary>
/// Renders everything that loading YAML produces (every property, plus the concrete type of each
/// measure) as text, so that two loads can be compared for structural equality.
/// </summary>
internal static class YamlLoadProjection
{
    public static string Of(IEnumerable<RuleDefinition>? rules)
    {
        if (rules is null)
        {
            return "<null>";
        }

        StringBuilder text = new();
        foreach (RuleDefinition rule in rules)
        {
            text.Append(CultureInfo.InvariantCulture, $"Rule {Quote(rule.Name)} {rule.Id} {rule.DataType} CanOptOut={rule.CanOptOut}\n");
            AppendMeasures(text, rule.Measures);
        }

        return text.ToString();
    }

    public static string Of(IEnumerable<RuleAssertion>? rules)
    {
        if (rules is null)
        {
            return "<null>";
        }

        StringBuilder text = new();
        foreach (RuleAssertion rule in rules)
        {
            text.Append(CultureInfo.InvariantCulture, $"Rule {Quote(rule.Name)} {rule.Id} OptOut={rule.OptOut}\n");
            AppendMeasures(text, rule.Measures);
        }

        return text.ToString();
    }

    public static string Of(MeasureDefinition? measure) => measure switch
    {
        null => "<null>",
        FrameworkMeasureDefinition framework => $"{Common(framework)} Framework={Quote(framework.Framework)}",
        AgeMeasureDefinition age => $"{Common(age)} Age={Quote(age.Age)}",
        _ => Common(measure),
    };

    public static string Of(MeasureAssertion? measure) => measure switch
    {
        null => "<null>",
        FrameworkMeasureAssertion framework => $"{Common(framework)} Framework={Quote(framework.Framework)}",
        AgeMeasureAssertion age => $"{Common(age)} Date={Quote(age.Date)}",
        _ => Common(measure),
    };

    private static void AppendMeasures(StringBuilder text, IEnumerable<MeasureDefinition>? measures)
    {
        if (measures is null)
        {
            text.Append("  Measures <null>\n");
            return;
        }

        foreach (MeasureDefinition measure in measures)
        {
            text.Append("  ").Append(Of(measure)).Append('\n');
        }
    }

    private static void AppendMeasures(StringBuilder text, IEnumerable<MeasureAssertion>? measures)
    {
        if (measures is null)
        {
            text.Append("  Measures <null>\n");
            return;
        }

        foreach (MeasureAssertion measure in measures)
        {
            text.Append("  ").Append(Of(measure)).Append('\n');
        }
    }

    private static string Common(MeasureDefinition measure) =>
        string.Create(CultureInfo.InvariantCulture, $"{measure.GetType().Name} Score={measure.Score} Description={Quote(measure.Description)} CanOptOut={measure.CanOptOut}");

    private static string Common(MeasureAssertion measure) =>
        string.Create(CultureInfo.InvariantCulture, $"{measure.GetType().Name} Score={measure.Score} Description={Quote(measure.Description)} OptOut={measure.OptOut}");

    private static string Quote(string? value) => value is null ? "<null>" : $"'{value}'";
}
