using System;

namespace CatCode.PlayerLoops.Tests
{
    public interface IWhileStrategy
    {
        void Start(
            Func<bool> predicate,
            Action onCompleted,
            Action onCanceled,
            PlayerLoopTiming timing,
            PlayerLoopPhase phase);

        void Cancel();
    }
}