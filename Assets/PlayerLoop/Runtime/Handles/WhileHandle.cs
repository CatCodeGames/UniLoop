using CatCode.Collections;
using System;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Handle for a running PlayerLoop while-process that can complete or be canceled.
    /// </summary>
    public readonly struct WhileHandle : IDisposable
    {
        private readonly IWhileController _runner;
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

        public WhileHandle(IWhileController runner, ElementHandle handle)
        {
            _runner = runner;
            _handle = handle;
            _handleGeneration = handle.Generation;
        }


        public WhileHandle SetOnCompleted(Action action)
        {
            if (!IsDisposed)
                _runner.SetOnCompleted(_handle, action);
            return this;
        }

        public WhileHandle SetOnCompleted<T>(Action<T> action, T state)
        {
            if (!IsDisposed)
                _runner.SetOnCompleted(_handle, action, state);
            return this;
        }

        public WhileHandle SetOnCanceled(Action action)
        {
            if (!IsDisposed)
                _runner.SetOnCanceled(_handle, action);
            return this;
        }

        public WhileHandle SetOnCanceled<T>(Action<T> action, T state)
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