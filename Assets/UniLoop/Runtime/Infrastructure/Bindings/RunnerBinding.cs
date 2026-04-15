using CatCode.PlayerLoops.LowLevel;
using System;


namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Binding that defines where a runner is placed in the PlayerLoop and how it is inserted.
    /// </summary>
    internal readonly struct RunnerBinding
    {
        public readonly PlayerLoopRunnerPosition Position;
        public readonly PlayerLoopInsertSettings Insert;

        public RunnerBinding(PlayerLoopRunnerPosition position, PlayerLoopInsertSettings insert) : this()
        {
            Position = position;
            Insert = insert;
        }


        public RunnerBinding(PlayerLoopTiming timing, PlayerLoopPhase phase, Type rootType, Type insertType, Type targetType, InsertPosition position)
        {
            Position = new PlayerLoopRunnerPosition(timing, phase);
            Insert = new PlayerLoopInsertSettings(rootType, insertType, targetType, position);
        }
    }
}