using System;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Registry that stores PlayerLoop runners for every timing–phase pair and provides indexed access.
    /// </summary>
    internal class PlayerLoopRunnerRegistry
    {
        private readonly PlayerLoopRunner[] _runners;
        private readonly int _phasesPerTiming;

        public PlayerLoopRunnerRegistry(int timingCount, int phasesPerTiming, Func<PlayerLoopRunner> factory)
        {
            _phasesPerTiming = phasesPerTiming;
            var total = timingCount * phasesPerTiming;

            _runners = new PlayerLoopRunner[total];
            for (int i = 0; i < total; i++)
                _runners[i] = factory();
        }

        public PlayerLoopRunner this[PlayerLoopTiming timing, PlayerLoopPhase phase]
            => _runners[PositionToIndex(timing, phase)];

        public PlayerLoopRunner this[PlayerLoopRunnerPosition position]
            => _runners[PositionToIndex(position.Timing, position.Phase)];


        private int PositionToIndex(PlayerLoopTiming timing, PlayerLoopPhase phase)
            => (int)timing * _phasesPerTiming + (int)phase;
    }
}
