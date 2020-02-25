namespace Endjin.Imm.Contracts
{
    using System.Threading.Tasks;

    /// <summary>
    /// Provides the ability to retrieve rule definitions. This fetches them from the 
    /// https://github.com/endjin/Endjin.Ip.Maturity.Matrix.RuleDefinitions repository. It caches
    /// the results.
    /// </summary>
    public interface IRuleDefinitionRepositorySource
    {
        /// <summary>
        /// Gets the rule definitions from GitHub as defined on the specified branch.
        /// </summary>
        /// <param name="name">
        /// The branch name (or any other object name that GitHub accepts, such as a tag or commit)
        /// from which to fetch the rule definitions.
        /// </param>
        /// <returns>A task that produces a rule definition repository.</returns>
        Task<IRuleDefinitionRepository> GetRuleDefinitionRepositoryAsync(string name);
    }
}