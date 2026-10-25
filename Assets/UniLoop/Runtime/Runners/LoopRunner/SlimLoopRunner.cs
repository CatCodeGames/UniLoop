using CatCode.PlayerLoops.Collections;
using System;
using UnityEngine;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Minimal runner for PlayerLoop actions; not safe to cancel tasks during execution.
    /// </summary>
    internal sealed class SlimLoopRunner : ISlimLoopController
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
            if (_denseArray.TotalCount == 0)
                return;

            _denseArray.ApplyChanges();

            var count = _denseArray.Count;
            for (int i = 0; i < count; i++)
            {
                try
                {
                    _denseArray[i]();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public bool IsValid(ElementHandle handle)
            => _denseArray.IsValid(handle);

        public bool IsActive(ElementHandle handle)
            => _denseArray.TryGetIndex(handle, out var index) && !_denseArray.IsPendingRemoveAt(index);

        public RunnerMetrics GetMetrics()
            => new(_denseArray.Count, _denseArray.PendingCount, _denseArray.TotalCount);
    }
}
