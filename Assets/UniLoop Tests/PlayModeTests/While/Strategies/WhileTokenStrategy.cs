using System;
using System.Threading;

namespace CatCode.PlayerLoops.Tests
{
    public sealed class WhileTokenStrategy : IWhileStrategy
    {
        private CancellationTokenSource _cts;

        public void Start(
            Func<bool> predicate,
            Action onCompleted,
            Action onCanceled,
            PlayerLoopTiming timing,
            PlayerLoopPhase phase)
        {
            _cts = new CancellationTokenSource();

            UniLoop.Schedule(predicate, _cts.Token, timing, phase)
                .SetOnCompleted(onCompleted)
                .SetOnCanceled(onCanceled);
        }

        public void Cancel()
        {
            if (_cts == null)
                return;
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }
}