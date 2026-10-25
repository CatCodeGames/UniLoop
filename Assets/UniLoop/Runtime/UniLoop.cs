using CatCode.Common;
using System;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Entry point for scheduling work into the Unity PlayerLoop,
    /// providing a unified, timing‑aware API over all loop and while runners.
    /// </summary>
    public static class UniLoop
    {
        public const int TimingCount = 3;
        public const int PhasesPerTiming = 2;


        private static PlayerLoopRunnerRegistry _registry;
        private static PlayerLoopRunnerInserter _inserter;
        private static int _mainThreadId;

        // Runs first on every Play Mode start, also when Domain Reload is disabled,
        // so state from the previous session never leaks into the new one.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _registry = null;
            _inserter = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void InitializeOnLoad()
            => EnsureInitialized();

        private static PlayerLoopRunnerRegistry Registry
        {
            get
            {
                EnsureInitialized();
                return _registry;
            }
        }

        private static void EnsureInitialized()
        {
            if (_registry != null)
                return;

            ValidateAccess();

            _registry = new PlayerLoopRunnerRegistry(TimingCount, PhasesPerTiming, PlayerLoopRunnerFactory.Create);
            _inserter = new PlayerLoopRunnerInserter(_registry, RunnerBindingMap.Bindings);

            var inserter = _inserter;
            inserter.InsertRunners();
            EditorPlayModeActionRegistry.Register(inserter.Dispose, PlayModeEvent.ExitingPlayMode);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        internal static void ValidateAccess()
        {
            if (_mainThreadId == 0)
                throw new InvalidOperationException("UniLoop is available only in Play Mode.");

            if (Thread.CurrentThread.ManagedThreadId != _mainThreadId)
                throw new InvalidOperationException("UniLoop must be called from the main thread.");
        }


        public static SlimLoopHandle ScheduleSlim(Action action, PlayerLoopTiming timing, PlayerLoopPhase phase)
            => Registry[timing, phase].ScheduleSlim(action);

        public static LoopHandle Schedule(Action action, PlayerLoopTiming timing, PlayerLoopPhase phase)
            => Registry[timing, phase].Schedule(action);

        public static TokenLoopHandle Schedule(Action action, CancellationToken cancellationToken, PlayerLoopTiming timing, PlayerLoopPhase phase)
            => Registry[timing, phase].Schedule(action, cancellationToken);

        public static WhileHandle Schedule(Action action, int count, PlayerLoopTiming timing, PlayerLoopPhase phase)
            => Registry[timing, phase].Schedule(action, count);

        public static WhileHandle ScheduleOnce(Action action, PlayerLoopTiming timing, PlayerLoopPhase phase)
            => Registry[timing, phase].ScheduleOnce(action);

        public static WhileHandle Schedule(Func<bool> predicate, PlayerLoopTiming timing, PlayerLoopPhase phase)
            => Registry[timing, phase].Schedule(predicate);

        public static TokenWhileHandle Schedule(Func<bool> predicate, CancellationToken cancellationToken, PlayerLoopTiming timing, PlayerLoopPhase phase)
            => Registry[timing, phase].Schedule(predicate, cancellationToken);



        public static ITimingScheduler GetLoop(PlayerLoopTiming timing, PlayerLoopPhase phase)
            => Registry[timing, phase];


        public static ITimingScheduler Update
            => Registry[PlayerLoopTiming.Update, PlayerLoopPhase.Early];

        public static ITimingScheduler FixedUpdate
            => Registry[PlayerLoopTiming.FixedUpdate, PlayerLoopPhase.Early];

        public static ITimingScheduler LateUpdate
            => Registry[PlayerLoopTiming.LateUpdate, PlayerLoopPhase.Early];


        public static ITimingScheduler PostUpdate
            => Registry[PlayerLoopTiming.Update, PlayerLoopPhase.Late];

        public static ITimingScheduler PostFixedUpdate
            => Registry[PlayerLoopTiming.FixedUpdate, PlayerLoopPhase.Late];

        public static ITimingScheduler PostLateUpdate
            => Registry[PlayerLoopTiming.LateUpdate, PlayerLoopPhase.Late];



        /// <summary>
        /// Task counts of one runner, without allocations. Returns zeros before UniLoop is initialized
        /// and never initializes it, so it is safe to call from Editor tools.
        /// </summary>
        public static RunnerMetrics GetMetrics(PlayerLoopTiming timing, PlayerLoopPhase phase, UniLoopRunnerType type)
            => _registry != null
                ? _registry[timing, phase].GetMetrics(type)
                : default;

    }
}