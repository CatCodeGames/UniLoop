
namespace CatCode.PlayerLoops.Tests
{
    public sealed class LoopTestRunner
    {
        private readonly ILoopStrategy _strategy;

        private int _value;
        public int CurrentValue => _value;
        public bool Canceled { get; private set; }

        public LoopTestRunner(ILoopStrategy strategy)
        {
            _strategy = strategy;
        }

        public void Start(PlayerLoopTiming timing, PlayerLoopPhase phase)
        {
            Canceled = false;
            _value = 0;

            _strategy.Start(
                Action,
                OnCanceled,
                timing,
                phase);
        }

        public void Cancel()
            => _strategy.Cancel();

        private void Action()
        {
            _value++;
        }

        private void OnCanceled()
        {
            Canceled = true;
        }
    }
}