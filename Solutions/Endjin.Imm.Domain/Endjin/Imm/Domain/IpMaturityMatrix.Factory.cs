namespace Endjin.Imm.Domain
{
    using System.IO;
    using Endjin.Imm.Serialisation;

    public partial class IpMaturityMatrix
    {
        public static IpMaturityMatrix FromYaml(string yaml)
        {
            using var input = new StringReader(yaml);

            var rules = ImmYamlDeserializer.Create().Deserialize<RuleAssertion[]>(input);

            return new IpMaturityMatrix(rules);
        }
    }
}
