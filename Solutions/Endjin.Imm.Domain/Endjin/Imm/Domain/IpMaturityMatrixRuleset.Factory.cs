namespace Endjin.Imm.Domain
{
    using System.IO;
    using Endjin.Imm.Serialisation;
    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public partial class IpMaturityMatrixRuleset
    {
        /// <summary>
        /// The URL for the canonical rule set definitions.
        /// </summary>
        public static readonly string RuleSetDefinitionsUrl = DefinitionsUrlForName("master");

        private static readonly INodeDeserializer nodeDeserializer = new PropertySwitchedNodeDeserializer<MeasureDefinition>(
            (nameof(FrameworkMeasureDefinition.Framework), typeof(FrameworkMeasureDefinition)),
            (nameof(AgeMeasureDefinition.Age), typeof(AgeMeasureDefinition)));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1055:Uri return values should not be strings", Justification = "We don't have any particular use for representing URIs as anything other than a string in this project")]
        public static string DefinitionsUrlForName(string name) => $"https://raw.githubusercontent.com/endjin/Endjin.Ip.Maturity.Matrix.RuleDefinitions/{name}/RuleSet.yaml";

        public static IpMaturityMatrixRuleset FromJson(string json) => JsonConvert.DeserializeObject<IpMaturityMatrixRuleset>(json, Converter.Settings) ?? new IpMaturityMatrixRuleset();

        public static IpMaturityMatrixRuleset FromYaml(string yaml)
        {
            using var input = new StringReader(yaml);

            var deserializer = new DeserializerBuilder()
                .WithNodeDeserializer(nodeDeserializer)
                .Build();

            var rules = deserializer.Deserialize<RuleDefinition[]>(input);

            return new IpMaturityMatrixRuleset(rules);
        }
    }
}