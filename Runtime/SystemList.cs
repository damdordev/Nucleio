using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Damdor.Nucleio
{
    /// <summary>
    /// Represents a list of systems and provides methods for executing actions on them.
    /// </summary>
    /// <typeparam name="TSystem">The base type for systems.</typeparam>
    public class SystemList<TSystem> where TSystem : ISystem
    {
        /// <summary>
        /// Gets all systems in the list.
        /// </summary>
        public IReadOnlyList<TSystem> AllSystems => systems;

        private readonly List<TSystem> systems;
        private readonly List<TSystem> multithreadedSystems;
        private readonly List<TSystem> notMultithreadedSystems;
        private readonly bool multithreading;
        private readonly CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemList{TSystem}"/> class.
        /// </summary>
        /// <param name="systems">The list of systems to manage.</param>
        /// <param name="multithreading">A value indicating whether to enable multithreading for systems that support it.</param>
        public SystemList(IList<TSystem> systems, bool multithreading)
        {
            this.systems = new List<TSystem>(systems);
            this.multithreading = multithreading;
            multithreadedSystems = systems.Where(s => s.MultithreadingSupported).ToList();
            notMultithreadedSystems = systems.Where(s => !s.MultithreadingSupported).ToList();
            cancellationTokenSource = new CancellationTokenSource();
        }
        
        /// <summary>
        /// Releases all resources used by the <see cref="SystemList{TSystem}"/>.
        /// </summary>
        public void Release()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }

        /// <summary>
        /// Gets a system of the specified type from the list.
        /// </summary>
        /// <typeparam name="T">The type of the system to get.</typeparam>
        /// <returns>The system of the specified type, or the default value if not found.</returns>
        public T GetSystem<T>() where T : TSystem
        {
            return systems.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Executes an action on all systems, potentially using multithreading for supported systems.
        /// </summary>
        /// <param name="action">The action to execute on each system.</param>
        /// <param name="cancellationToken">The cancellation token to observe.</param>
        public async UniTask RunMultithread(Action<TSystem> action, CancellationToken cancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token);
            
            if (multithreading)
            {
                await RunWithMultithreading(action, cts.Token);
            }
            else
            {
                Run(action);
            }
        }

        /// <summary>
        /// Executes an action on all systems sequentially on the main thread.
        /// </summary>
        /// <param name="action">The action to execute on each system.</param>
        public void Run(Action<TSystem> action)
        {
            foreach (var system in AllSystems)
            {
                if (cancellationTokenSource.IsCancellationRequested) return;
                action(system);
            }
        }

        private UniTask RunWithMultithreading(Action<TSystem> action, CancellationToken cancellationToken) =>
            UniTask.WhenAll(
                RunInThreadPool(multithreadedSystems, action, cancellationToken),
                RunInMainThread(notMultithreadedSystems, action, cancellationToken)
            );


        private UniTask RunInThreadPool(List<TSystem> systems, Action<TSystem> action, CancellationToken cancellationToken) =>
            systems.Count == 0 
                ? UniTask.CompletedTask 
                : UniTask.WhenAll(systems.Select(s => RunInThreadPool(s, action, cancellationToken)));

        private UniTask RunInMainThread(IEnumerable<TSystem> systems, Action<TSystem> action, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return UniTask.CompletedTask;
            foreach (var system in systems) action(system);

            return UniTask.CompletedTask;
        }
        
        private async UniTask RunInThreadPool(TSystem system, Action<TSystem> action, CancellationToken cancellationToken)
        {
            await UniTask.SwitchToThreadPool();
            if (cancellationToken.IsCancellationRequested) return;
            action(system);
        }
        
    }
}