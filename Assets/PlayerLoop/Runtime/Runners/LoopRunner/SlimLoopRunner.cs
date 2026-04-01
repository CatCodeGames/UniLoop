using CatCode.Collections;
using System;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Minimal runner for PlayerLoop actions; not safe to cancel tasks during execution.
    /// </summary>
    internal sealed class SlimLoopRunner : ILoopCanceller
    {
        private readonly DeferredDenseArray<Action> _denseArray;

        public SlimLoopRunner(int startSize, int growSize)
        {
            _denseArray = new DeferredDenseArray<Action>(startSize, growSize);
        }

        public ElementHandle Add(Action action)
            => _denseArray.Add(action);

        public void Cancel(ElementHandle handle)
            => _denseArray.Remove(handle);


        public void Run()
        {
            _denseArray.ApplyAdd();

            for (int i = 0; i < _denseArray.Count; i++)
                _denseArray[i]();

            _denseArray.ApplyRemove();
        }


        public RunnerMetrics GetMetrics()
            => new(_denseArray.Count, _denseArray.PendingCount, _denseArray.TotalCount);
    }
}