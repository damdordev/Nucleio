using System;
using System.Reflection;
using Cysharp.Threading.Tasks;

namespace Damdor.Nucleio
{
    /// <summary>
    /// Manages the lifecycle of systems in the game.
    /// </summary>
    /// <typeparam name="TSystem">The base type for systems managed by this game instance.</typeparam>
    public class Game<TSystem> where TSystem : ISystem
    {
        /// <summary>
        /// Gets or sets the resolver used to retrieve the systems managed by this game.
        /// </summary>
        public ISystemResolver<TSystem> SystemResolver { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether multithreading is enabled for the systems.
        /// </summary>
        public bool Multithreading { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether UniTask runners and yielders should be cleared when the game stops.
        /// Useful when the entire game/application context is torn down to avoid memory leaks or dangling tasks.
        /// </summary>
        public bool CleanUniTaskOnStop { get; set; }
        
        private SystemList<TSystem> systems;
        private bool isRunning;

        /// <summary>
        /// Starts the game and all associated systems.
        /// </summary>
        public async UniTask Run()
        {
            await BeforeRun();
            
            systems = new SystemList<TSystem>(SystemResolver.Resolve(), Multithreading);
            await AfterSystemsResolved(systems);

            await systems.RunMultithread(s => s.Create());
            await systems.RunMultithread(s => s.Start());
            
            isRunning = true;
            Loop().Forget();
            await AfterRun();
        }

        /// <summary>
        /// Stops the game and all associated systems.
        /// </summary>
        public  void Stop()
        {
            BeforeStop();
            isRunning = false;
            systems.Run(s => s.Stop());
            systems.Release();
            if(CleanUniTaskOnStop) CleanUniTask();
        }

        private async UniTask Loop()
        {
            while (isRunning)
            {
                BeforeUpdate();
                systems.Run(s => s.Update());
                AfterUpdate();
                await UniTask.NextFrame();
            }
        }

        /// <summary>
        /// Called before the game starts.
        /// </summary>
        protected virtual UniTask BeforeRun() => UniTask.CompletedTask;
        
        /// <summary>
        /// Called after the game starts.
        /// </summary>
        protected virtual UniTask AfterRun() => UniTask.CompletedTask;
        
        /// <summary>
        /// Called before the game stops.
        /// </summary>
        protected virtual void BeforeStop()  {}
        
        /// <summary>
        /// Called after systems have been resolved.
        /// </summary>
        /// <param name="systems">The list of resolved systems.</param>
        protected virtual UniTask AfterSystemsResolved(SystemList<TSystem> systems) => UniTask.CompletedTask;
        
        /// <summary>
        /// Called before the update loop.
        /// </summary>
        protected virtual void BeforeUpdate() {}
        
        /// <summary>
        /// Called after the update loop.
        /// </summary>
        protected virtual void AfterUpdate() {}
        
        private void CleanUniTask()
        {
            var parameters = Array.Empty<object>();
            var type = typeof(PlayerLoopHelper);

            var runners = (Array)type.GetField("runners", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            foreach(var runner in runners)
            {
                runner.GetType().GetMethod("Clear").Invoke(runner, parameters);
            }

            var yielders = (Array)type.GetField("yielders", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null);
            foreach(var yielder in yielders)
            {
                yielder.GetType().GetMethod("Clear").Invoke(yielder, parameters);
            }

        }
    }
}
