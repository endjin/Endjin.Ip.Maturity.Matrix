using System.Reflection;
using System.Text;
using Endjin.Imm.Domain;
using Endjin.Imm.Serialisation;
using Endjin.Imm.Tests.TestData;
using Shouldly;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;

namespace Endjin.Imm.Tests;

/// <summary>
/// Rules load exactly as they did when YamlDotNet's general-purpose object deserializer read them:
/// the same property values for every document, and the same exceptions (type, message, position
/// and inner exception type) for every document that cannot be loaded.
/// </summary>
/// <remarks>
/// The expected result for each document comes from the deserializer configuration used before
/// rules were read by a dedicated deserializer, so this covers unusual documents that the
/// behavioural tests do not describe individually. Every document is loaded both as a rule set
/// (<see cref="RuleDefinition"/>s) and as an IMM (<see cref="RuleAssertion"/>s).
/// </remarks>
[TestClass]
public class RuleDeserializationParityTests
{
    private const string RuleId = "74e29f9b-6dca-4161-8fdd-b468a1eb185d";

    private static readonly (string Name, string Yaml)[] Documents =
    [
        // Ordinary forms
        ("Block rule with every property", $"-\n    Name: 'Rule'\n    Id: {RuleId}\n    DataType: Discrete\n    CanOptOut: true\n    OptOut: true\n    Measures:\n        -\n            Score: 1\n            Description: d\n"),
        ("Flow rule with every property", $"- {{Name: Rule, Id: '{RuleId}', DataType: Age, CanOptOut: false, OptOut: false, Measures: [{{Score: 1, Age: '*', Date: 2026-01-01}}]}}"),
        ("Compact block rule", $"- Name: Rule\n  Id: {RuleId}\n  DataType: Framework\n  Measures:\n  - Score: 2\n    Framework: net10.0\n"),
        ("Properties in reverse order", $"- {{Measures: [{{Score: 1}}], OptOut: true, CanOptOut: true, DataType: Continuous, Id: '{RuleId}', Name: Rule}}"),

        // Key casing and quoting
        ("Lower-case keys", $"- {{name: r, id: '{RuleId}', datatype: Age, canoptout: true, optout: true, measures: [{{score: 1}}]}}"),
        ("Upper-case keys", $"- {{NAME: r, ID: '{RuleId}', DATATYPE: Age, CANOPTOUT: true, OPTOUT: true, MEASURES: [{{SCORE: 1}}]}}"),
        ("Mixed-case keys", $"- {{nAmE: r, iD: '{RuleId}', DaTaTyPe: Age, CanOPTout: yes, OptOUT: yes, MeAsUrEs: []}}"),
        ("Single-quoted keys", $"- {{'Name': r, 'Id': '{RuleId}', 'Measures': []}}"),
        ("Double-quoted keys", $"- {{\"Name\": r, \"Id\": \"{RuleId}\", \"DataType\": \"Age\"}}"),
        ("Tagged key", "- {!!str Name: r, !!str OptOut: true}"),
        ("Key with surrounding spaces in quotes", "- {' Name': r, 'Name ': s}"),
        ("Key that differs by a Turkish i", "- {NAME: a, NAMİ: b, ıd: c}"),

        // Unknown keys
        ("Unknown scalar key", "- {Name: r, Extra: 1}"),
        ("Unknown nested keys", "- {Extra: {a: [1, {b: c}], d: e}, Name: r, More: [[1], {x: y}]}"),
        ("Age on a rule, as in endjin/dotnet-adr", $"-\n    Name: 'Date of Last IP Review'\n    Id: '{RuleId}'\n    Age: <P1M\n    Measures:\n        -\n            Score: 3\n            Date: 2026-01-01\n"),
        ("Measure keys on a rule", "- {Score: 1, Description: d, Framework: x, Date: 2026-01-01}"),
        ("Unknown key with an unknown alias", "- {Extra: *nope, Name: r}"),
        ("Unknown key with an unknown tag", "- {Extra: !foo bar, Name: r}"),

        // Null and empty forms
        ("Tilde for every property", "- {Name: ~, Id: ~, DataType: ~, CanOptOut: ~, OptOut: ~, Measures: ~}"),
        ("Null word for every property", "- {Name: null, Id: null, DataType: null, CanOptOut: null, OptOut: null, Measures: null}"),
        ("Null tag for every property", "-\n    Name: !!null\n    Id: !!null\n    DataType: !!null\n    CanOptOut: !!null\n    OptOut: !!null\n    Measures: !!null\n"),
        ("Null tag for reference properties", "-\n    Name: !!null\n    Measures: !!null\n"),
        ("Null word for reference properties", "- {Name: null, Measures: NULL}"),
        ("Empty value for Id only", "- {Name: r, Id: }"),
        ("Tilde for Id only", "- {Name: r, Id: ~}"),
        ("Tilde for reference properties", "- {Name: ~, Measures: ~}"),
        ("Empty values", "-\n    Name:\n    Id:\n    DataType:\n    CanOptOut:\n    OptOut:\n    Measures:\n"),
        ("Empty quoted strings", "- {Name: '', Id: '', DataType: ''}"),
        ("Empty quoted booleans", "- {CanOptOut: '', OptOut: \"\"}"),
        ("Quoted null words", "- {Name: 'null', Measures: 'null'}"),

        // Id variants
        ("Unquoted Id", $"- {{Id: {RuleId}}}"),
        ("Upper-case Id", $"- {{Id: {RuleId.ToUpperInvariant()}}}"),
        ("Braced Id", $"- {{Id: '{{{RuleId}}}'}}"),
        ("Id without hyphens", $"- {{Id: {RuleId.Replace("-", string.Empty, StringComparison.Ordinal)}}}"),
        ("Invalid Id", "- {Id: not-a-guid}"),
        ("Numeric Id", "- {Id: 5}"),
        ("String-tagged Id", $"- {{Id: !!str {RuleId}}}"),
        ("Sequence Id", "- {Id: [a]}"),
        ("Mapping Id", "- {Id: {a: b}}"),

        // DataType variants
        ("Each DataType", "- {DataType: Continuous}\n- {DataType: Discrete}\n- {DataType: Age}\n- {DataType: Framework}"),
        ("Lower-case DataType", "- {DataType: framework}"),
        ("Numeric DataType", "- {DataType: 2}"),
        ("Int-tagged DataType", "- {DataType: !!int 1}"),
        ("String-tagged DataType", "- {DataType: !!str Age}"),
        ("Out-of-range numeric DataType", "- {DataType: 7}"),
        ("Unknown DataType", "- {DataType: Percentage}"),
        ("Sequence DataType", "- {DataType: [Age]}"),
        ("Comma-separated DataType", "- {DataType: 'Age, Discrete'}"),

        ("Id with surrounding spaces", $"- {{Id: ' {RuleId} '}}"),
        ("Id in parentheses", $"- {{Id: '({RuleId})'}}"),
        ("Id in hexadecimal form", "- {Id: '{0x74e29f9b,0x6dca,0x4161,{0x8f,0xdd,0xb4,0x68,0xa1,0xeb,0x18,0x5d}}'}"),
        ("Literal block Id", $"-\n    Id: |\n        {RuleId}\n"),
        ("DataType with surrounding spaces", "- {DataType: ' Age '}"),
        ("Literal block DataType", "-\n    DataType: |\n        Age\n"),
        ("Quoted DataType", "- {DataType: 'Discrete'}\n- {DataType: \"framework\"}"),
        ("Literal block name", "-\n    Name: |\n        Multi\n        line\n"),
        ("Folded name", "-\n    Name: >\n        Folded\n        text\n"),
        ("Quoted null forms for a name", "- {Name: ''}\n- {Name: '~'}\n- {Name: \"NULL\"}"),
        ("Plain null forms for a name", "- {Name: Null}\n- {Name: NULL}"),
        ("Name with a non-specific tag", "- {Name: ! text}"),

        // Boolean variants
        ("Literal block boolean", "-\n    CanOptOut: |\n        true\n    OptOut: |\n        true\n"),
        ("Single-letter booleans", "- {CanOptOut: y, OptOut: Y}\n- {CanOptOut: n, OptOut: N}"),
        ("Mixed-case true and false", "- {CanOptOut: tRuE, OptOut: tRuE}\n- {CanOptOut: FaLsE, OptOut: FaLsE}"),
        ("True and false words", "- {CanOptOut: true, OptOut: false}\n- {CanOptOut: True, OptOut: FALSE}"),
        ("Yes, no, on and off", "- {CanOptOut: yes, OptOut: on}\n- {CanOptOut: no, OptOut: off}"),
        ("Numeric booleans", "- {CanOptOut: 1, OptOut: 0}"),
        ("Quoted booleans", "- {CanOptOut: 'true', OptOut: \"yes\"}"),
        ("String-tagged boolean", "- {CanOptOut: !!str true}"),
        ("Bool-tagged boolean", "- {OptOut: !!bool true}"),
        ("Invalid boolean", "- {CanOptOut: maybe}"),
        ("Invalid OptOut", "- {OptOut: maybe}"),

        // Measures forms
        ("Empty flow measures", "- {Name: r, Measures: []}"),
        ("Flow measures", "- {Measures: [{Score: 1}, {Score: 2, Framework: x}, ~]}"),
        ("Block measures", "- Name: r\n  Measures:\n    - Score: 1\n    -\n    - Score: 2\n      Age: '*'\n      Date: d\n"),
        ("Measures that are a mapping", "- {Measures: {Score: 1}}"),
        ("Measures that are a scalar", "- {Measures: 5}"),
        ("Measures that are an empty string", "- {Measures: ''}"),
        ("Sequence-tagged measures", "- {Measures: !!seq [{Score: 1}]}"),
        ("Unknown-tagged measures", "- {Measures: !foo [{Score: 1}]}"),
        ("Anchored measures aliased by a later rule", "- {Name: a, Measures: &m [{Score: 1, Framework: x}]}\n- {Name: b, Measures: *m}"),
        ("Anchored measure aliased by a later rule", "- {Measures: [&m {Score: 1}]}\n- {Measures: [*m, *m]}"),
        ("Measures aliasing an unknown anchor", "- {Measures: *nope}"),
        ("Rule aliasing itself as a measure", "- &r {Measures: [{Score: 1}, *r]}"),
        ("Measures aliasing the whole document as a measure", "&all\n- {Measures: [*all]}"),
        ("Anchored measure list aliased as a measure", "- {Measures: &m [*m]}"),
        ("Measures containing a scalar", "- {Measures: [5]}"),
        ("Measures containing a mapping-tagged item", "- {Measures: [!!map {Score: 1}]}"),
        ("Measures containing an unknown alias", "- {Measures: [{Score: 1}, *nope]}"),
        ("Measures with a syntax error", "- {Measures: [{Score: 1}, {Score: ]}"),
        ("Unterminated measures", "- {Measures: [{Score: 1}"),
        ("Nested sequence in measures","- {Measures: [[1, 2]]}"),

        // Anchors and aliases at rule level
        ("Anchored rule aliased later", "- &r {Name: r, Measures: [{Score: 1}]}\n- *r"),
        ("Anchored name aliased by a later rule", "- {Name: &n hello}\n- {Name: *n}"),
        ("Anchored Id aliased by a later rule", $"- {{Id: &i '{RuleId}'}}\n- {{Id: *i}}"),
        ("Anchored DataType aliased by a later rule", "- {DataType: &d Age}\n- {DataType: *d}"),
        ("Anchored number aliased into a name", "- {Name: r, CanOptOut: &n 1}\n- {Name: *n}"),
        ("Measures aliased into an Id", "- {Measures: &m [{Score: 1}]}\n- {Id: *m}"),
        ("Measures aliased into a name", "- {Measures: &m [{Score: 1}]}\n- {Name: *m}"),
        ("Name aliased into a DataType", "- {Name: &n Age}\n- {DataType: *n}"),
        ("Unknown name aliased into a DataType", "- {Name: &n Nope}\n- {DataType: *n}"),
        ("Name aliased into measures", "- {Name: &n x}\n- {Measures: *n}"),
        ("Rule aliased into a later rule's name", "- &r {Name: a}\n- {Name: *r}"),
        ("Rule aliased into a later rule's measures", "- &r {Name: a}\n- {Measures: *r}"),
        ("Id aliased into a name", $"- {{Id: &i {RuleId}}}\n- {{Name: *i}}"),
        ("Id aliased into a boolean", $"- {{Id: &i {RuleId}}}\n- {{CanOptOut: *i, OptOut: *i}}"),
        ("DataType aliased into a boolean", "- {DataType: &d Age}\n- {CanOptOut: *d}"),
        ("Boolean aliased into a DataType", "- {CanOptOut: &b true}\n- {DataType: *b}"),
        ("Boolean aliased into an Id", "- {CanOptOut: &b true, OptOut: &c true}\n- {Id: *b}\n- {Id: *c}"),
        ("Boolean aliased into a name", "- {CanOptOut: &b true, OptOut: &c true}\n- {Name: *b}\n- {Name: *c}"),
        ("Rule aliasing itself as its name","- &r {Name: *r}"),
        ("Rule aliasing itself as its measures", "- &r {Measures: *r}"),
        ("Rule aliasing itself as its Id", "- &r {Id: *r}"),
        ("Rule aliasing itself as its DataType", "- &r {DataType: *r}"),
        ("Rule aliasing itself, then overwritten", "- &r {Name: *r, Name: plain}"),
        ("Rule aliasing the whole document", "&all\n- {Name: *all}"),
        ("Rule aliasing the whole document as its measures", "&all\n- {Measures: *all}"),
        ("Alias to an unknown anchor", "- {Name: *nope}"),
        ("Alias as a key", "- {&k Name: r, *k : s}"),

        // Duplicate, complex and merge keys
        ("Duplicate keys", "- {Name: a, Name: b}"),
        ("Duplicate keys with different case", "- {Name: a, name: b, NAME: c}"),
        ("Duplicate measures", "- {Measures: [{Score: 1}], Measures: [{Score: 2}]}"),
        ("Duplicate Id, second invalid", $"- {{Id: '{RuleId}', Id: bad}}"),
        ("Complex key first", "- {[a, b]: c, Name: r}"),
        ("Complex key after properties", "- {Name: r, ? {x: y} : z}"),
        ("Complex key naming a property", "- {? [Name] : r}"),
        ("Merge key", $"- {{<<: {{Name: r}}, Id: '{RuleId}'}}"),
        ("Merge key with an alias", "- &base {Name: r, DataType: Age}\n- <<: *base\n  Id: 74e29f9b-6dca-4161-8fdd-b468a1eb185d\n"),

        // Rule-level forms other than a mapping
        ("Bare dash", "-"),
        ("Bare dash between rules", "- {Name: a}\n-\n- {Name: b}"),
        ("Tilde rule", "- ~"),
        ("Null word rule", "- null"),
        ("Null tag rule", "- !!null"),
        ("Scalar rule", "- 5"),
        ("Quoted scalar rule", "- 'text'"),
        ("String-tagged rule", "- !!str x"),
        ("Sequence rule", "- [1, 2]"),
        ("Empty sequence rule", "- []"),
        ("Empty mapping rule", "- {}"),
        ("Map-tagged rule", "- !!map {Name: r}"),
        ("Unknown-tagged rule", "- !foo {Name: r}"),
        ("Map-tagged empty rule", "- !!map {}"),

        // Root forms
        ("Empty document", ""),
        ("Comment-only document", "# nothing yet"),
        ("Empty root sequence", "[]"),
        ("Root mapping", "{Name: r}"),
        ("Root scalar", "r"),
        ("Root null", "~"),
        ("Anchored root sequence", "&all [{Name: r}]"),

        // Document markers, BOM and line endings
        ("Document start marker", "---\n- {Name: r}"),
        ("Document start and end markers", "---\n- {Name: r}\n..."),
        ("YAML directive", "%YAML 1.1\n---\n- {Name: r}"),
        ("Two documents", "- {Name: a}\n---\n- {Name: b}"),
        ("Byte order mark", "﻿- {Name: r}"),
        ("Byte order mark before a block document", "﻿-\n    Name: r\n    Measures:\n        -\n            Score: 1\n"),
        ("CRLF line endings", "-\r\n    Name: r\r\n    Id: 74e29f9b-6dca-4161-8fdd-b468a1eb185d\r\n    DataType: Age\r\n    Measures:\r\n        -\r\n            Score: 1\r\n            Age: '*'\r\n"),
        ("CR line endings", "-\r    Name: r\r    DataType: Age\r"),
        ("Tabs inside a quoted value", "- {Name: \"a\tb\"}"),

        // Errors, and which error wins
        ("Syntax error inside a rule", "- {Name: r, Id: [}"),
        ("Conversion error then syntax error", "- {Id: bad, Name: 'unterminated}"),
        ("Two conversion errors in a rule", "- {Id: bad, DataType: bad}"),
        ("Conversion errors in two rules", "- {DataType: bad}\n- {Id: bad}"),
        ("Conversion error then syntax error in a later rule", "- {OptOut: maybe}\n- {Name: [}"),
        ("Measure error then rule error", "- {Measures: [{Score: high}], Id: bad}"),
        ("Rule error then measure error", "- {Id: bad, Measures: [{Score: high}]}"),
        ("Unknown alias then syntax error", "- {Name: *nope, Id: [}"),
        ("Block rule with bad indentation", "-\n    Name: r\n   Id: x\n"),
        ("Block rule with tab indentation", "-\n\tName: r\n"),
        ("Unterminated flow rule", "- {Name: r"),
        ("Unterminated root sequence", "[{Name: r}"),
    ];

    public static IEnumerable<object[]> DocumentCases =>
        Documents
            .Select(d => new object[] { d.Name, d.Yaml })
            .Concat(
            [
                [nameof(RuleSetYaml.DateOfLastIpReview), RuleSetYaml.DateOfLastIpReview],
                [nameof(RuleSetYaml.FrameworkVersion), RuleSetYaml.FrameworkVersion],
                [nameof(RuleSetYaml.DeploymentWithOptionalMeasure), RuleSetYaml.DeploymentWithOptionalMeasure],
                [nameof(RuleSetYaml.OptionalDeploymentRule), RuleSetYaml.OptionalDeploymentRule],
                [nameof(ImmYaml.SharedEngineeringStandardsConfigured), ImmYaml.SharedEngineeringStandardsConfigured],
                [nameof(ImmYaml.SharedEngineeringStandardsNone), ImmYaml.SharedEngineeringStandardsNone],
                ["Published rule set snapshot", PublishedRuleSet],
                ["Default IMM template", ReadRepositoryFile("Solutions", "Endjin.Imm.App", "imm-default.yaml")],
                ["180-rule rule set", string.Concat(Enumerable.Repeat(PublishedRuleSet.TrimEnd() + "\n", 10))],
                ["180-rule IMM", ManyRuleAssertions(180)],
            ]);

    private static string PublishedRuleSet => ReadRepositoryFile("Solutions", "Endjin.Imm.Benchmarks", "Data", "RuleSet.yaml");

    public static string CaseDisplayName(MethodInfo method, object[] data) => $"{method.Name} ({data[0]})";

    [TestMethod]
    [DynamicData(nameof(DocumentCases), DynamicDataDisplayName = nameof(CaseDisplayName))]
    public void FromYaml_DocumentAsRuleSet_LoadsAsThePreviousConfigurationDid(string name, string yaml)
    {
        // Act
        string expected = Load<RuleDefinition>(CreatePreviousConfiguration(), yaml);
        string actual = Load<RuleDefinition>(ImmYamlDeserializer.Create(), yaml);

        // Assert
        actual.ShouldBe(expected, name);
    }

    [TestMethod]
    [DynamicData(nameof(DocumentCases), DynamicDataDisplayName = nameof(CaseDisplayName))]
    public void FromYaml_DocumentAsImm_LoadsAsThePreviousConfigurationDid(string name, string yaml)
    {
        // Act
        string expected = Load<RuleAssertion>(CreatePreviousConfiguration(), yaml);
        string actual = Load<RuleAssertion>(ImmYamlDeserializer.Create(), yaml);

        // Assert
        actual.ShouldBe(expected, name);
    }

    [TestMethod]
    [DynamicData(nameof(DocumentCases), DynamicDataDisplayName = nameof(CaseDisplayName))]
    public void FromYaml_DocumentWithSharedDeserializer_LoadsAsThePreviousConfigurationDid(string name, string yaml)
    {
        // Act
        string expectedRuleSet = Load<RuleDefinition>(CreatePreviousConfiguration(), yaml);
        string actualRuleSet = Load<RuleDefinition>(ImmYamlDeserializer.Shared, yaml);
        string expectedImm = Load<RuleAssertion>(CreatePreviousConfiguration(), yaml);
        string actualImm = Load<RuleAssertion>(ImmYamlDeserializer.Shared, yaml);

        // Assert
        actualRuleSet.ShouldBe(expectedRuleSet, $"{name} as a rule set");
        actualImm.ShouldBe(expectedImm, $"{name} as an IMM");
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
            return DescribeException(x);
        }
    }

    private static string DescribeException(Exception x)
    {
        StringBuilder text = new($"Throws {x.GetType().FullName}: {x.Message}");
        if (x is YamlException yaml)
        {
            text.Append($" [{yaml.Start.Line}:{yaml.Start.Column}-{yaml.End.Line}:{yaml.End.Column}]");
        }

        for (Exception? inner = x.InnerException; inner is not null; inner = inner.InnerException)
        {
            text.Append($" <- {inner.GetType().FullName}");
        }

        return text.ToString();
    }

    private static string ManyRuleAssertions(int count)
    {
        StringBuilder yaml = new();
        for (int i = 0; i < count; i++)
        {
            yaml.Append($"-\n    Name: 'Rule {i}'\n    Id: {new Guid(i, 0, 0, new byte[8])}\n");
            if (i % 5 == 0)
            {
                yaml.Append("    OptOut: true\n");
            }

            yaml.Append($"    Measures:\n        -\n            Score: {i % 4}\n            Description: 'Measure {i}'\n");
            if (i % 3 == 0)
            {
                yaml.Append("        -\n            Score: 1\n            Framework: net10.0\n");
            }
        }

        return yaml.ToString();
    }

    /// <summary>
    /// The deserializer configuration in use before rules had their own deserializer: a frozen
    /// copy of what <see cref="ImmYamlDeserializer.Create"/> built at that time.
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
            .WithNodeDeserializer(new MeasureNodeDeserializer(), s => s.Before<DictionaryNodeDeserializer>())
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
