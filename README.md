## UniLoop

`UniLoop` provides high-performance C# logic execution directly within the Unity **PlayerLoop**, completely bypassing the need for MonoBehaviour.

* Update outside MonoBehaviour — Run cyclic logic in pure C# classes and services without scene objects.
* Zero Allocation — No heap allocations during task registration, execution, or completion.
* No Performance Spikes — Predictable load without spikes at any stage of the task lifecycle.
* Max Performance — Low-level injection directly into the PlayerLoop systems.
* Safe Modification — Add or remove tasks safely during the loop iteration.
* Deterministic Order — Tasks of the same type are executed strictly in the order they were registered.
* Error Isolation — An exception in one task is logged and does not stop the others.
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
// Fluent API: [Timing + Phase] -> [Type] -> [Schedule]
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


// --- 2. Counted Cycle (Counted Loop) ---
// Runs the given number of times, then calls OnCompleted
UniLoop.Update.Loop.Schedule(() => Debug.Log("Tick"), 3)
    .SetOnCompleted(() => Debug.Log("Done"));

// Single call on the next execution of the phase
UniLoop.Update.Loop.ScheduleOnce(() => Debug.Log("Once"));

// Delay by N frames: an empty action plus a completion callback
UniLoop.Update.Loop.Schedule(EmptyAction, 10).SetOnCompleted(DoWork);


// --- 3. Conditional Cycle (While) ---
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
- `Counted Loop` (Counted cycle) — Executes the given number of times, then completes. `ScheduleOnce` is the case of a single run. A run counts even if the action throws.
- `While` (Conditional cycle) — Executes as long as the predicate returns `true`. Supports both cancellation and completion events.

### Execution Points (Timings & Phases)
Precise control over when your code executes within the Unity frame:
- `PlayerLoopTiming` — Select the loop: Update, FixedUpdate, or LateUpdate.
- `PlayerLoopPhase` — Select the execution window: Early (start of the cycle) or Late (end of the cycle).

Fluent API properties and their insertion points:

| Property | Timing | Phase | PlayerLoop insertion point |
| :--- | :--- | :--- | :--- |
| `UniLoop.FixedUpdate` | FixedUpdate | Early | start of `FixedUpdate` |
| `UniLoop.PostFixedUpdate` | FixedUpdate | Late | end of `FixedUpdate` |
| `UniLoop.Update` | Update | Early | start of `Update` |
| `UniLoop.PostUpdate` | Update | Late | end of `Update` |
| `UniLoop.LateUpdate` | LateUpdate | Early | start of `PreLateUpdate` |
| `UniLoop.PostLateUpdate` | LateUpdate | Late | end of `PreLateUpdate` |

### Execution Order
The execution sequence of operation types within a single phase. Tasks of different types run in groups in this order, not in the order `Schedule` was called:
- Slim Loop - Lightweight cycle. Cancellation during iteration takes effect from the next frame, so the task may run once more in the current frame.
- Loop - Infinite cycle.
- Loop (with CancellationToken) - Infinite cycle with CancellationToken support.
- Counted Loop - Counted cycle.
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
Both variants support registering a cancellation callback via `.SetOnCanceled()`.

- **For Conditional Cycles (While) and Counted Cycles (Counted Loop)**
  - `WhileHandle` — handle with a `.Dispose()` method for manual stop.
  - `TokenWhileHandle` — for processes with a `CancellationToken`.
Support for subscribing to the completion of a process via `.SetOnCompleted()`.

`IsDisposed` becomes `true` right after `Dispose()`. The task itself is removed from the loop on the next frame, and the cancellation callback is invoked at that point.

`TokenLoopHandle` and `TokenWhileHandle` have no `Dispose()`: the token stops the task. `IsActive` tells whether the task is still running and becomes `false` right after the token is canceled.

### Limitations
- Requires Unity 2021.3 or newer.
- The API must be called from the main thread and only in Play Mode. In the Editor and development builds a violation throws `InvalidOperationException`.
- On exiting Play Mode all scheduled tasks are dropped without calling `onCanceled`.


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
- Limitation: If `Dispose()` is called while the loop is running, the task may run once more in the current frame. From the next frame on it no longer runs.
- Registration via `UniLoop.Update.Loop.ScheduleSlim(action)` or `UniLoop.ScheduleSlim(...)`.


### Zero-Alloc & Delegate Caching
Using cached delegates and State eliminates closures during registration to achieve 0B GC Alloc. State must be a reference type (`where T : class`), so there is no boxing either.

```csharp
public sealed class MyClass
{
    // Cache method references once during initialization
    private readonly Func<bool> _cachedPredicate;
    private readonly Action<GameObject> _cachedOnCompleted;
    private IObjectPool<GameObject> _pool;

    public MyClass()
    {
        _cachedPredicate = Predicate;
        _cachedOnCompleted = OnCompleted;
    }

    public void Run()
    {
        var obj = _pool.Get();
        // Passing the object through State — no allocations
        UniLoop.Update.While.Schedule(_cachedPredicate)
            .SetOnCompleted(_cachedOnCompleted, obj);
    }

    private void OnCompleted(GameObject entity) => _pool.Release(entity);
    private bool Predicate() => /* logic */ true;
}
```
### DeferredDenseArray
Specialized collection at the core of UniLoop.
- Dense Packing: All active tasks are stored in memory without "holes". Despite using reference types, this guarantees strict execution order and the fastest possible iteration.
- Deferred Updates: Task additions and removals are buffered and applied only at safe moments. This ensures iteration stability without "collection modified" errors.



---
## Performance
Every method runs the same work in `Update`. Values are medians. **Empty frame** is a frame without tasks; compare frame times against it. **GC** is managed memory allocated on top of the empty frame.

Setup: Unity 6000.3, Editor, Mono (Code Optimization: Debug), Ryzen 5 5600. In Release and in a player build the absolute numbers are lower. The benchmarks live in `UniLoop Tests/Benchmarks` and can be run from the Test Runner.

### 10,000 tasks: start, run and stop at once

| Method | Start, ms | GC on start | Frame, ms | GC per frame | Stop*, ms |
| :--- | ---: | ---: | ---: | ---: | ---: |
| *Empty frame* | — | — | *4.01* | — | — |
| **UniLoop Slim** | 1.40 | **0** | 4.29 | **0** | 0.5 |
| **UniLoop Loop** | 2.78 | **0** | 4.23 | **0** | 0.4 |
| **UniLoop Loop + CancellationToken** | 2.85 | **0** | 4.28 | **0** | 0.5 |
| **UniLoop While** | 3.51 | **0** | 4.08 | **0** | 0.5 |
| Custom manager (`List<Action>` in `Update`) | 0.21 | 0 | 4.23 | 0 | 0.1 |
| Coroutine | 13.96 | 547 KB | 8.30 | 0 | 394 |
| UniTask.Yield | 8.13 | 1.8 MB | 14.49 | 234 KB | 4.9 |

*\*Stop is the call itself plus the deferred work in the next frame: removal from lists and cancellation handling.*

### Pipeline: 1,000 new tasks every frame, 10-frame lifetime (~10,000 active)

**Tasks are stopped manually, one by one**

| Method | Start and cancel per frame, ms | Frame, ms | GC per frame |
| :--- | ---: | ---: | ---: |
| *Empty frame* | — | *4.07* | — |
| **UniLoop Loop** | 0.37 | 4.25 | **0** |
| Custom manager | 0.06 | 4.07 | 0 |
| Coroutine | 79.03 | 10.27 | 56 KB |
| UniTask.Yield (token per task) | 1.18 | 14.57 | 688 KB |

**Tasks end by themselves**

| Method | Start per frame, ms | Frame, ms | GC per frame |
| :--- | ---: | ---: | ---: |
| *Empty frame* | — | *3.93* | — |
| **UniLoop Counted** | 0.42 | 4.35 | **0** |
| **UniLoop While** (counter in a closure) | 0.44 | 4.63 | 145 KB |
| Custom manager | 0.04 | 4.18 | 0 |
| Coroutine | 1.37 | 9.14 | 63 KB |
| UniTask.Yield | 0.39 | 6.07 | 47 KB |

### Conclusion
- **No allocations** at any stage: start, run, cancellation, completion. Coroutines and UniTask allocate from tens of kilobytes to megabytes.
- **Running** 10,000 tasks is almost indistinguishable from an empty frame. Coroutines add about 4 ms, UniTask about 10 ms.
- **Stable cost.** UniLoop costs the same whether tasks are canceled manually or end by themselves: about 0.4 ms per 1,000 operations per frame. Stopping coroutines one by one costs 79 ms per frame; UniTask with a token per task adds 10 ms to the frame and 688 KB.
- **While with a closure** allocates per task: that is the closure in user code. For "run N times" use the counted loop, which allocates nothing.

### Why not just `List<Action>`?
A custom `List<Action>` manager is faster at starting and stopping, but all it does is iterate a list. UniLoop adds on top of that:

- **Safe changes during the loop** — start or stop a task right from another task.
- **Protected handles** — generation-checked: a repeated `Dispose` or a stale handle breaks nothing.
- **Error isolation** — an exception in one task does not stop the others.
- **Callbacks and cancellation** — `SetOnCompleted`, `SetOnCanceled`, `CancellationToken`, "while a condition holds" and "N times" tasks.
- **Any point of the frame** — start and end of `Update`, `FixedUpdate` and `LateUpdate` without extra MonoBehaviours.
- **State without closures** — data is passed to callbacks with no allocation.

The price is tenths of a millisecond per thousand operations per frame; in a typical game that is microseconds.



## Installation

### Unity Package Manager
1. Open the **Package Manager** (`Window` -> `Package Manager`).
2. Click the **"+"** button in the top-left corner.
3. Select **"Add package from git URL..."**.
4. Enter the following URL:

`https://github.com/CatCodeGames/UniLoop.git?path=Assets/UniLoop`

## License
This project is licensed under the **MIT License**
