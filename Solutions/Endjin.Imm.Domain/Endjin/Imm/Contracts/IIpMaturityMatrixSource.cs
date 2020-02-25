namespace Endjin.Imm.Contracts
{
    using Endjin.Imm.Domain;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides the ability to retrieve the maturity matrix from a GitHub repository. It caches
    /// the results.
    /// </summary>
    public interface IIpMaturityMatrixSource
    {
        /// <summary>
        /// Gets the <see cref="IpMaturityMatrix"/> from a GitHub repo, on the specifed branch.
        /// </summary>
        /// <param name="gitHubOwner">
        /// The organization or account that owns the repository from which to fetch the IMM.
        /// (E.g., `corvus-dotnet`.)</param>
        /// <param name="gitHubProject">
        /// The repository from which to fetch the IMM. (E.g., `Corvus.Monitoring`.)
        /// </param>
        /// <param name="objectName">
        /// The branch name (or any other object name that GitHub accepts, such as a tag or commit)
        /// from which to fetch the IMM.
        /// </param>
        /// <returns></returns>
        Task<IpMaturityMatrix> GetIpMaturityMatrixAsync(
            string gitHubOwner,
            string gitHubProject,
            string objectName);
    }
}
