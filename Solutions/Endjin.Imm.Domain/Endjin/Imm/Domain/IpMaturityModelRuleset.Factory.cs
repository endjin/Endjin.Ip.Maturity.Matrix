namespace Endjin.Imm.Domain
{
    using System.IO;
    using Endjin.Imm.Serialisation;

    public partial class IpMaturityModelRuleset
    {
        /// <summary>
        /// The branch of the rule definitions repository that holds the canonical rule set, and that
        /// is used when no other branch is requested.
        /// </summary>
        public const string DefaultDefinitionsBranch = "main";

        /// <summary>
        /// The URL for the canonical rule set definitions.
        /// </summary>
        public static readonly string RuleSetDefinitionsUrl = DefinitionsUrlForName(DefaultDefinitionsBranch);

        public static string DefinitionsUrlForName(string name) => $"https://raw.githubusercontent.com/endjin/Endjin.Ip.Maturity.Matrix.RuleDefinitions/{name}/RuleSet.yaml";

        public static IpMaturityModelRuleset FromYaml(string yaml)
        {
            using var input = new StringReader(yaml);

            var rules = ImmYamlDeserializer.Shared.Deserialize<RuleDefinition[]>(input);

            // An empty document (or one containing only comments) deserializes to null: that is a rule set with no rules.
            return new IpMaturityModelRuleset(rules ?? []);
        }
    }
}
