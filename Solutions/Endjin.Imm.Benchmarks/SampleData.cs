using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Endjin.Imm.Contracts;
using Endjin.Imm.Domain;
using NodaTime;
using NodaTime.Text;

namespace Endjin.Imm.Benchmarks;

/// <summary>
/// Deterministic, offline test data for the benchmarks.
/// </summary>
/// <remarks>
/// <para>
/// Everything is derived from the embedded <c>Data/RuleSet.yaml</c> snapshot (see <c>Data/README.md</c>),
/// so no benchmark makes a network call and results are reproducible.
/// </para>
/// </remarks>
internal static partial class SampleData
{
    /// <summary>Logical name of the embedded canonical rule set snapshot.</summary>
    public const string RuleSetResourceName = "RuleSet.yaml";

    /// <summary>Logical name of the embedded <c>imm-default.yaml</c> (linked from Endjin.Imm.App).</summary>
    public const string DefaultImmResourceName = "imm-default.yaml";

    /// <summary>A <see cref="DataType.Continuous"/> rule: Deployment.</summary>
    public const string ContinuousRuleId = "edea4593-d2dd-485b-bc1b-aaaf18f098f9";

    /// <summary>A <see cref="DataType.Discrete"/> rule: Shared Engineering Standards.</summary>
    public const string DiscreteRuleId = "74e29f9b-6dca-4161-8fdd-b468a1eb185d";

    /// <summary>The <see cref="DataType.Age"/> rule: Date of Last IP Review.</summary>
    public const string AgeRuleId = "da4ed776-0365-4d8a-a297-c4e91a14d646";

    /// <summary>The <see cref="DataType.Framework"/> rule: Framework Version.</summary>
    public const string FrameworkRuleId = "6c0402b3-f0e3-4bd7-83fe-04bb6dca7924";

    /// <summary>The fixed evaluation date. Benchmarks never use the current date.</summary>
    public static readonly LocalDate ReferenceDate = new(2026, 10, 15);

    /// <summary>
    /// Reads an embedded resource from this assembly as a string.
    /// </summary>
    public static string LoadEmbedded(string logicalName)
    {
        using Stream stream = typeof(SampleData).Assembly.GetManifestResourceStream(logicalName)
            ?? throw new InvalidOperationException($"Embedded resource '{logicalName}' not found.");
        using StreamReader reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Splits a top-level YAML sequence into its entries. An entry starts at each line that begins
    /// with <c>-</c> in column 0 (a <c>---</c> document marker is not an entry). Anything before the
    /// first entry (comments, a document marker) is returned as the preamble.
    /// </summary>
    public static (string Preamble, IReadOnlyList<string> Entries) SplitTopLevelEntries(string yaml)
    {
        StringBuilder preamble = new();
        List<string> entries = [];
        StringBuilder? current = null;

        foreach (string rawLine in yaml.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');
            bool startsEntry = line.StartsWith('-') && !line.StartsWith("---", StringComparison.Ordinal);

            if (startsEntry)
            {
                if (current is not null)
                {
                    entries.Add(current.ToString());
                }

                current = new StringBuilder();
            }

            (current ?? preamble).Append(line).Append('\n');
        }

        if (current is not null)
        {
            entries.Add(current.ToString().TrimEnd('\n') + "\n");
        }

        return (preamble.ToString(), entries);
    }

    /// <summary>
    /// Produces a rule set with exactly <paramref name="ruleCount"/> rules: the first N rules of the
    /// snapshot, or the snapshot's rules replicated with deterministic, distinct ids.
    /// </summary>
    public static string ScaleRuleSet(string snapshot, int ruleCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ruleCount);

        (string preamble, IReadOnlyList<string> entries) = SplitTopLevelEntries(snapshot);
        StringBuilder result = new(preamble);
        HashSet<Guid> ids = [];

        for (int i = 0; i < ruleCount; i++)
        {
            string entry = entries[i % entries.Count];
            int copy = i / entries.Count;

            if (copy > 0)
            {
                int replacements = 0;
                entry = IdLine().Replace(entry, m =>
                {
                    replacements++;
                    Guid derived = DeriveId(Guid.Parse(m.Groups["id"].Value), copy);
                    return m.Groups["prefix"].Value + derived.ToString("D");
                });

                if (replacements != 1)
                {
                    throw new InvalidOperationException($"Expected exactly one Id line in rule entry, found {replacements}.");
                }
            }

            Guid id = Guid.Parse(IdLine().Match(entry).Groups["id"].Value);
            if (!ids.Add(id))
            {
                throw new InvalidOperationException($"Duplicate rule id {id} produced while scaling the rule set.");
            }

            result.Append(entry);
        }

        return result.ToString();
    }

    /// <summary>
    /// Extracts the single top-level entry for <paramref name="ruleId"/> as a one-rule YAML document.
    /// </summary>
    public static string ExtractRule(string snapshot, string ruleId)
    {
        Guid wanted = Guid.Parse(ruleId);
        (_, IReadOnlyList<string> entries) = SplitTopLevelEntries(snapshot);

        return entries.Single(e =>
        {
            Match m = IdLine().Match(e);
            return m.Success && Guid.Parse(m.Groups["id"].Value) == wanted;
        });
    }

    /// <summary>
    /// Builds a populated IMM document covering every rule in <paramref name="ruleset"/>, shaped so
    /// that every calculator does real work:
    /// <list type="bullet">
    /// <item><description>Age: a <c>Date</c> 45 days before <paramref name="referenceDate"/>.</description></item>
    /// <item><description>Framework: the first non-wildcard framework in the definition.</description></item>
    /// <item><description>Continuous: every measure with a non-zero score.</description></item>
    /// <item><description>Discrete: exactly one measure (the highest scoring), because
    /// <c>DiscreteRuleCalculator</c> uses <c>Single()</c>.</description></item>
    /// </list>
    /// </summary>
    public static string BuildImmYaml(IpMaturityModelRuleset ruleset, LocalDate referenceDate)
    {
        StringBuilder sb = new();

        foreach (RuleDefinition rule in ruleset.Rules)
        {
            sb.Append("-\n");
            sb.Append("    Name: ").Append(Quote(rule.Name ?? string.Empty)).Append('\n');
            sb.Append("    Id: ").Append(rule.Id.ToString("D")).Append('\n');
            sb.Append("    Measures:\n");

            switch (rule.DataType)
            {
                case DataType.Age:
                    string date = LocalDatePattern.Iso.Format(referenceDate.PlusDays(-45));
                    sb.Append("        -\n");
                    sb.Append("            Date: ").Append(Quote(date)).Append('\n');
                    break;

                case DataType.Framework:
                    FrameworkMeasureDefinition framework = rule.Measures
                        .OfType<FrameworkMeasureDefinition>()
                        .First(f => f.Framework != "*");
                    sb.Append("        -\n");
                    sb.Append("            Framework: ").Append(Quote(framework.Framework)).Append('\n');
                    break;

                case DataType.Continuous:
                    IEnumerable<MeasureDefinition> scoring = rule.Measures.Where(m => m.Score > 0);
                    if (!scoring.Any())
                    {
                        scoring = rule.Measures.Take(1);
                    }

                    foreach (MeasureDefinition measure in scoring)
                    {
                        AppendScoredMeasure(sb, measure);
                    }

                    break;

                case DataType.Discrete:
                    long max = rule.Measures.Max(m => m.Score);
                    AppendScoredMeasure(sb, rule.Measures.First(m => m.Score == max));
                    break;

                default:
                    throw new NotSupportedException($"Unexpected data type {rule.DataType} for rule {rule.Id}.");
            }
        }

        return sb.ToString();

        static void AppendScoredMeasure(StringBuilder sb, MeasureDefinition measure)
        {
            sb.Append("        -\n");
            sb.Append("            Score: ").Append(measure.Score.ToString(CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("            Description: ").Append(Quote(measure.Description)).Append('\n');
        }
    }

    private static string Quote(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

    private static Guid DeriveId(Guid original, int copy)
    {
        Span<byte> bytes = stackalloc byte[16];
        original.TryWriteBytes(bytes);
        int tail = BitConverter.ToInt32(bytes[12..]) ^ (copy * 0x01000193);
        BitConverter.TryWriteBytes(bytes[12..], tail);
        return new Guid(bytes);
    }

    // Matches an indented "Id:" line, with the id unquoted, single-quoted or double-quoted.
    [GeneratedRegex("""^(?<prefix>[ \t]+Id:[ \t]*)['"]?(?<id>[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12})['"]?(?=[ \t]*\r?$)""", RegexOptions.Multiline)]
    private static partial Regex IdLine();

    /// <summary>
    /// An <see cref="IEvaluationContext"/> pinned to a fixed date.
    /// </summary>
    public sealed class FixedDateContext(LocalDate evaluationReferenceDate) : IEvaluationContext
    {
        public LocalDate EvaluationReferenceDate { get; } = evaluationReferenceDate;
    }
}
