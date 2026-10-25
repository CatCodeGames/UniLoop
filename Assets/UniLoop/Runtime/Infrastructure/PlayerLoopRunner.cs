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
        private readonly CountedLoopRunner _countedLoopRunner;
        private readonly DefaultWhileRunner _defaultWhileRunner;
        private readonly TokenWhileRunner _tokenWhileRunner;

        public ILoopScheduler Loop => this;
        public IWhileScheduler While => this;

        internal PlayerLoopRunner(
            SlimLoopRunner slimLoopRunner,
            DefaultLoopRunner defaultLoopRunner,
            TokenLoopRunner tokenLoopRunner,
            CountedLoopRunner countedLoopRunner,
            DefaultWhileRunner defaultWhileRunner,
            TokenWhileRunner tokenWhileRunner)
        {
            _slimLoopRunner = slimLoopRunner;
            _defaultLoopRunner = defaultLoopRunner;
            _tokenLoopRunner = tokenLoopRunner;
            _countedLoopRunner = countedLoopRunner;
            _defaultWhileRunner = defaultWhileRunner;
            _tokenWhileRunner = tokenWhileRunner;
        }

        public SlimLoopHandle ScheduleSlim(Action action)
        {
            UniLoop.ValidateAccess();
            return new(_slimLoopRunner, _slimLoopRunner.Add(action));
        }

        public LoopHandle Schedule(Action action)
        {
            UniLoop.ValidateAccess();
            return new(_defaultLoopRunner, _defaultLoopRunner.Schedule(action));
        }

        public TokenLoopHandle Schedule(Action action, CancellationToken cancellationToken)
        {
            UniLoop.ValidateAccess();
            return new(_tokenLoopRunner, _tokenLoopRunner.Schedule(action, cancellationToken));
        }

        public WhileHandle Schedule(Action action, int count)
        {
            UniLoop.ValidateAccess();
            return new(_countedLoopRunner, _countedLoopRunner.Schedule(action, count));
        }

        public WhileHandle ScheduleOnce(Action action)
            => Schedule(action, 1);

        public WhileHandle Schedule(Func<bool> predicate)
        {
            UniLoop.ValidateAccess();
            return new(_defaultWhileRunner, _defaultWhileRunner.Schedule(predicate));
        }

        public TokenWhileHandle Schedule(Func<bool> predicate, CancellationToken cancellationToken)
        {
            UniLoop.ValidateAccess();
            return new(_tokenWhileRunner, _tokenWhileRunner.Schedule(predicate, cancellationToken));
        }


        public void Run()
        {
            _slimLoopRunner.Run();
            _defaultLoopRunner.Run();
            _tokenLoopRunner.Run();
            _countedLoopRunner.Run();
            _defaultWhileRunner.Run();
            _tokenWhileRunner.Run();
        }

        public RunnerMetrics GetMetrics(UniLoopRunnerType type)
            => type switch
            {
                UniLoopRunnerType.SlimLoop => _slimLoopRunner.GetMetrics(),
                UniLoopRunnerType.Loop => _defaultLoopRunner.GetMetrics(),
                UniLoopRunnerType.TokenLoop => _tokenLoopRunner.GetMetrics(),
                UniLoopRunnerType.CountedLoop => _countedLoopRunner.GetMetrics(),
                UniLoopRunnerType.While => _defaultWhileRunner.GetMetrics(),
                UniLoopRunnerType.TokenWhile => _tokenWhileRunner.GetMetrics(),
                _ => throw new ArgumentOutOfRangeException(nameof(type)),
            };
    }
}
