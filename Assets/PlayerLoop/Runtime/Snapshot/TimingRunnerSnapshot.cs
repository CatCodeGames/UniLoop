
namespace CatCode.PlayerLoops
{
    public enum UniLoopRunnerType
    {
        SlimLoop,
        Loop,
        TokenLoop,
        While,
        TokenWhile
    }

    public readonly struct TimingRunnerSnapshot
    {
        public readonly PlayerLoopRunnerPosition Position;
        public readonly RunnerSnapshot[] RunnerSnapshots;

        public TimingRunnerSnapshot(PlayerLoopRunnerPosition position, RunnerSnapshot[] runnerSnapshots )
        {
            Position = position;
            RunnerSnapshots = runnerSnapshots;
        }
    }

    public readonly struct RunnerSnapshot
    {
        public readonly UniLoopRunnerType Type;
        public readonly RunnerMetrics Metrics;

        public RunnerSnapshot(UniLoopRunnerType type, RunnerMetrics metrics)
        {
            Type = type;
            Metrics = metrics;
        }
    }

    public readonly struct RunnerMetrics
    {
        public readonly int Count;
        public readonly int PendingAddCount;
        public readonly int TotalCount;

        public RunnerMetrics(int count, int pendingAddCount, int totalCount)
        {
            Count = count;
            PendingAddCount = pendingAddCount;
            TotalCount = totalCount;
        }
    }
}