using CatCode.Collections;
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

        public bool IsDisposed => _runner == null || !_runner.IsValid(_handle);

        public LoopHandle(ILoopController runner, ElementHandle handle)
        {
            _runner = runner;
            _handle = handle;
        }


        public LoopHandle SetOnCanceled(Action action)
        {
            if (!IsDisposed)
                _runner.SetOnCanceled(_handle, action);
            return this;
        }

        public LoopHandle SetOnCanceled<T>(Action<T> action, T state)
        {
            if (!IsDisposed)
                _runner.SetOnCanceled(_handle, action, state);
            return this;
        }

        public void Dispose()
        {
            if (!IsDisposed)
                _runner.Cancel(_handle);
        }
    }
}