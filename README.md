## UniLoop

`UniLoop` provides high-performance C# logic execution directly within the Unity **PlayerLoop**, completely bypassing the need for MonoBehaviour.

* Update outside MonoBehaviour — Run cyclic logic in pure C# classes and services without scene objects.
* Zero Allocation — No heap allocations during task registration, execution, or completion.
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
Comparison processing 10,000 active tasks. A single MonoBehaviour with a foreach loop over a HashSet is taken as the baseline (100%).


|              | Time (ms)  | Speed (%)   |
|--------------------|-----------:|------------:|
| **UniLoop (Slim)** | **2.70**   | **157%**    |
| Update (Baseline)  | 4.23       | 100%        |
| **UniLoop (Default)**| **4.36**       | **97%**         |
| Coroutines         | 10.44      | 40%         |
| UniTask (Yield)    | 23.54      | 18%         |

<details>
  <summary>Test Details</summary> 
  
- `Update`: A MonoBehaviour storing `Action` delegates in a `HashSet` (for easy removal). Addition and removal of new Actions are also deferred.
- `Coroutine`: A separate coroutine was started for each process with `yield return null`.
- `UniTask`: An infinite loop using `UniTask.Yield`.
</details>

### Conditional Cycle (While)
Each frame, 100 new processes were started (lifetime ~1 sec).

#### Registration (Start)


|             | Creation (ms) | Speed (%)    | Alloc (KB)   |
|-------------------|--------------:|-------------:|-------------:|
| Update (Baseline)| 0.31      | 100%     | 28.1     |
| **UniLoop (Default)** | **0.64**          | **47%**          | **28.1**         |
| Coroutine         | 0.67          | 45%          | 36.7         |
| **UniLoop (NoAlloc)**| **0.88**      | **35%**      | **0**        |
| UniTask (Yield)   | 0.94          | 32%          | 35.9         |
| UniTaskWaitUntil  | 1.10          | 28%          | 37.5         |

#### Execution (Update)


|             | Update (ms)   | Speed (%)    | Alloc (KB)  |
|-------------------|--------------:|-------------:|-------------:|
| Update (Baseline)| 2.02      | 100%     | 0      |
| **UniLoop (Default)** | 2.05          | 99%          | 0          |
| **UniLoop (NoAlloc)** | 2.05          | 99%          | 0          |
| WaitUntil         | 2.12          | 95%          | 0          |
| Coroutine         | 3.18          | 63%          | 0          |
| UniTask (Yield)   | 6.49          | 31%          | 0          |

<details>
  <summary>Test Details & NoAlloc Explanation</summary> 
  Each process received a temporary object and two methods: a predicate and a completion callback to return the object to the pool.
  
`UniLoop` (NoAlloc) supports **Generic State Passing**, which eliminates **closures** and **boxing**. Combined with cached method references, this ensures **zero allocations** in the hot execution loop.
</details>

---

### Honest Conclusion

Strictly looking at the numbers:
- Execution Speed: `UniLoop` is on par with a raw `Update` loop and significantly faster than Coroutines or `UniTask` for mass updates.
- Registration Cost: Starting a task requires more CPU than a simple `HashSet.Add`, primarily due to internal pooling logic.

**What you get in return:**
- **Guaranteed execution order** for all tasks.
- **Modern management** via `IDisposable` or `CancellationToken`.
- **Total elimination of heap allocations.**

## Installation

### Unity Package Manager
1. Open the **Package Manager** (`Window` -> `Package Manager`).
2. Click the **"+"** button in the top-left corner.
3. Select **"Add package from git URL..."**.
4. Enter the following URL:

`https://github.com/CatCodeGames/UniLoop.git?path=Assets/PlayerLoop`

## License
This project is licensed under the **MIT License**
