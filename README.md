## UniLoop

`UniLoop` provides high-performance C# logic execution directly within the Unity **PlayerLoop**, completely bypassing the need for MonoBehaviour.

* Update outside MonoBehaviour — Run cyclic logic in pure C# classes and services without scene objects.
* Zero Allocation — No heap allocations during task registration, execution, or completion.
* No Performance Spikes — Predictable load without spikes at any stage of the task lifecycle.
* Max Performance — Low-level injection directly into the PlayerLoop systems.
* Safe Modification — Add or remove tasks safely during the loop iteration.
* Deterministic Order — Tasks are executed strictly in the order they were registered.
* CancellationToken Support — Built-in integration for modern lifecycle management.
* IDisposable Handles — Precise manual control over running processes.
* Editor Safety — Automatic cleanup of injected systems when exiting Play Mode.
* Zero-Alloc State — Pass data to callbacks without closures or boxing.

### A Note from the Author (Why UniLoop?)
<details>
<summary> Expand </summary>

This library is not an attempt to "beat" the native Update speed or replace it entirely. It was born from a few simple frustrations:
- MonoBehaviour Overhead: I was tired of turning pure C# classes into MonoBehaviour or dragging references just for a single update loop.
- Direct Access: Singletons in the scene are a standard solution, but why access the engine's loop through a game object proxy when you can have direct access?
- Workflow Control: UniTask.Yield solves the problem, but managing its lifecycle isn't always convenient, and its efficiency for mass updates is lower.

In **Unreal Engine**, running logic in the world tick without being tied to an object is a standard practice, and it felt natural to bring that to Unity. With `UniLoop`, I aimed to combine the **Zero-Alloc** philosophy of **UniTask** with the **Fluent API** convenience of **DOTween**. 

P.S. Inside the library, you'll find some interesting solutions, such as specialized collections and handle-based management. Feel free to use these components separately in your own projects if they fit. I hope `UniLoop` makes your development life a bit easier! :)
</details>



## Table of Contents
- [Usage](#usage)
- [Architecture and API](#architecture-and-api)
- [Showcase](#showcase)
- [Advanced Optimization](#advanced-optimization)
- [Performance](#performance)
- [Installation](#installation)
- [License](#license)



## Usage

```csharp
// --- 1. Infinite Cycle (Loop) ---
// Fluent API: [Timing] -> [Phase] -> [Type] -> [Schedule]
LoopHandle handle = UniLoop.Update.Loop.Schedule(() => Debug.Log("Tick"));
handle.Dispose(); // Stop the process via handle

// Registration via universal Schedule method
LoopHandle handle = UniLoop.Schedule(() => Debug.Log("Tick"), PlayerLoopTiming.Update, PlayerLoopPhase.Early);

// Adding a callback for process cancellation
LoopHandle handle = UniLoop.FixedUpdate.Loop.Schedule(() => Debug.Log("Tick"))
    .SetOnCanceled(() => Debug.Log("Canceled"));

// Registration with CancellationToken
UniLoop.Update.Loop.Schedule(() => Debug.Log("Tick"), _cts.Token);
_cts.Cancel();


// --- 2. Conditional Cycle (While) ---
// Runs as long as the predicate returns true
WhileHandle handle = UniLoop.Update.While.Schedule(() => 
    {
        _interval -= Time.deltaTime;
        return _interval > 0;
    });

// While registration with support for completion and cancellation events
UniLoop.Update.While.Schedule(predicateFunc, _cts.Token)
    .SetOnCanceled(() => Debug.Log("Canceled"))    // Triggered on Dispose/Cancel
    .SetOnCompleted(() => Debug.Log("Completed")); // Triggered when predicate returns false

// Using 'using' guarantees stopping when exiting the scope
using (var handle = UniLoop.Update.Loop.Schedule(() => Debug.Log("Tick")))
{    
    await DoSomethingAsync();
}
```


## Architecture and API

`UniLoop` acts as a unified entry point for running logic within the Unity PlayerLoop by combining **Timings**, **Phases** and **Process Types**.

### Process Types
- `Loop` (Infinite cycle) — Executes every frame until manually stopped or cancelled via a token.
- `While` (Conditional cycle) — Executes as long as the predicate returns `true`. Supports both cancellation and completion events.

### Execution Points (Timings & Phases)
Precise control over when your code executes within the Unity frame:
- `PlayerLoopTiming` — Select the loop: Update, FixedUpdate, or LateUpdate.
- `PlayerLoopPhase` — Select the execution window: Early (start of the cycle) or Late (end of the cycle).

### Execution Order
The execution sequence of operation types within a single phase:
- Slim Loop - Lightweight cycle. Unlike other types, modifying the collection (add/remove) during iteration is not safe.
- Loop - Infinite cycle.
- Loop (with CancellationToken) - Infinite cycle with CancellationToken support.
- While - Conditional cycle.
- While (with CancellationToken) - Conditional cycle with CancellationToken support.

### Execution Methods
- **Fluent API** — Sequential selection through a call chain:
```cs
UniLoop.Update.Loop.Schedule(() => ...)
```
- **Universal `Schedule` method** — Execution by explicitly passing `PlayerLoopTiming` and `PlayerLoopPhase` parameters.

### Handles and Process Management
Each registration returns a **struct handle** — a lightweight descriptor to control the process.

- **For Infinite Cycles (Loop)**
  - `LoopHandle` — handle with a `.Dispose()` method for manual stop.
  - `TokenLoopHandle` — for processes with a `CancellationToken`.
Both variants support registering a cancellation callback via `.SetOnCanceled()`.*

- **For Conditional Cycles (While)**
  - `WhileHandle` — handle with a `.Dispose()` method for manual stop.
  - `TokenWhileHandle` — for processes with a `CancellationToken`.
Support for subscribing to the completion of a process via `.SetOnCompleted()`.


## Showcase

TimeScale controller example: activates the multiplier calculation only when the first modifier is added and completely shuts down when the list is empty.
<details>
  <summary>Note</summary>
The code is intentionally simplified to demonstrate lifecycle management via UniLoop.
</details>    
    
```csharp
public class TimeScaleController
{
    private readonly List<ITimeScaleModifier> _modifiers = new();
    private readonly float originalTimeScale;
    private LoopHandle _loopHandle;

    public TimeScaleController() => originalTimeScale = Time.timeScale;

    public void AddModifier(ITimeScaleModifier modifier)
    {
        _modifiers.Add(modifier);
            
        // If this is the first modifier — start the update loop
        if (_modifiers.Count == 1)
        {
            _loopHandle = UniLoop.Schedule(UpdateTimeScale, PlayerLoopTiming.Update, PlayerLoopPhase.Early)
                .SetOnCanceled(() => Time.timeScale = originalTimeScale);
        }
    }

    public void RemoveModifier(ITimeScaleModifier modifier)
    {
        if (_modifiers.Remove(modifier) && _modifiers.Count == 0)
        {
            // If no modifiers are left — completely stop the loop
            _loopHandle.Dispose();
        }
    }

    private void UpdateTimeScale()
    {
        // Calculate average value based on dynamic data from modifiers
        var averageMultiplier = _modifiers.Average(q => q.Multiplier);
        Time.timeScale = originalTimeScale * averageMultiplier;
    }
}
```



## Advanced Optimization

### Slim Loop
Lightweight version of the loop. Ideal for global systems running throughout the app's lifetime.
- Fastest way to iterate in the PlayerLoop.
- Limitation: If `Dispose()` is called during execution, the task will be removed in the next frame.
- Registration via `UniLoop.Update.Loop.ScheduleSlim(action)` or `UniLoop.ScheduleSlim(...)`.


### Zero-Alloc & Delegate Caching
Using cached delegates and State (structs) eliminates closures and boxing during registration to achieve 0B GC Alloc.

```csharp
public sealed class MyClass
{
    private readonly struct PoolContext // Context as a readonly struct
    {
        public readonly IObjectPool<GameObject> Pool;
        public readonly GameObject Entity;
        public PoolContext(IObjectPool<GameObject> pool, GameObject entity) => (Pool, Entity) = (pool, entity);
    }

    // Cache method references once during initialization
    private readonly Func<bool> _cachedPredicate;
    private readonly Action<PoolContext> _cachedOnCompleted;
    private IObjectPool<GameObject> _pool;

    public MyClass()
    {
        _cachedPredicate = Predicate;
        _cachedOnCompleted = OnCompleted;
    }

    public void Run()
    {
        var obj = _pool.Get();
        // Passing PoolContext through State — no allocations
        UniLoop.Update.While.Schedule(_cachedPredicate)
            .SetOnCompleted(_cachedOnCompleted, new PoolContext(_pool, obj));
    }

    private void OnCompleted(PoolContext ctx) => ctx.Pool.Release(ctx.Entity);
    private bool Predicate() => /* logic */ true;
}
```
### DeferredDenseArray
Specialized collection at the core of UniLoop.
- Dense Packing: All active tasks are stored in memory without "holes". Despite using reference types, this guarantees strict execution order and the fastest possible iteration.
- Deferred Updates: Task additions and removals are buffered and applied only at safe moments. This ensures iteration stability without "collection modified" errors.



---
## Performance

### Infinite Cycle (Loop)
Benchmarking 10,000 active concurrent tasks in a single frame.

| Method | Registration (ms) | Execution (ms) | Stop (ms) | GC Alloc |
| :--- | :---: | :---: | :---: | :---: |
| **UniLoop (Slim)** | **1.51** | **0.10** | **0.10** | **0 B** |
| **UniLoop (Default)** | **2.35** | **0.26** | **1.72** | **0 B** |
| Update (Baseline)* | 3.21 | 0.78 | 1.08 | 0 B |
| Coroutines | 15.20 | 5.88 | 5.88 | 0.8 MB |
| UniTask.Yield | 5.32 | 1.92 | 1.92 | 0.6 MB |

*\*Baseline: A single MonoBehaviour iterating over an Action collection in a standard Update loop.*

### Conditional Cycle (While)
Continuous registration and completion of tasks every frame (pipeline workload).

#### Scenario A: 100 new tasks / frame (lifetime 100 frames, ~10,000 active)

| Method | Start (ms) | Update (ms) | Cancel (ms) | GC Alloc |
| :--- | :---: | :---: | :---: | :---: |
| **UniLoop (No Alloc)** | **0.10** | **0.55** | **4.31** | **0 B** |
| UniLoop (Default) | 0.11 | 0.58 | 3.59 | 28.1 KB |
| Update (Baseline) | 0.07 | 0.59 | 3.25 | 28.1 KB |
| UniTask.Yield | 0.15 | 2.62 | 2.62 | 35.9 KB |
| Coroutine | 0.34 | 6.04 | 6.04 | 36.7 KB |
| UniTask.WaitUntil | 0.17 | 0.85 | 85.70 | 4.5 MB |

#### Scenario B: 1000 new tasks / frame (lifetime 10 frames, ~10,000 active)

| Method | Start (ms) | Update (ms) | Cancel (ms) | GC Alloc |
| :--- | :---: | :---: | :---: | :---: |
| **UniLoop (No Alloc)** | **0.73** | **0.93** | **4.50** | **0 B** |
| UniLoop (Default) | 0.80 | 0.93 | 3.29 | 281.2 KB |
| Update (Baseline) | 0.55 | 1.44 | 3.49 | 281.2 KB |
| UniTask.Yield | 1.43 | 3.17 | 3.17 | 359.4 KB |
| Coroutine | 2.23 | 6.17 | 6.17 | 367.2 KB |
| UniTask.WaitUntil | 1.70 | 2.21 | 90.02 | 4.9 MB |

> **Note:** Pay attention to `UniTask.WaitUntil`. Mass cancellation triggers an "exception flood," resulting in critical memory spikes (**4.9 MB**) and CPU freezes (**90 ms**). UniLoop in **No Alloc** mode maintains a stable FPS and zero heap allocations.


### Conclusion

- **Registration** — CPU overhead is comparable to standard collection additions and UniTask. No Alloc mode allows for zero memory pressure during task creation.
- **Execution** — in dynamic scenarios (continuous task pipeline), outperforms standard iteration (foreach) and async methods. Using Slim Loop reduces execution time significantly compared to the native Update.
- **Completion & Cancellation** — minimal impact on frame performance. Unlike async solutions, the system maintains stable FPS and zero allocations even during the simultaneous termination of thousands of tasks.


#### Key Advantages:
* Guaranteed execution orde within the frame.
* Zero-Alloc Suppor — ability to run without allocations throughout the entire task lifecycle.
* Flexible contro via `IDisposable` handles or `CancellationToken`.
* No performance spike at any stage of the task lifecycle.



## Installation

### Unity Package Manager
1. Open the **Package Manager** (`Window` -> `Package Manager`).
2. Click the **"+"** button in the top-left corner.
3. Select **"Add package from git URL..."**.
4. Enter the following URL:

`https://github.com/CatCodeGames/UniLoop.git?path=Assets/UniLoop`

## License
This project is licensed under the **MIT License**
