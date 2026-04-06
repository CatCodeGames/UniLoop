using CatCode.Collections;
using System;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Handle for a running PlayerLoop action controlled by an external cancellation token.
    /// </summary>
    public readonly struct TokenLoopHandle
    {
        private readonly ILoopRegistry  _runner;
        private readonly ElementHandle _handle;

        public bool IsDisposed => _runner == null || !_runner.IsValid(_handle);

        public TokenLoopHandle(ILoopRegistry runner, ElementHandle handle)
        {
            _runner = runner;
            _handle = handle;
        }

        public TokenLoopHandle SetOnCanceled(Action action)
        {
            if (!IsDisposed)
                _runner.SetOnCanceled(_handle, action);
            return this;
        }

        public TokenLoopHandle SetOnCanceled(Action<object> action, object state)
        {
            if (!IsDisposed)
                _runner.SetOnCanceled(_handle, action, state);
            return this;
        }
    }
}