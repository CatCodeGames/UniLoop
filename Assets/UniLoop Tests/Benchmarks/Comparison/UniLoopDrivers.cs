using System.Threading;

namespace CatCode.PlayerLoops.Benchmarks
{
    /// <summary>
    /// No tasks at all. Gives the frame time of the empty test scene to compare the other methods against.
    /// </summary>
    internal sealed class EmptyDriver : ITaskDriver
    {
        public void Start() { }
        public void Stop() { }
        public void Dispose() { }
    }

    internal sealed class UniLoopSlimDriver : ITaskDriver
    {
        private readonly SlimLoopHandle[] _handles;

        public UniLoopSlimDriver(int count) => _handles = new SlimLoopHandle[count];

        public void Start()
        {
            for (int i = 0; i < _handles.Length; i++)
                _handles[i] = UniLoop.Update.Loop.ScheduleSlim(BenchmarkWork.Tick);
        }

        public void Stop()
        {
            for (int i = 0; i < _handles.Length; i++)
                _handles[i].Dispose();
        }

        public void Dispose() => Stop();
    }

    internal sealed class UniLoopLoopDriver : ITaskDriver
    {
        private readonly LoopHandle[] _handles;

        public UniLoopLoopDriver(int count) => _handles = new LoopHandle[count];

        public void Start()
        {
            for (int i = 0; i < _handles.Length; i++)
                _handles[i] = UniLoop.Update.Loop.Schedule(BenchmarkWork.Tick);
        }

        public void Stop()
        {
            for (int i = 0; i < _handles.Length; i++)
                _handles[i].Dispose();
        }

        public void Dispose() => Stop();
    }

    /// <summary>
    /// Token Loop tasks stopped together through one shared CancellationTokenSource,
    /// the direct counterpart of the UniTask driver.
    /// </summary>
    internal sealed class UniLoopTokenLoopDriver : ITaskDriver
    {
        private readonly int _count;
        private CancellationTokenSource _cts;

        public UniLoopTokenLoopDriver(int count) => _count = count;

        public void Start()
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            for (int i = 0; i < _count; i++)
                UniLoop.Update.Loop.Schedule(BenchmarkWork.Tick, token);
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
    }

    internal sealed class UniLoopWhileDriver : ITaskDriver
    {
        private readonly WhileHandle[] _handles;

        public UniLoopWhileDriver(int count) => _handles = new WhileHandle[count];

        public void Start()
        {
            for (int i = 0; i < _handles.Length; i++)
                _handles[i] = UniLoop.Update.While.Schedule(BenchmarkWork.TickWhile);
        }

        public void Stop()
        {
            for (int i = 0; i < _handles.Length; i++)
                _handles[i].Dispose();
        }

        public void Dispose() => Stop();
    }
}
