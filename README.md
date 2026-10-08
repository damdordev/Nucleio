# Nucleio

Nucleio is a lightweight and flexible framework for building system-based game architectures in Unity, heavily relying on `Cysharp.Threading.Tasks` (UniTask) for asynchronous execution.

It allows developers to organize game logic into modular systems with predictable lifecycles, and offers built-in support for multithreading, allowing multiple systems to be updated in parallel to optimize performance.


## Table of Contents

- [Core Concepts](#core-concepts)
- [Installation](#installation)
- [Usage](#usage)
  - [1. Define your systems](#1-define-your-systems)
  - [2. Set up the Resolver](#2-set-up-the-resolver)
  - [3. Create your game class](#3-create-your-game-class)
  - [3. Run the Game](#3-run-the-game)
- [Multithreading](#multithreading)
- [Configuration Flags](#configuration-flags)
- [Dependencies](#dependencies)

## Core Concepts

- **`ISystem`**: The base interface that all systems must implement. It defines the lifecycle methods (`Create`, `Start`, `Update`, `Stop`) and a property to indicate if it supports multithreading.
- **`Game<TSystem>`**: The core manager that controls the lifecycle of all registered systems. It handles initialization, starting the game loop, running updates, and stopping the game gracefully.
- **`ISystemResolver<TSystem>`**: An interface for discovering and providing systems to the `Game`.
- **`ListSystemResolver<TSystem>`**: A simple resolver that accepts a manually defined list of system instances.
- **`AttributeBasedSystemResolver<TSystem, TAttribute>`**: A dynamic resolver that uses reflection to find all system types in specified assemblies decorated with a specific attribute, and instantiates them.

## Installation

This package is currently under development. In the future, it will be available via a UPM registry. For now, you can install it using the Git URL.

**Option A: Install via Package Manager window**
1. In Unity, open **Window** > **Package Manager**.
2. Click the **+** button and select **Add package from git URL...**
3. Enter the following URL and click **Add**:
   `https://github.com/damdordev/Nucleio.git#1.0.0-preview`

**Option B: Install via `manifest.json`**
Open your project's `Packages/manifest.json` file and add the following line to your `"dependencies"` block:
```json
"com.damdor.nucleio": "https://github.com/damdordev/Nucleio.git#1.0.0-preview"
```

## Usage

### 1. Define your systems

Implement the `ISystem` interface. 

```csharp
using Damdor.Nucleio;
using UnityEngine;

// Define your own base interface to be used by the Game
public interface IMyGameSystem : ISystem { }

[NucleioSystem]
public class MovementSystem : IMyGameSystem
{
    public bool MultithreadingSupported => true; // Can run in parallel with other multithreaded systems

    public void Create() { /* Initialize resources */ }
    public void Start() { /* Game started */ }
    public void Update() { /* Called every frame */ }
    public void Stop() { /* Cleanup resources */ }
}

[NucleioSystem]
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
// Finds all classes implementing IMyGameSystem and decorated with [NucleioSystem]
var assemblies = new List<Assembly> { typeof(MovementSystem).Assembly };
var resolver = new AttributeBasedSystemResolver<IMyGameSystem, NucleioSystemAttribute>(assemblies);
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
