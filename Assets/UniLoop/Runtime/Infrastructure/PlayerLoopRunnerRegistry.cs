using System;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Registry that stores PlayerLoop runners for every timing–phase pair
    /// and provides indexed access and snapshot generation.
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
            => PositionToIndex((int)timing, (int)phase);

        private int PositionToIndex(int timingIndex, int phaseIndex)
            => timingIndex * _phasesPerTiming + phaseIndex;

        private PlayerLoopRunnerPosition IndexToPosition(int index)
            => new((PlayerLoopTiming)(index / _phasesPerTiming), (PlayerLoopPhase)(index % _phasesPerTiming));


        public TimingRunnerSnapshot[] GetSnapshot()
        {
            TimingRunnerSnapshot[] snapshots = new TimingRunnerSnapshot[_runners.Length];
            for (int i = 0; i < _runners.Length; i++)
            {
                var position = IndexToPosition(i);
                var processorSnapshots = _runners[i].GetSnapshots();
                var runnerSnapshot = new TimingRunnerSnapshot(position, processorSnapshots);
                snapshots[i] = runnerSnapshot;
            }
            return snapshots;
        }
    }
}