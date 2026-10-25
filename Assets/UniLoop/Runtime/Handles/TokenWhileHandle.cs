using CatCode.PlayerLoops.Collections;
using System;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Handle for a running PlayerLoop while-process controlled by an external cancellation token.
    /// </summary>
    public readonly struct TokenWhileHandle
    {
        private readonly IWhileRegistry _runner;
        private readonly ElementHandle _handle;

        public bool IsActive => _runner != null && _runner.IsActive(_handle);

        public TokenWhileHandle(IWhileRegistry runner, ElementHandle handle) : this()
        {
            _runner = runner;
            _handle = handle;
        }


        public TokenWhileHandle SetOnCompleted(Action action)
        {
            _runner?.SetOnCompleted(_handle, action);
            return this;
        }

        public TokenWhileHandle SetOnCanceled(Action action)
        {
            _runner?.SetOnCanceled(_handle, action);
            return this;
        }


        public TokenWhileHandle SetOnCompleted<T>(Action<T> action, T state) where T : class
            => SetOnCompleted(state, action);

        public TokenWhileHandle SetOnCanceled<T>(Action<T> action, T state) where T : class
            => SetOnCanceled(state, action);


        public TokenWhileHandle SetOnCompleted<T>(T state, Action<T> action) where T : class
        {
            _runner?.SetOnCompleted(_handle, state, action);
            return this;
        }

        public TokenWhileHandle SetOnCanceled<T>(T state, Action<T> action) where T : class
        {
            _runner?.SetOnCanceled(_handle, state, action);
            return this;
        }
    }
}
