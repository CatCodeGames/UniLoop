using System;

namespace CatCode.PlayerLoops.Tests
{
    public class LoopHandleStrategy : ILoopStrategy
    {
        private LoopHandle _handle;
        
        public void Start(Action action, Action onCanceled, PlayerLoopTiming timing, PlayerLoopPhase phase)
        {
            _handle = UniLoop.Schedule(action,timing,phase)
                .SetOnCanceled(onCanceled);
        }
        public void Cancel()
        {
            _handle.Dispose();
        }
    }
}