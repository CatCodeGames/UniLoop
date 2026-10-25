using CatCode.PlayerLoops.Collections;
using System;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Handle for a running PlayerLoop action controlled by an external cancellation token.
    /// </summary>
    public readonly struct TokenLoopHandle
    {
        private readonly ILoopRegistry _runner;
        private readonly ElementHandle _handle;

        public bool IsActive => _runner != null && _runner.IsActive(_handle);

        public TokenLoopHandle(ILoopRegistry runner, ElementHandle handle)
        {
            _runner = runner;
            _handle = handle;
        }

        public TokenLoopHandle SetOnCanceled(Action action)
        {
            _runner?.SetOnCanceled(_handle, action);
            return this;
        }

        public TokenLoopHandle SetOnCanceled<T>(Action<T> action, T state) where T : class
            => SetOnCanceled(state, action);

        public TokenLoopHandle SetOnCanceled<T>(T state, Action<T> action) where T : class
        {
            _runner?.SetOnCanceled(_handle, state, action);
            return this;
        }
    }
}
