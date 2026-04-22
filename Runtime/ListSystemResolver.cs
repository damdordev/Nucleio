using System.Collections.Generic;

namespace Damdor.Nucleio
{
    /// <summary>
    /// A system resolver that returns a predefined list of systems.
    /// </summary>
    /// <typeparam name="TSystem">The base type for systems.</typeparam>
    public class ListSystemResolver<TSystem> : ISystemResolver<TSystem> where TSystem : ISystem
    {
        private readonly List<TSystem> systems;

        /// <summary>
        /// Initializes a new instance of the <see cref="ListSystemResolver{TSystem}"/> class.
        /// </summary>
        /// <param name="systems">The predefined list of systems to resolve.</param>
        public ListSystemResolver(List<TSystem> systems)
        {
            this.systems = systems;
        }
        
        /// <summary>
        /// Resolves and returns the predefined list of systems.
        /// </summary>
        /// <returns>The list of resolved systems.</returns>
        public List<TSystem> Resolve() => systems;
    }
}