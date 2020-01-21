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
        public const string RuleSetDefinitionsUrl = "https://raw.githubusercontent.com/endjin/Endjin.Ip.Maturity.Matrix.RuleDefinitions/master/RuleSet.yaml";

        private static readonly INodeDeserializer nodeDeserializer = new PropertySwitchedNodeDeserializer<MeasureDefinition>(
            (nameof(FrameworkMeasureDefinition.Framework), typeof(FrameworkMeasureDefinition)),
            (nameof(AgeMeasureDefinition.Age), typeof(AgeMeasureDefinition)));

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