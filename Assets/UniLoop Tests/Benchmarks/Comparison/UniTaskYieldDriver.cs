using Cysharp.Threading.Tasks;
using System.Threading;

namespace CatCode.PlayerLoops.Benchmarks
{
    /// <summary>
    /// One async loop per task with UniTask.Yield, stopped through a shared CancellationToken.
    /// Cancellation is not thrown, so Stop is not dominated by exceptions.
    /// </summary>
    internal sealed class UniTaskYieldDriver : ITaskDriver
    {
        private readonly int _count;
        private CancellationTokenSource _cts;

        public UniTaskYieldDriver(int count) => _count = count;

        public void Start()
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            for (int i = 0; i < _count; i++)
                Loop(token).Forget();
        }

        public void Stop()
        {
            if (_cts == null)
                return;

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        public void Dispose() => Stop();

        private static async UniTaskVoid Loop(CancellationToken token)
        {
            while (true)
            {
                BenchmarkWork.Tick();

                // PlayerLoopTiming here is the UniTask one, not the UniLoop enum with the same name.
                var isCanceled = await UniTask
                    .Yield(Cysharp.Threading.Tasks.PlayerLoopTiming.Update, token)
                    .SuppressCancellationThrow();

                if (isCanceled)
                    return;
            }
        }
    }
}
