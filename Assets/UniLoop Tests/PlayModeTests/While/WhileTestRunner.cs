namespace CatCode.PlayerLoops.Tests
{
    public sealed class WhileTestRunner
    {
        private readonly IWhileStrategy _strategy;
        private readonly int _target;

        private int _value;

        public int CurrentValue => _value;
        public bool Completed { get; private set; }
        public bool Canceled { get; private set; }

        public WhileTestRunner(IWhileStrategy strategy, int target)
        {
            _strategy = strategy;
            _target = target;
        }

        public void Start(PlayerLoopTiming timing, PlayerLoopPhase phase)
        {
            Completed = false;
            Canceled = false;
            _value = 0;

            _strategy.Start(
                Predicate,
                OnCompleted,
                OnCanceled,
                timing,
                phase);
        }

        public void Cancel()
        {
            _strategy.Cancel();
        }

        private bool Predicate()
        {
            _value++;
            return _value < _target;
        }

        private void OnCompleted() => Completed = true;
        private void OnCanceled() => Canceled = true;
    }
}