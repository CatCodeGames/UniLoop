namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Map of predefined runner bindings for inserting PlayerLoop runners at specific timings and phases.
    /// </summary>
    internal static class RunnerBiningMap
    {
        public static readonly RunnerBinding[] Bindings = new RunnerBinding[]
        {
            new (PlayerLoopTiming.FixedUpdate,
                 PlayerLoopPhase.Early,
                 typeof(UnityEngine.PlayerLoop.FixedUpdate),
                 typeof(UniLoopRunner.FixedUpdateBegin),
                 null,
                 InsertPosition.Begin),

            new (PlayerLoopTiming.FixedUpdate,
                PlayerLoopPhase.Late,
                typeof(UnityEngine.PlayerLoop.FixedUpdate),
                typeof(UniLoopRunner.FixedUpdateEnd),
                null,
                InsertPosition.End),


             new(PlayerLoopTiming.Update,
                PlayerLoopPhase.Early,
                typeof(UnityEngine.PlayerLoop.Update),
                typeof(UniLoopRunner.UpdateBegin),
                null,
                InsertPosition.Begin),

            new(PlayerLoopTiming.Update,
                PlayerLoopPhase.Late,
                typeof(UnityEngine.PlayerLoop.Update),
                typeof(UniLoopRunner.UpdateEnd),
                null,
                InsertPosition.End),


            new(PlayerLoopTiming.LateUpdate,
                PlayerLoopPhase.Early,
                typeof(UnityEngine.PlayerLoop.PreLateUpdate),
                typeof(UniLoopRunner.PreLateUpdateBegin),
                null,
                InsertPosition.Begin),

            new(PlayerLoopTiming.LateUpdate,
                PlayerLoopPhase.Late,
                typeof(UnityEngine.PlayerLoop.PreLateUpdate),
                typeof(UniLoopRunner.PreLateUpdateEnd),
                null,
                InsertPosition.End)
        };
    }
}