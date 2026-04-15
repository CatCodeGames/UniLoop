using System;
using System.Threading;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Central scheduler that coordinates all PlayerLoop runners within a single timing phase
    /// and provides unified scheduling APIs.
    /// </summary>
    internal class PlayerLoopRunner : ITimingScheduler, ILoopScheduler, IWhileScheduler
    {
        private readonly SlimLoopRunner _slimLoopRunner;
        private readonly DefaultLoopRunner _defaultLoopRunner;
        private readonly TokenLoopRunner _tokenLoopRunner;
        private readonly DefaultWhileRunner _defaultWhileRunner;
        private readonly TokenWhileRunner _tokenWhileRunner;

        public ILoopScheduler Loop => this;
        public IWhileScheduler While => this;

        internal PlayerLoopRunner(
            SlimLoopRunner slimLoopRunner,
            DefaultLoopRunner defaultLoopRunner,
            TokenLoopRunner tokenLoopRunner,
            DefaultWhileRunner defaultWhileRunner,
            TokenWhileRunner tokenWhileRunner)
        {
            _slimLoopRunner = slimLoopRunner;
            _defaultLoopRunner = defaultLoopRunner;
            _tokenLoopRunner = tokenLoopRunner;
            _defaultWhileRunner = defaultWhileRunner;
            _tokenWhileRunner = tokenWhileRunner;
        }

        public SlimLoopHandle ScheduleSlim(Action action)
            => new(_slimLoopRunner, _slimLoopRunner.Add(action));

        public LoopHandle Schedule(Action action)
            => new(_defaultLoopRunner, _defaultLoopRunner.Schedule(action));

        public TokenLoopHandle Schedule(Action action, CancellationToken cancellationToken)
            => new(_tokenLoopRunner, _tokenLoopRunner.Schedule(action, cancellationToken));

        public WhileHandle Schedule(Func<bool> predicate)
            => new(_defaultWhileRunner, _defaultWhileRunner.Schedule(predicate));

        public TokenWhileHandle Schedule(Func<bool> predicate, CancellationToken cancellationToken)
            => new(_tokenWhileRunner, _tokenWhileRunner.Schedule(predicate, cancellationToken));


        public void Run()
        {
            _slimLoopRunner.Run();
            _defaultLoopRunner.Run();
            _tokenLoopRunner.Run();
            _defaultWhileRunner.Run();
            _tokenWhileRunner.Run();
        }

        public RunnerSnapshot[] GetSnapshots()
        {
            var snapshots = new RunnerSnapshot[]
                {
                    new (UniLoopRunnerType.SlimLoop, _slimLoopRunner.GetMetrics()),
                    new (UniLoopRunnerType.Loop, _defaultLoopRunner.GetMetrics()),
                    new (UniLoopRunnerType.TokenLoop, _tokenLoopRunner.GetMetrics()),
                    new (UniLoopRunnerType.While, _defaultWhileRunner.GetMetrics()),
                    new (UniLoopRunnerType.TokenWhile, _tokenWhileRunner.GetMetrics()),
                };
            return snapshots;
        }
    }
}