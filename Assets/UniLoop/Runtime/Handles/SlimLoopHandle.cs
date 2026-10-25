using CatCode.PlayerLoops.Collections;
using System;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Handle for a running PlayerLoop action with simple cancellation.
    /// </summary>
    public readonly struct SlimLoopHandle : IDisposable
    {
        private readonly ISlimLoopController _runner;
        private readonly ElementHandle _handle;

        public bool IsDisposed => _runner == null || !_runner.IsActive(_handle);

        public SlimLoopHandle(ISlimLoopController runner, ElementHandle handle)
        {
            _runner = runner;
            _handle = handle;
        }
                
        public void Dispose()
            => _runner?.Cancel(_handle);
    }
}
