using CatCode.Common;
using System;
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
        internal const int RunnerStartSize = 32;
        internal const int RunnerGrowSize = 32;


        public const int TimingCount = 3;
        public const int PhasesPerTiming = 2;
        public const int ProcessorsCount = 5;


        private static PlayerLoopRunnerRegistry _registry;
        private static PlayerLoopRunnerInserter _inserter;

#if UNITY_2020_1_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#else
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        private static void Initialize()
        {
            _registry = new PlayerLoopRunnerRegistry(TimingCount, PhasesPerTiming, PlayerLoopRunnerFactory.Create);
            _inserter = new PlayerLoopRunnerInserter(_registry, RunnerBiningMap.Bindings);

            _inserter.InserRunners();
            EditorPlayModeActionRegistry.Register(() => _inserter.Dispose(), PlayModeEvent.ExitingPlayMode);
        }


        public static SlimLoopHandle ScheduleSlim(Action action, PlayerLoopTiming timing, PlayerLoopPhase phase)
            => _registry[timing, phase].ScheduleSlim(action);

        public static LoopHandle Schedule(Action action, PlayerLoopTiming timing, PlayerLoopPhase phase)
            => _registry[timing, phase].Schedule(action);

        public static TokenLoopHandle Schedule(Action action, CancellationToken cancellationToken, PlayerLoopTiming timing, PlayerLoopPhase phase)
            => _registry[timing, phase].Schedule(action, cancellationToken);

        public static WhileHandle Schedule(Func<bool> predicate, PlayerLoopTiming timing, PlayerLoopPhase phase)
            => _registry[timing, phase].Schedule(predicate);

        public static TokenWhileHandle Schedule(Func<bool> predicate, CancellationToken cancellationToken, PlayerLoopTiming timing, PlayerLoopPhase phase)
            => _registry[timing, phase].Schedule(predicate, cancellationToken);



        public static ITimingScheduler GetLoop(PlayerLoopTiming timing, PlayerLoopPhase phase)
            => _registry[timing, phase];


        public static ITimingScheduler Update
            => _registry[PlayerLoopTiming.Update, PlayerLoopPhase.Early];

        public static ITimingScheduler FixedUpdate
            => _registry[PlayerLoopTiming.FixedUpdate, PlayerLoopPhase.Early];

        public static ITimingScheduler LateUpdate
            => _registry[PlayerLoopTiming.LateUpdate, PlayerLoopPhase.Early];


        public static ITimingScheduler PostUpdate
            => _registry[PlayerLoopTiming.Update, PlayerLoopPhase.Late];

        public static ITimingScheduler PostFixedUpdate
            => _registry[PlayerLoopTiming.FixedUpdate, PlayerLoopPhase.Late];

        public static ITimingScheduler PostLateUpdate
            => _registry[PlayerLoopTiming.LateUpdate, PlayerLoopPhase.Late];



        public static TimingRunnerSnapshot[] GetSnapshot()
            => _registry.GetSnapshot();

    }
}