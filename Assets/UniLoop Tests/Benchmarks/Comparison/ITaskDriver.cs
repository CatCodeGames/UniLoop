using System;

namespace CatCode.PlayerLoops.Benchmarks
{
    /// <summary>
    /// Starts and stops a batch of per-frame tasks with one particular method (UniLoop, Update, coroutines, UniTask).
    /// Storage for the batch is allocated in the constructor, so Start and Stop measure only the method itself.
    /// </summary>
    internal interface ITaskDriver : IDisposable
    {
        void Start();
        void Stop();
    }

    /// <summary>
    /// The work every task does each frame. A shared counter keeps the call from being optimized away.
    /// </summary>
    internal static class BenchmarkWork
    {
        public static int Counter;

        public static readonly Action Tick = () => Counter++;

        public static readonly Func<bool> TickWhile = () =>
        {
            Counter++;
            return true;
        };
    }
}
