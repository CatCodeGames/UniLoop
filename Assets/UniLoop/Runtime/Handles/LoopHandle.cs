using CatCode.PlayerLoops.Collections;
using System;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Handle for a running PlayerLoop action with cancellation support.
    /// </summary>
    public readonly struct LoopHandle : IDisposable
    {
        private readonly ILoopController _runner;
        private readonly ElementHandle _handle;

        public bool IsDisposed => _runner == null || !_runner.IsActive(_handle);


        public LoopHandle(ILoopController runner, ElementHandle handle)
        {
            _runner = runner;
            _handle = handle;
        }


        public LoopHandle SetOnCanceled(Action action)
        {
            _runner?.SetOnCanceled(_handle, action);
            return this;
        }

        public LoopHandle SetOnCanceled<T>(Action<T> action, T state) where T : class
            => SetOnCanceled(state, action);

        public LoopHandle SetOnCanceled<T>(T state, Action<T> action) where T : class
        {
            _runner?.SetOnCanceled(_handle, state, action);
            return this;
        }


        public void Dispose()
            => _runner?.Cancel(_handle);
    }
}
