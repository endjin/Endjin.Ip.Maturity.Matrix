namespace Endjin.Imm.Domain
{
    using System.IO;
    using Endjin.Imm.Serialisation;
    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    public partial class IpMaturityMatrix
    {
        public static IpMaturityMatrix FromJson(string json) => JsonConvert.DeserializeObject<IpMaturityMatrix>(json, Converter.Settings) ?? new IpMaturityMatrix();

        private static readonly INodeDeserializer nodeDeserializer = new PropertySwitchedNodeDeserializer<MeasureAssertion>(
            (nameof(FrameworkMeasureAssertion.Framework), typeof(FrameworkMeasureAssertion)),
            (nameof(AgeMeasureAssertion.Date), typeof(AgeMeasureAssertion)));

        public static IpMaturityMatrix FromYaml(string yaml)
        {
            using var input = new StringReader(yaml);

            var deserializer = new DeserializerBuilder()
                .WithNodeDeserializer(nodeDeserializer)
                .Build();

            var rules = deserializer.Deserialize<RuleAssertion[]>(input);

            return new IpMaturityMatrix(rules);
        }
    }
}