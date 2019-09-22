namespace Endjin.Imm.Domain
{
    using System.Collections.Generic;
    using System.IO;
    using Endjin.Imm.Serialisation;
    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public partial class IpMaturityMatrix
    {
        public static IpMaturityMatrix FromJson(string json) => JsonConvert.DeserializeObject<IpMaturityMatrix>(json, Converter.Settings);

        public static IpMaturityMatrix FromYaml(string yaml)
        {
            var input = new StringReader(yaml);

            var deserializer = new DeserializerBuilder().Build();

            var rules = deserializer.Deserialize<List<Rule>>(input);

            return new IpMaturityMatrix { Rules = rules.ToArray() };
        }
    }
}