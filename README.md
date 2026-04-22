# Nucleio

Nucleio is a lightweight and flexible framework for building system-based game architectures in Unity, heavily relying on `Cysharp.Threading.Tasks` (UniTask) for asynchronous execution.

It allows developers to organize game logic into modular systems with predictable lifecycles, and offers built-in support for multithreading, allowing multiple systems to be updated in parallel to optimize performance.

## Core Concepts

- **`ISystem`**: The base interface that all systems must implement. It defines the lifecycle methods (`Create`, `Start`, `Update`, `Stop`) and a property to indicate if it supports multithreading.
- **`Game<TSystem>`**: The core manager that controls the lifecycle of all registered systems. It handles initialization, starting the game loop, running updates, and stopping the game gracefully.
- **`ISystemResolver<TSystem>`**: An interface for discovering and providing systems to the `Game`.
- **`ListSystemResolver<TSystem>`**: A simple resolver that accepts a manually defined list of system instances.
- **`AttributeBasedSystemResolver<TSystem, TAttribute>`**: A dynamic resolver that uses reflection to find all system types in specified assemblies decorated with a specific attribute, and instantiates them.

## Usage

### 1. Define your systems

Implement the `ISystem` interface. 

```csharp
using Damdor.Nucleio;
using UnityEngine;

// Define your own base interface to be used by the Game
public interface IMyGameSystem : ISystem { }

[System]
public class MovementSystem : IMyGameSystem
{
    public bool MultithreadingSupported => true; // Can run in parallel with other multithreaded systems

    public void Create() { /* Initialize resources */ }
    public void Start() { /* Game started */ }
    public void Update() { /* Called every frame */ }
    public void Stop() { /* Cleanup resources */ }
}

[System]
public class RenderSystem : IMyGameSystem
{
    public bool MultithreadingSupported => false; // Must run on the main thread

    public void Create() { }
    public void Start() { }
    public void Update() { }
    public void Stop() { }
}
```

### 2. Set up the Resolver

You can either pass a predefined list of systems or use reflection to discover them automatically using the `AttributeBasedSystemResolver`.

**Manual List:**
```csharp
var systems = new List<IMyGameSystem> 
{
    new MovementSystem(),
    new RenderSystem()
};
var resolver = new ListSystemResolver<IMyGameSystem>(systems);
```

**Attribute-Based:**
```csharp
// Finds all classes implementing IMyGameSystem and decorated with [System]
var assemblies = new List<Assembly> { typeof(MovementSystem).Assembly };
var resolver = new AttributeBasedSystemResolver<IMyGameSystem, SystemAttribute>(assemblies);
```

### 3. Create your game class

public class MyGame : Game<IMyGameSystem>
{
    // here you can implement your onw callbacks
}

### 3. Run the Game

Instantiate the `Game` and call `Run()`. You can enable or disable multithreading here.

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;
using Damdor.Nucleio;
using System.Reflection;
using System.Collections.Generic;

public class GameBootstrap : MonoBehaviour
{
    private MyGame game;

    private async void Start()
    {
        var resolver = new AttributeBasedSystemResolver<IMyGameSystem, SystemAttribute>();
        
        // Initialize Game, passing the resolver and enabling multithreading
        game = MyGame()
        {
            SystemResolver = new AttributeBasedSystemResolver<IMyGameSystem, SystemAttribute>(),
            Multithreading = true,
            CleanUniTaskOnStop = true // Optional: clears UniTask runners and yielders when the game stops
        };

        // Run the game loop
        await game.Run();
    }

    private void OnDestroy()
    {
        game?.Stop().Forget();
    }
}
```

## Multithreading

When `Multithreading` is enabled in the `Game`:
- Systems where `MultithreadingSupported` returns `true` will be executed on Unity's ThreadPool during the `Update` loop using `UniTask.SwitchToThreadPool()`.
- Systems where `MultithreadingSupported` returns `false` will be executed on the main thread sequentially.
- The `Update` phase finishes only when both the multithreaded and non-multithreaded systems have completed their updates for the current frame.

## Configuration Flags

- **`Multithreading`**: Enable or disable multithreading for updating systems.
- **`CleanUniTaskOnStop`**: When enabled, stopping the game will aggressively clean up UniTask's internal yielders and runners using reflection. This is useful when stopping and restarting the game loop frequently or tearing down the entire game context to prevent potential memory leaks or hanging tasks.

## Dependencies

- [UniTask](https://github.com/Cysharp/UniTask): Required for asynchronous control flow (`UniTask`).