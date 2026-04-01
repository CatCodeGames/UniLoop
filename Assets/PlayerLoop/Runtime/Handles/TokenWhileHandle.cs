using CatCode.Collections;
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
        private readonly uint _handleGeneration;
       
        public bool IsDisposed
        {
            get
            {
                if (_handle == null)
                    return true;
                return _handle.Generation != _handleGeneration;
            }
        }

        public TokenWhileHandle(IWhileRegistry runner, ElementHandle handle) : this()
        {
            _runner = runner;
            _handle = handle;
            _handleGeneration = handle.Generation;
        }


        public TokenWhileHandle SetOnCompleted(Action action)
        {
            if (!IsDisposed)
                _runner.SetOnCompleted(_handle, action);
            return this;
        }

        public TokenWhileHandle SetOnCompleted<T>(Action<T> action, T state)
        {
            if (!IsDisposed)
                _runner.SetOnCompleted(_handle, action, state);
            return this;
        }

        public TokenWhileHandle SetOnCanceled(Action action)
        {
            if (!IsDisposed)
                _runner.SetOnCanceled(_handle, action);
            return this;
        }

        public TokenWhileHandle SetOnCanceled<T>(Action<T> action, T state)
        {
            if (!IsDisposed)
                _runner.SetOnCanceled(_handle, action, state);
            return this;
        }
    }
}