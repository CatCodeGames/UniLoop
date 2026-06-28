using System;

namespace CatCode.PlayerLoops.Tests
{
    public sealed class WhileHandleStrategy : IWhileStrategy
    {
        private WhileHandle _handle;

        public void Start(
            Func<bool> predicate,
            Action onCompleted,
            Action onCanceled,
            PlayerLoopTiming timing,
            PlayerLoopPhase phase)
        {
            _handle = UniLoop.Schedule(predicate, timing, phase)
                .SetOnCompleted(onCompleted)
                .SetOnCanceled(onCanceled);
        }

        public void Cancel()
        {
            _handle.Dispose();
        }
    }
}