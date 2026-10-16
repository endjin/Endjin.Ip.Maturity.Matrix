using System.Reflection;
using Endjin.Imm.Domain;
using Endjin.Imm.Serialisation;
using Endjin.Imm.Tests.TestData;
using Shouldly;
using YamlDotNet.Serialization;

namespace Endjin.Imm.Tests;

/// <summary>
/// Measures load exactly as they did when YamlDotNet's type-discriminating deserializer chose
/// their types: the same concrete types and property values for every document, and the same
/// exception types for every document that cannot be loaded.
/// </summary>
/// <remarks>
/// The expected result for each document comes from the deserializer configuration used before
/// measures were read by a dedicated deserializer, so this covers unusual documents that the
/// behavioural tests do not describe individually.
/// </remarks>
[TestClass]
public class MeasureDeserializationParityTests
{
    private const string RuleId = "74e29f9b-6dca-4161-8fdd-b468a1eb185d";

    /// <summary>
    /// Measure list entries, each loaded both as a rule set's measure definitions and as an IMM's
    /// measure assertions.
    /// </summary>
    private static readonly (string Name, string Measures)[] MeasureEntries =
    [
        ("Plain measure", "- {Score: 1, Description: Configured}"),
        ("Block style measures", "-\n    Score: 3\n    Framework: net10.0\n    Description: 'Current LTS'\n-\n    Score: 2\n    Age: <P1M\n    Date: 2026-01-01\n-\n    Score: 0\n    Description: None"),
        ("Framework then Age then Date", "- {Framework: net8.0, Age: <P1M, Date: 2026-01-01, Score: 2}"),
        ("Age then Framework", "- {Age: <P1M, Framework: net8.0, Score: 2}"),
        ("Date then Framework", "- {Date: 2026-01-01, Framework: net8.0}"),
        ("Discriminator after other properties", "- {Score: 2, Description: d, CanOptOut: true, OptOut: true, Age: '*'}"),
        ("Mixed-case keys", "- {FRAMEWORK: x, score: 3, DESCRIPTION: d, canoptout: true, optout: true}"),
        ("Mixed-case discriminators", "- {aGe: '*', DaTe: 2026-01-01}"),
        ("Null score and empty description", "- {Score: ~, Description: ''}"),
        ("Null score and description", "- {Score: ~, Description: ~}"),
        ("Null words for every property", "- {Score: null, Description: null, CanOptOut: ~, OptOut: ~, Framework: ~, Age: ~, Date: ~}"),
        ("Empty values", "-\n    Score:\n    Description:\n    Framework:"),
        ("Unknown nested mapping", "- {Score: 1, Extra: {a: [1, {b: c}], d: e}, Framework: x}"),
        ("Unknown nested sequence", "- {Extra: [[1, 2], {Framework: y}], Age: '*'}"),
        ("Unknown key that looks like a discriminator value", "- {Description: Framework, Score: 1}"),
        ("Anchored measure aliased later", "- &m {Score: 1, Framework: x}\n- *m"),
        ("Anchored value aliased into a discriminator", "- {Description: &d hello, Framework: *d}"),
        ("Anchored string aliased into the score", "- {Description: &n '5', Score: *n}"),
        ("Anchored number aliased into the description", "- {Score: &n 5, Description: *n}"),
        ("Alias to an unknown anchor", "- {Score: *nope}"),
        ("Measure aliasing itself", "- &s {Score: 1, Description: *s}"),
        ("Measure aliasing itself as its discriminator", "- &s {Framework: *s}"),
        ("Measure aliasing itself, then overwritten", "- &s {Description: *s, Description: plain}"),
        ("Later measure aliasing an earlier one", "- &a {Score: 1}\n- {Description: *a}"),
        ("Duplicate keys", "- {Score: 1, Score: 2, Framework: a, framework: b}"),
        ("Duplicate discriminators with another between", "- {Framework: a, Age: b, Date: c, Framework: d}"),
        ("Complex key first", "- {[a, b]: c, Score: 1}"),
        ("Complex key naming a discriminator", "- {? {x: y} : Framework, Score: 1}"),
        ("Complex key after properties", "- {Score: 1, ? [Framework] : x}"),
        ("Complex key after a discriminator", "- {Age: '*', ? [a] : b}"),
        ("Alias as a key", "- {&k Score: 1, *k : 2}"),
        ("Empty mapping", "- {}"),
        ("Bare dash", "-"),
        ("Null tag", "- !!null"),
        ("Null tag with value", "- !!null ~"),
        ("Tilde", "- ~"),
        ("Null word", "- null"),
        ("Quoted null word", "- 'null'"),
        ("Double-quoted null word", "- \"null\""),
        ("Scalar measure", "- 5"),
        ("Sequence measure", "- [1, 2]"),
        ("Map tag on measure", "- !!map {Score: 1}"),
        ("Unknown tag on measure", "- !foo {Score: 1}"),
        ("String tag on score", "- {Score: !!str 5}"),
        ("Hex score", "- {Score: 0x1F}"),
        ("Tagged hex score", "- {Score: !!int 0x1F}"),
        ("Non-numeric score", "- {Score: high}"),
        ("Fractional score", "- {Score: 1.5}"),
        ("Score too large", "- {Score: 99999999999999999999}"),
        ("Zero score", "- {Score: 0}"),
        ("Leading-zero score", "- {Score: 007}"),
        ("Leading-zero score with an 8", "- {Score: 08}"),
        ("Signed scores", "- {Score: +5}\n- {Score: -5}\n- {Score: -0}"),
        ("Underscored score", "- {Score: 1_000}"),
        ("Base-60 score", "- {Score: 1:30}"),
        ("Binary score", "- {Score: 0b101}"),
        ("Eighteen-digit score", "- {Score: 123456789012345678}"),
        ("Nineteen-digit score", "- {Score: 1234567890123456789}"),
        ("Largest score", "- {Score: 9223372036854775807}"),
        ("Quoted score", "- {Score: '5'}\n- {Score: \"0\"}"),
        ("Literal block score", "-\n    Score: |\n        5\n"),
        ("Score with surrounding spaces", "- {Score: ' 5 '}"),
        ("Unicode digit score", "- {Score: ٥}"),
        ("Quoted null forms for strings", "- {Description: '', Framework: '~'}\n- {Description: \"null\", Age: 'NULL'}"),
        ("Plain null forms for strings", "- {Description: Null, Framework: NULL}"),
        ("Folded description", "-\n    Description: >\n        Folded\n        text\n"),
        ("Mixed-case true and false", "- {CanOptOut: TRUE, OptOut: False}\n- {CanOptOut: fAlSe, OptOut: tRuE}"),
        ("Literal block boolean", "-\n    CanOptOut: |\n        true\n    OptOut: |\n        true\n"),
        ("Single-letter booleans", "- {CanOptOut: y, OptOut: N}"),
        ("Anchored score aliased later", "- {Score: &s 3}\n- {Score: *s}"),
        ("Tagged string description", "- {Description: !!str 5, Framework: !!str x}"),
        ("Yes and on booleans","- {CanOptOut: yes, OptOut: on}"),
        ("Numeric boolean", "- {CanOptOut: 1, OptOut: 0}"),
        ("Invalid boolean", "- {CanOptOut: maybe, OptOut: maybe}"),
        ("Sequence discriminator value", "- {Framework: [a]}"),
        ("Age sequence before Framework sequence", "- {Age: [a], Framework: [b]}"),
        ("Valid Framework then sequence Age", "- {Framework: x, Age: [a]}"),
        ("Mapping description", "- {Description: {a: b}}"),
        ("Merge key", "- {<<: {Score: 1}, Description: d}"),
        ("Syntax error inside a measure", "- {Score: 1, Framework: [a}"),
        ("Conversion error then syntax error", "- {Score: high, Framework: 'unterminated}"),
        ("Unknown alias then syntax error", "- {Score: *nope, Description: [}"),
        ("Unknown alias then syntax error after the measure", "- {Score: *nope}\n- [}"),
        ("Conversion error then error after the measure", "-\n    Score: high\n  - x: ]"),
        ("Block measure with bad indentation", "-\n    Score: 1\n   Description: d"),
        ("Tab-indented value", "-\n    Score: 1\n    Description: \"a\tb\""),
        ("Many measures", string.Join('\n', Enumerable.Range(0, 50).Select(i => i % 3 == 0 ? $"- {{Score: {i}, Framework: f{i}}}" : i % 3 == 1 ? $"- {{Score: {i}, Age: a{i}, Date: d{i}}}" : $"- {{Score: {i}, Description: d{i}}}"))),
    ];

    public static IEnumerable<object[]> MeasureEntryCases => MeasureEntries.Select(e => new object[] { e.Name, e.Measures });

    public static IEnumerable<object[]> DocumentCases =>
    [
        [nameof(RuleSetYaml.DateOfLastIpReview), RuleSetYaml.DateOfLastIpReview],
        [nameof(RuleSetYaml.FrameworkVersion), RuleSetYaml.FrameworkVersion],
        [nameof(RuleSetYaml.DeploymentWithOptionalMeasure), RuleSetYaml.DeploymentWithOptionalMeasure],
        [nameof(RuleSetYaml.OptionalDeploymentRule), RuleSetYaml.OptionalDeploymentRule],
        [nameof(ImmYaml.SharedEngineeringStandardsConfigured), ImmYaml.SharedEngineeringStandardsConfigured],
        [nameof(ImmYaml.SharedEngineeringStandardsNone), ImmYaml.SharedEngineeringStandardsNone],
        ["Published rule set snapshot", ReadRepositoryFile("Solutions", "Endjin.Imm.Benchmarks", "Data", "RuleSet.yaml")],
        ["Default IMM template", ReadRepositoryFile("Solutions", "Endjin.Imm.App", "imm-default.yaml")],
        ["Empty document", ""],
        ["Comment-only document", "# nothing yet"],
        ["Empty sequence", "[]"],
        ["Measures not a sequence", $"- {{Id: '{RuleId}', Measures: {{Score: 1}}}}"],
        ["Null measures", $"- {{Id: '{RuleId}', Measures: ~}}"],
    ];

    public static string CaseDisplayName(MethodInfo method, object[] data) => $"{method.Name} ({data[0]})";

    [TestMethod]
    [DynamicData(nameof(MeasureEntryCases), DynamicDataDisplayName = nameof(CaseDisplayName))]
    public void FromYaml_MeasureDefinitionEntry_LoadsAsThePreviousConfigurationDid(string name, string measures)
    {
        // Arrange
        string ruleSetYaml = RuleWithMeasures(measures, "DataType: Discrete");

        // Act
        string expected = Load<RuleDefinition>(CreatePreviousConfiguration(), ruleSetYaml);
        string actual = Load<RuleDefinition>(ImmYamlDeserializer.Create(), ruleSetYaml);

        // Assert
        actual.ShouldBe(expected, name);
    }

    [TestMethod]
    [DynamicData(nameof(MeasureEntryCases), DynamicDataDisplayName = nameof(CaseDisplayName))]
    public void FromYaml_MeasureAssertionEntry_LoadsAsThePreviousConfigurationDid(string name, string measures)
    {
        // Arrange
        string immYaml = RuleWithMeasures(measures, "OptOut: false");

        // Act
        string expected = Load<RuleAssertion>(CreatePreviousConfiguration(), immYaml);
        string actual = Load<RuleAssertion>(ImmYamlDeserializer.Create(), immYaml);

        // Assert
        actual.ShouldBe(expected, name);
    }

    [TestMethod]
    [DynamicData(nameof(DocumentCases), DynamicDataDisplayName = nameof(CaseDisplayName))]
    public void FromYaml_Document_LoadsAsThePreviousConfigurationDid(string name, string yaml)
    {
        // Act
        string expectedRuleSet = Load<RuleDefinition>(CreatePreviousConfiguration(), yaml);
        string actualRuleSet = Load<RuleDefinition>(ImmYamlDeserializer.Create(), yaml);
        string expectedImm = Load<RuleAssertion>(CreatePreviousConfiguration(), yaml);
        string actualImm = Load<RuleAssertion>(ImmYamlDeserializer.Create(), yaml);

        // Assert
        actualRuleSet.ShouldBe(expectedRuleSet, $"{name} as a rule set");
        actualImm.ShouldBe(expectedImm, $"{name} as an IMM");
    }

    private static string RuleWithMeasures(string measures, string ruleProperty)
    {
        string indented = string.Join('\n', measures.Split('\n').Select(line => "        " + line));
        return $"-\n    Name: 'Rule'\n    Id: '{RuleId}'\n    {ruleProperty}\n    Measures:\n{indented}\n";
    }

    /// <summary>
    /// Loads a document, describing either everything that was loaded or the exception thrown.
    /// </summary>
    private static string Load<TRule>(IDeserializer deserializer, string yaml)
    {
        try
        {
            using StringReader input = new(yaml);
            return deserializer.Deserialize<TRule[]>(input) switch
            {
                RuleDefinition[] rules => YamlLoadProjection.Of(rules),
                RuleAssertion[] rules => YamlLoadProjection.Of(rules),
                null => "<null>",
                var other => throw new InvalidOperationException($"Unexpected result {other}"),
            };
        }
        catch (Exception x)
        {
            return $"Throws {x.GetType().FullName}: {x.Message}";
        }
    }

    /// <summary>
    /// The deserializer configuration in use before measures had their own deserializer.
    /// </summary>
    private static IDeserializer CreatePreviousConfiguration()
    {
        var emptyMeasureDefinition = new EmptyNodeAsDefaultInstanceNodeDeserializer<MeasureDefinition>();
        var emptyMeasureAssertion = new EmptyNodeAsDefaultInstanceNodeDeserializer<MeasureAssertion>();

        return new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithCaseInsensitivePropertyMatching()
            .WithNodeTypeResolver(emptyMeasureDefinition)
            .WithNodeTypeResolver(emptyMeasureAssertion)
            .WithNodeDeserializer(emptyMeasureDefinition)
            .WithNodeDeserializer(emptyMeasureAssertion)
            .WithTypeDiscriminatingNodeDeserializer(options =>
            {
                options.AddUniqueKeyTypeDiscriminator<MeasureDefinition>(
                    new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
                    {
                        [nameof(FrameworkMeasureDefinition.Framework)] = typeof(FrameworkMeasureDefinition),
                        [nameof(AgeMeasureDefinition.Age)] = typeof(AgeMeasureDefinition),
                    });
                options.AddUniqueKeyTypeDiscriminator<MeasureAssertion>(
                    new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
                    {
                        [nameof(FrameworkMeasureAssertion.Framework)] = typeof(FrameworkMeasureAssertion),
                        [nameof(AgeMeasureAssertion.Date)] = typeof(AgeMeasureAssertion),
                    });
            })
            .Build();
    }

    private static string ReadRepositoryFile(params string[] pathFromRepositoryRoot)
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            string path = Path.Combine([directory.FullName, .. pathFromRepositoryRoot]);
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
        }

        throw new FileNotFoundException($"'{Path.Combine(pathFromRepositoryRoot)}' not found above {AppContext.BaseDirectory}.");
    }
}
