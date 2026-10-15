namespace Endjin.Imm.Domain
{
    using System.IO;
    using Endjin.Imm.Serialisation;

    public partial class IpMaturityMatrix
    {
        public static IpMaturityMatrix FromYaml(string yaml)
        {
            using var input = new StringReader(yaml);

            var rules = ImmYamlDeserializer.Shared.Deserialize<RuleAssertion[]>(input);

            // An empty document (or one containing only comments) deserializes to null: that is an IMM with no rules.
            return new IpMaturityMatrix(rules ?? []);
        }
    }
}
