// ------------------------------------------------------------------------------
//  <auto-generated>
//      This code was generated by SpecFlow (http://www.specflow.org/).
//      SpecFlow Version:3.1.0.0
//      SpecFlow Generator Version:3.1.0.0
// 
//      Changes to this file may cause incorrect behavior and will be lost if
//      the code is regenerated.
//  </auto-generated>
// ------------------------------------------------------------------------------
#region Designer generated code
#pragma warning disable
namespace Endjin.Imm.Specs.Features
{
    using TechTalk.SpecFlow;
    using System;
    using System.Linq;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "3.1.0.0")]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [NUnit.Framework.TestFixtureAttribute()]
    [NUnit.Framework.DescriptionAttribute("LoadRuleDefinitions")]
    public partial class LoadRuleDefinitionsFeature
    {
        
        private TechTalk.SpecFlow.ITestRunner testRunner;
        
        private string[] _featureTags = ((string[])(null));
        
#line 1 "LoadRuleDefinitions.feature"
#line hidden
        
        [NUnit.Framework.OneTimeSetUpAttribute()]
        public virtual void FeatureSetup()
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "LoadRuleDefinitions", "    In order to be able to process an IP Maturity Matrix\r\n    As a developer\r\n   " +
                    " I want to be able to load the rule definitions from YAML", ProgrammingLanguage.CSharp, ((string[])(null)));
            testRunner.OnFeatureStart(featureInfo);
        }
        
        [NUnit.Framework.OneTimeTearDownAttribute()]
        public virtual void FeatureTearDown()
        {
            testRunner.OnFeatureEnd();
            testRunner = null;
        }
        
        [NUnit.Framework.SetUpAttribute()]
        public virtual void TestInitialize()
        {
        }
        
        [NUnit.Framework.TearDownAttribute()]
        public virtual void TestTearDown()
        {
            testRunner.OnScenarioEnd();
        }
        
        public virtual void ScenarioInitialize(TechTalk.SpecFlow.ScenarioInfo scenarioInfo)
        {
            testRunner.OnScenarioInitialize(scenarioInfo);
            testRunner.ScenarioContext.ScenarioContainer.RegisterInstanceAs<NUnit.Framework.TestContext>(NUnit.Framework.TestContext.CurrentContext);
        }
        
        public virtual void ScenarioStart()
        {
            testRunner.OnScenarioStart();
        }
        
        public virtual void ScenarioCleanup()
        {
            testRunner.CollectScenarioErrors();
        }
        
        [NUnit.Framework.TestAttribute()]
        [NUnit.Framework.DescriptionAttribute("Framework rules")]
        public virtual void FrameworkRules()
        {
            string[] tagsOfScenario = ((string[])(null));
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Framework rules", null, ((string[])(null)));
#line 6
this.ScenarioInitialize(scenarioInfo);
#line hidden
            bool isScenarioIgnored = default(bool);
            bool isFeatureIgnored = default(bool);
            if ((tagsOfScenario != null))
            {
                isScenarioIgnored = tagsOfScenario.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((this._featureTags != null))
            {
                isFeatureIgnored = this._featureTags.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((isScenarioIgnored || isFeatureIgnored))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
                TechTalk.SpecFlow.Table table3 = new TechTalk.SpecFlow.Table(new string[] {
                            "Score",
                            "Framework",
                            "Description"});
                table3.AddRow(new string[] {
                            "3",
                            "netcoreapp6.0",
                            "Using the most current LTS version"});
                table3.AddRow(new string[] {
                            "2",
                            "netcoreapp3.1",
                            "Using a LTS version"});
                table3.AddRow(new string[] {
                            "2",
                            "netcoreapp2.1",
                            "Using a LTS version"});
                table3.AddRow(new string[] {
                            "3",
                            "node14",
                            "Using the most current LTS version"});
                table3.AddRow(new string[] {
                            "3",
                            "node12",
                            "Using the most current LTS version"});
                table3.AddRow(new string[] {
                            "2",
                            "node10",
                            "Using a LTS version"});
                table3.AddRow(new string[] {
                            "2",
                            "node8",
                            "Using a LTS version"});
                table3.AddRow(new string[] {
                            "1",
                            "\'*\'",
                            "Using an unsupported version"});
                table3.AddRow(new string[] {
                            "0",
                            "",
                            "None"});
#line 7
    testRunner.Given("I have a rule named \'Framework Version\' with id \'6c0402b3-f0e3-4bd7-83fe-04bb6dca" +
                        "7924\' and DataType \'Framework\'", ((string)(null)), table3, "Given ");
#line hidden
#line 18
    testRunner.When("I load the rules", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 19
    testRunner.Then("the rule definition name should be \'Framework Version\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
#line 20
    testRunner.And("the rule definition id should be \'6c0402b3-f0e3-4bd7-83fe-04bb6dca7924\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 21
    testRunner.And("the rule definition should have the same number of measures as were in the YAML", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 22
    testRunner.And("all the rule definition measure definitions with Framework property should be of " +
                        "type \'Endjin.Imm.Domain.FrameworkMeasureDefinition\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 23
    testRunner.And("all the rule definition measure definition scores should match the scores in the " +
                        "YAML", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 24
    testRunner.And("all the rule definition measure definition descriptions should match the descript" +
                        "ions in the YAML", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 25
    testRunner.And("all the rule definition measure definitions with Framework property frameworks sh" +
                        "ould match the frameworks in the YAML", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [NUnit.Framework.TestAttribute()]
        [NUnit.Framework.DescriptionAttribute("Age rules")]
        public virtual void AgeRules()
        {
            string[] tagsOfScenario = ((string[])(null));
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Age rules", null, ((string[])(null)));
#line 27
this.ScenarioInitialize(scenarioInfo);
#line hidden
            bool isScenarioIgnored = default(bool);
            bool isFeatureIgnored = default(bool);
            if ((tagsOfScenario != null))
            {
                isScenarioIgnored = tagsOfScenario.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((this._featureTags != null))
            {
                isFeatureIgnored = this._featureTags.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((isScenarioIgnored || isFeatureIgnored))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
                TechTalk.SpecFlow.Table table4 = new TechTalk.SpecFlow.Table(new string[] {
                            "Score",
                            "Age",
                            "Description"});
                table4.AddRow(new string[] {
                            "3",
                            "<P1M",
                            "< 1 month"});
                table4.AddRow(new string[] {
                            "2",
                            "<P3M",
                            "> 1 month"});
                table4.AddRow(new string[] {
                            "1",
                            "\'*\'",
                            "> 3 months"});
                table4.AddRow(new string[] {
                            "0",
                            "",
                            "None"});
#line 28
    testRunner.Given("I have a rule named \'Date of Last IP Review\' with id \'da4ed776-0365-4d8a-a297-c4e" +
                        "91a14d646\' and DataType \'Age\'", ((string)(null)), table4, "Given ");
#line hidden
#line 34
    testRunner.When("I load the rules", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 35
    testRunner.Then("the rule definition name should be \'Date of Last IP Review\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
#line 36
    testRunner.And("the rule definition id should be \'da4ed776-0365-4d8a-a297-c4e91a14d646\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 37
    testRunner.And("the rule definition should have the same number of measures as were in the YAML", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 38
    testRunner.And("all the rule definition measure definitions with Age property should be of type \'" +
                        "Endjin.Imm.Domain.AgeMeasureDefinition\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 39
    testRunner.And("all the rule definition measure definition scores should match the scores in the " +
                        "YAML", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 40
    testRunner.And("all the rule definition measure definition descriptions should match the descript" +
                        "ions in the YAML", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
#line 41
    testRunner.And("all the rule definition measure definitions with Age property frameworks should m" +
                        "atch the frameworks in the YAML", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
            }
            this.ScenarioCleanup();
        }
    }
}
#pragma warning restore
#endregion
