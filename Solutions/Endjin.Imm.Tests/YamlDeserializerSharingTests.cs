using System.Collections.Concurrent;
using Endjin.Imm.Domain;
using Endjin.Imm.Serialisation;
using Endjin.Imm.Tests.TestData;
using Shouldly;
using YamlDotNet.Core;

namespace Endjin.Imm.Tests;

/// <summary>
/// Rule sets and IMMs load identically however many loads are in progress at once, and however
/// many have gone before (including ones that failed), so the Host can load several documents in
/// parallel and keep loading after a malformed document.
/// </summary>
[TestClass]
public class YamlDeserializerSharingTests
{
    private static readonly string[] RuleSetDocuments =
    [
        RuleSetYaml.DateOfLastIpReview,
        RuleSetYaml.FrameworkVersion,
        RuleSetYaml.DeploymentWithOptionalMeasure,
        RuleSetYaml.OptionalDeploymentRule,
    ];

    private static readonly string[] ImmDocuments =
    [
        ImmYaml.SharedEngineeringStandardsConfigured,
        ImmYaml.SharedEngineeringStandardsNone,
        DateAndFrameworkImm,
    ];

    // Exercises both discriminated measure assertion types alongside the base type.
    private const string DateAndFrameworkImm = $"""
        -
            Name: 'Date of Last IP Review'
            Id: '{RuleSetYaml.DateOfLastIpReviewRuleId}'
            Measures:
                -
                    Date: 2026-09-01
        -
            Name: 'Framework Version'
            Id: '{RuleSetYaml.FrameworkVersionRuleId}'
            Measures:
                -
                    Framework: netcoreapp3.1
        -
            Name: 'Deployment'
            Id: '{RuleSetYaml.DeploymentRuleId}'
            Measures:
                -
                    Score: 1
                    Description: 'Multi-tenanted - as a Service'
                    OptOut: true
        """;

    [TestMethod]
    public void FromYaml_CalledConcurrently_LoadsEveryDocumentIdentically()
    {
        // Arrange
        const int Workers = 64;
        const int LoadsPerWorker = 25;
        string[] expectedRuleSets = RuleSetDocuments.Select(ReferenceRuleSetProjection).ToArray();
        string[] expectedImms = ImmDocuments.Select(ReferenceImmProjection).ToArray();
        ConcurrentBag<string> mismatches = [];

        // Act
        Parallel.For(0, Workers, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, worker =>
        {
            for (int load = 0; load < LoadsPerWorker; load++)
            {
                int index = worker + load;
                if (index % 2 == 0)
                {
                    int document = index / 2 % RuleSetDocuments.Length;
                    string actual = YamlLoadProjection.Of(IpMaturityMatrixRuleset.FromYaml(RuleSetDocuments[document]).Rules);
                    if (actual != expectedRuleSets[document])
                    {
                        mismatches.Add($"Rule set {document}, worker {worker}, load {load}:\n{actual}");
                    }
                }
                else
                {
                    int document = index / 2 % ImmDocuments.Length;
                    string actual = YamlLoadProjection.Of(IpMaturityMatrix.FromYaml(ImmDocuments[document]).Rules);
                    if (actual != expectedImms[document])
                    {
                        mismatches.Add($"IMM {document}, worker {worker}, load {load}:\n{actual}");
                    }
                }
            }
        });

        // Assert
        mismatches.ShouldBeEmpty();
    }

    [TestMethod]
    public void FromYaml_SameDocumentLoadedRepeatedly_ProducesEqualResults()
    {
        // Arrange
        string expectedRuleSet = ReferenceRuleSetProjection(RuleSetYaml.FrameworkVersion);
        string expectedImm = ReferenceImmProjection(DateAndFrameworkImm);

        for (int load = 0; load < 200; load++)
        {
            // Act
            IpMaturityMatrixRuleset ruleSet = IpMaturityMatrixRuleset.FromYaml(RuleSetYaml.FrameworkVersion);
            IpMaturityMatrix imm = IpMaturityMatrix.FromYaml(DateAndFrameworkImm);

            // Assert
            YamlLoadProjection.Of(ruleSet.Rules).ShouldBe(expectedRuleSet, $"rule set load {load}");
            YamlLoadProjection.Of(imm.Rules).ShouldBe(expectedImm, $"IMM load {load}");
        }
    }

    [TestMethod]
    [DataRow("- [unterminated", DisplayName = "Malformed YAML")]
    [DataRow("-\n    Measures:\n        -\n            Framework: net10.0\n            Score: high", DisplayName = "Invalid value inside a discriminated measure")]
    [DataRow("-\n    Measures:\n        -\n            Framework: [a, b]", DisplayName = "Wrong node kind for a discriminator key")]
    [DataRow("-\n    Measures:\n        - 'null'", DisplayName = "Scalar where a measure is expected")]
    public void FromYaml_AfterALoadFails_LoadsTheNextDocumentCorrectly(string invalidYaml)
    {
        // Arrange
        string expectedRuleSet = ReferenceRuleSetProjection(RuleSetYaml.FrameworkVersion);
        string expectedImm = ReferenceImmProjection(DateAndFrameworkImm);

        // Act
        Should.Throw<YamlException>(() => IpMaturityMatrix.FromYaml(invalidYaml));
        Should.Throw<YamlException>(() => IpMaturityMatrixRuleset.FromYaml(invalidYaml));
        IpMaturityMatrixRuleset ruleSet = IpMaturityMatrixRuleset.FromYaml(RuleSetYaml.FrameworkVersion);
        IpMaturityMatrix imm = IpMaturityMatrix.FromYaml(DateAndFrameworkImm);

        // Assert
        YamlLoadProjection.Of(ruleSet.Rules).ShouldBe(expectedRuleSet);
        YamlLoadProjection.Of(imm.Rules).ShouldBe(expectedImm);
    }

    // The reference result for each document comes from a deserializer built just for that load,
    // so it cannot be affected by anything other loads have done.
    private static string ReferenceRuleSetProjection(string yaml)
    {
        using StringReader input = new(yaml);
        return YamlLoadProjection.Of(ImmYamlDeserializer.Create().Deserialize<RuleDefinition[]>(input));
    }

    private static string ReferenceImmProjection(string yaml)
    {
        using StringReader input = new(yaml);
        return YamlLoadProjection.Of(ImmYamlDeserializer.Create().Deserialize<RuleAssertion[]>(input));
    }
}
