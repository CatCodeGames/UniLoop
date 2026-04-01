using CatCode.Collections;
using System;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Handle for a running PlayerLoop action with simple cancellation.
    /// </summary>
    public readonly struct SlimLoopHandle : IDisposable
    {
        private readonly ILoopCanceller _runner;
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

        public SlimLoopHandle(ILoopCanceller runner, ElementHandle handle)
        {
            _runner = runner;
            _handle = handle;
            _handleGeneration = handle.Generation;
        }

        public void Dispose()
        {
            if(!IsDisposed)
                _runner.Cancel(_handle);
        }
    }
}