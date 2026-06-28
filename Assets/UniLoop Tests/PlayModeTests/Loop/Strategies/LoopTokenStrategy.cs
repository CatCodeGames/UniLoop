using System;
using System.Threading;

namespace CatCode.PlayerLoops.Tests
{

    public class LoopTokenStrategy : ILoopStrategy
    {
        private CancellationTokenSource _cts;

        public void Start(Action action, Action onCanceled, PlayerLoopTiming timing, PlayerLoopPhase phase)
        {
            _cts = new CancellationTokenSource();

            UniLoop.Schedule(action, _cts.Token, timing, phase)
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