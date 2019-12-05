namespace Endjin.Imm.Domain
{
    using System.Collections.Generic;
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

        public static IpMaturityMatrixRuleset FromJson(string json) => JsonConvert.DeserializeObject<IpMaturityMatrixRuleset>(json, Converter.Settings);

        public static IpMaturityMatrixRuleset FromYaml(string yaml)
        {
            var input = new StringReader(yaml);

            var deserializer = new DeserializerBuilder().Build();

            var rules = deserializer.Deserialize<List<RuleDefinition>>(input);

            return new IpMaturityMatrixRuleset { Rules = rules.ToArray() };
        }
    }
}