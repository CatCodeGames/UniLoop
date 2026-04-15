using CatCode.PlayerLoops.Collections;
using System;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Minimal runner for PlayerLoop actions; not safe to cancel tasks during execution.
    /// </summary>
    internal sealed class SlimLoopRunner : ISlimLoopContoller
    {
        private readonly DeferredDenseArray<Action> _denseArray;

        public SlimLoopRunner(int capacity)
        {
            _denseArray = new DeferredDenseArray<Action>(capacity);
        }

        public ElementHandle Add(Action action)
            => _denseArray.Add(action);

        public void Cancel(ElementHandle handle)
            => _denseArray.Remove(handle);


        public void Run()
        {
            _denseArray.ApplyAdd();
            _denseArray.ApplyRemove();

            var count = _denseArray.Count;
            for (int i = 0; i < count; i++)
                _denseArray[i]();
        }

        public bool IsValid(ElementHandle handle)
            => _denseArray.IsValid(handle);

        public RunnerMetrics GetMetrics()
            => new(_denseArray.Count, _denseArray.PendingCount, _denseArray.TotalCount);
    }
}