using System;

namespace CatCode.PlayerLoops.Tests
{
    public interface ILoopStrategy
    {
        void Start(Action action, Action onCanceled, PlayerLoopTiming timing, PlayerLoopPhase phase);
        void Cancel();
    }
}