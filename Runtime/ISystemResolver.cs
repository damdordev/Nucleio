using System.Collections.Generic;

namespace Damdor.Nucleio
{
    /// <summary>
    /// Provides a mechanism to resolve a list of systems.
    /// </summary>
    /// <typeparam name="TSystem">The base type for systems.</typeparam>
    public interface ISystemResolver<TSystem> where TSystem : ISystem
    {
        /// <summary>
        /// Resolves and returns a list of systems.
        /// </summary>
        /// <returns>A list of resolved systems.</returns>
        List<TSystem> Resolve();
    }
}