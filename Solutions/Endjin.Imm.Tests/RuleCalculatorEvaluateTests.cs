using Endjin.Imm.Contracts;
using Endjin.Imm.Domain;
using Endjin.Imm.Processing;
using Endjin.Imm.Repository;
using Endjin.Imm.Tests.TestData;
using NodaTime;
using Shouldly;

namespace Endjin.Imm.Tests;

/// <summary>
/// A rule calculator works out a rule's percentage and score in a single pass, and the results
/// are exactly what its separate <see cref="IRuleCalculator.Percentage"/> and
/// <see cref="IRuleCalculator.Score"/> methods give. All evaluations use a reference date of
/// 2019-12-03.
/// </summary>
[TestClass]
public class RuleCalculatorEvaluateTests
{
    private const string RuleSet = $"""
        {RuleSetYaml.DateOfLastIpReview}
        {RuleSetYaml.FrameworkVersion}
        {RuleSetYaml.DeploymentWithOptionalMeasure}
        """;

    private const string MalformedAgeRuleSet = $"""
        -
            Name: 'Date of Last IP Review'
            Id: '{RuleSetYaml.DateOfLastIpReviewRuleId}'
            DataType: Age
            Measures:
                -
                    Score: 3
                    Age: <NotAPeriod
                    Description: '< 1 month'
                -
                    Score: 0
                    Description: None
        """;

    private static readonly IEvaluationContext Context = new FixedDateContext(new LocalDate(2019, 12, 3));

    [TestMethod]
    [DataRow("Continuous", 2, 67)]
    [DataRow("ContinuousWithOptedOutMeasure", 2, 100)]
    [DataRow("Discrete", 1, 100)]
    [DataRow("DiscreteNone", 0, 0)]
    [DataRow("AgeWithDate", 2, 67)]
    [DataRow("AgeLegacyScore", 2, 67)]
    [DataRow("AgeWithNeitherDateNorScore", 0, 0)]
    [DataRow("FrameworkMatch", 2, 67)]
    [DataRow("FrameworkWildcard", 1, 33)]
    public void Evaluate_PercentageAndScore_AgreeWithTheSeparateCalculatorMethods(string caseName, int expectedScore, int expectedPercentage)
    {
        // Arrange
        (IRuleCalculator calculator, RuleAssertion assertion) = Arrange(RuleSet, ImmFor(caseName));

        // Act
        RuleEvaluation evaluation = calculator.Evaluate(assertion, Context);

        // Assert
        evaluation.RuleAssertion.ShouldBeSameAs(assertion);
        evaluation.Score.ShouldBe(calculator.Score(assertion, Context));
        evaluation.Percentage.ShouldBe(calculator.Percentage(assertion, Context));
        evaluation.Score.ShouldBe(expectedScore);
        evaluation.Percentage.ShouldBe(expectedPercentage);
    }

    [TestMethod]
    public void Evaluate_DiscreteRuleWithTwoAssertedMeasures_ThrowsTheSameExceptionAsPercentage()
    {
        // Arrange
        const string immYaml = $"""
            -
                Name: 'Shared Engineering Standards'
                Id: '{RuleSetYaml.SharedEngineeringStandardsRuleId}'
                Measures:
                    -
                        Score: 1
                        Description: Configured
                    -
                        Score: 0
                        Description: None
            """;
        (IRuleCalculator calculator, RuleAssertion assertion) = Arrange(RuleSet, immYaml);
        InvalidOperationException fromPercentage = Should.Throw<InvalidOperationException>(() => calculator.Percentage(assertion, Context));

        // Act
        InvalidOperationException fromEvaluate = Should.Throw<InvalidOperationException>(() => calculator.Evaluate(assertion, Context));

        // Assert
        fromEvaluate.Message.ShouldBe(fromPercentage.Message);
    }

    [TestMethod]
    public void Evaluate_AgeRuleWithMalformedPeriod_ThrowsEveryTimeItIsEvaluated()
    {
        // Arrange
        (IRuleCalculator calculator, RuleAssertion assertion) = Arrange(MalformedAgeRuleSet, ImmFor("AgeWithDate"));
        Exception fromPercentage = Should.Throw<Exception>(() => calculator.Percentage(assertion, Context));

        // Act
        Exception first = Should.Throw<Exception>(() => calculator.Evaluate(assertion, Context));
        Exception second = Should.Throw<Exception>(() => calculator.Evaluate(assertion, Context));

        // Assert
        fromPercentage.ShouldBeOfType<NodaTime.Text.UnparsableValueException>();
        first.GetType().ShouldBe(fromPercentage.GetType());
        first.Message.ShouldBe(fromPercentage.Message);
        second.GetType().ShouldBe(fromPercentage.GetType());
        second.Message.ShouldBe(fromPercentage.Message);
    }

    [TestMethod]
    public void Evaluate_AgeChangedAfterEvaluation_UsesTheNewAge()
    {
        // Arrange
        RuleDefinitionRepository repository = new(IpMaturityMatrixRuleset.FromYaml(RuleSet));
        RuleAssertion assertion = IpMaturityMatrix.FromYaml(ImmFor("AgeWithDate")).Rules.Single();
        IRuleCalculator calculator = new RuleCalculatorFactory(repository).Create(DataType.Age);
        calculator.Evaluate(assertion, Context).Score.ShouldBe(2);
        AgeMeasureDefinition topMeasure = (AgeMeasureDefinition)repository.GetDefinitionFor(assertion).Measures[0];

        // Act
        topMeasure.Age = "<P3M";
        RuleEvaluation evaluation = calculator.Evaluate(assertion, Context);

        // Assert
        evaluation.Score.ShouldBe(3);
    }

    [TestMethod]
    public void Evaluate_UnknownDataType_ScoresZeroAgainstTheHighestMeasure()
    {
        // Arrange
        const string ruleSetYaml = $"""
            -
                Name: 'Shared Engineering Standards'
                Id: '{RuleSetYaml.SharedEngineeringStandardsRuleId}'
                DataType: 99
                Measures:
                    -
                        Score: 0
                        Description: None
                    -
                        Score: 1
                        Description: Configured
            """;
        RuleDefinitionRepository repository = new(IpMaturityMatrixRuleset.FromYaml(ruleSetYaml));
        IRuleCalculator calculator = new RuleCalculatorFactory(repository).Create((DataType)99);
        RuleAssertion assertion = IpMaturityMatrix.FromYaml(ImmYaml.SharedEngineeringStandardsConfigured).Rules.Single();

        // Act
        RuleEvaluation evaluation = calculator.Evaluate(assertion, Context);

        // Assert
        evaluation.RuleAssertion.ShouldBeSameAs(assertion);
        evaluation.Score.ShouldBe(0);
        evaluation.Percentage.ShouldBe(0m);
        evaluation.MaximumScore.ShouldBe(1);
    }

    private static (IRuleCalculator Calculator, RuleAssertion Assertion) Arrange(string ruleSetYaml, string immYaml)
    {
        RuleDefinitionRepository repository = new(IpMaturityMatrixRuleset.FromYaml(ruleSetYaml));
        RuleAssertion assertion = IpMaturityMatrix.FromYaml(immYaml).Rules.Single();
        IRuleCalculator calculator = new RuleCalculatorFactory(repository).Create(repository.GetDefinitionFor(assertion).DataType);
        return (calculator, assertion);
    }

    private static string ImmFor(string caseName) => caseName switch
    {
        "Continuous" => $"""
            -
                Name: 'Deployment'
                Id: '{RuleSetYaml.DeploymentRuleId}'
                Measures:
                    -
                        Score: 1
                        Description: 'Scripted and Documented'
                    -
                        Score: 1
                        Description: Templated
            """,
        "ContinuousWithOptedOutMeasure" => $"""
            -
                Name: 'Deployment'
                Id: '{RuleSetYaml.DeploymentRuleId}'
                Measures:
                    -
                        Score: 1
                        Description: 'Scripted and Documented'
                    -
                        Score: 1
                        Description: Templated
                    -
                        Description: 'Multi-tenanted - as a Service'
                        OptOut: true
            """,
        "Discrete" => ImmYaml.SharedEngineeringStandardsConfigured,
        "DiscreteNone" => ImmYaml.SharedEngineeringStandardsNone,
        "AgeWithDate" => $"""
            -
                Name: 'Date of Last IP Review'
                Id: '{RuleSetYaml.DateOfLastIpReviewRuleId}'
                Measures:
                    -
                        Date: 2019-10-15
            """,
        "AgeLegacyScore" => $"""
            -
                Name: 'Date of Last IP Review'
                Id: '{RuleSetYaml.DateOfLastIpReviewRuleId}'
                Measures:
                    -
                        Score: 2
                        Description: '> 1 month'
            """,
        "AgeWithNeitherDateNorScore" => $"""
            -
                Name: 'Date of Last IP Review'
                Id: '{RuleSetYaml.DateOfLastIpReviewRuleId}'
                Measures: []
            """,
        "FrameworkMatch" => $"""
            -
                Name: 'Framework Version'
                Id: '{RuleSetYaml.FrameworkVersionRuleId}'
                Measures:
                    -
                        Framework: netcoreapp3.1
            """,
        "FrameworkWildcard" => $"""
            -
                Name: 'Framework Version'
                Id: '{RuleSetYaml.FrameworkVersionRuleId}'
                Measures:
                    -
                        Framework: node99
            """,
        _ => throw new ArgumentOutOfRangeException(nameof(caseName), caseName, "Unknown case"),
    };

    private sealed class FixedDateContext(LocalDate evaluationReferenceDate) : IEvaluationContext
    {
        public LocalDate EvaluationReferenceDate { get; } = evaluationReferenceDate;
    }
}
