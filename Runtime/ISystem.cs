namespace Damdor.Nucleio
{
    /// <summary>
    /// Defines a system that can be managed by a <see cref="Game{TSystem}"/>.
    /// </summary>
    public interface ISystem
    {
        /// <summary>
        /// Gets a value indicating whether this system supports multithreading.
        /// If true, <see cref="Create"/>, <see cref="Start"/> methods
        /// might be called from a background thread.
        /// </summary>
        bool MultithreadingSupported { get; }
        
        /// <summary>
        /// Called when the system is created.
        /// Use this to initialize resources.
        /// </summary>
        void Create();
        
        /// <summary>
        /// Called when the game starts, after all systems have been created.
        /// </summary>
        void Start();
        
        /// <summary>
        /// Called every frame.
        /// </summary>
        void Update();
        
        /// <summary>
        /// Called when the game stops.
        /// Use this to release resources.
        /// </summary>
        void Stop();
    }
}