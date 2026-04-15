using CatCode.PlayerLoops.Collections;
using System;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Handle for a running PlayerLoop action with simple cancellation.
    /// </summary>
    public readonly struct SlimLoopHandle : IDisposable
    {
        private readonly ISlimLoopContoller _runner;
        private readonly ElementHandle _handle;

        public bool IsDisposed => _runner == null || !_runner.IsValid(_handle);

        public SlimLoopHandle(ISlimLoopContoller runner, ElementHandle handle)
        {
            _runner = runner;
            _handle = handle;
        }

        public void Dispose()
        {
            if(!IsDisposed)
                _runner.Cancel(_handle);
        }
    }
}