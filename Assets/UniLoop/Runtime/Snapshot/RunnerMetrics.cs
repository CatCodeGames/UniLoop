
namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Runner types in the order they run within one phase.
    /// </summary>
    public enum UniLoopRunnerType
    {
        SlimLoop,
        Loop,
        TokenLoop,
        CountedLoop,
        While,
        TokenWhile
    }

    /// <summary>
    /// Task counts of one runner.
    /// </summary>
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
