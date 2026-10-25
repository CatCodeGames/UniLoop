using CatCode.PlayerLoops.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Default runner for PlayerLoop actions with cancellation support.
    /// </summary>
    internal sealed class DefaultLoopRunner : ILoopController
    {
        private struct Entry
        {
            public Action Action;
            public CallbackData OnCanceled;
            public bool IsCancellationRequested;

            public Entry(Action action)
            {
                Action = action;
                OnCanceled = CallbackData.Empty;
                IsCancellationRequested = false;
            }
        }

        private readonly DeferredDenseArray<Entry> _denseArray;
        private readonly Queue<CallbackData> _finishedQueue = new();

        public DefaultLoopRunner(int capacity = 32)
        {
            _denseArray = new DeferredDenseArray<Entry>(capacity);
        }

        public ElementHandle Schedule(Action action)
        {
            var entry = new Entry(action);
            return _denseArray.Add(entry);
        }

        public void Cancel(ElementHandle handle)
        {
            if (_denseArray.TryGetIndex(handle, out var index))
                _denseArray[index].IsCancellationRequested = true;
        }


        public void SetOnCanceled(ElementHandle handle, Action onCanceled)
        {
            if (TryGetActiveIndex(handle, out var index))
                _denseArray[index].OnCanceled = CallbackData.Create(onCanceled);
        }

        public void SetOnCanceled<T>(ElementHandle handle, T state, Action<T> onCanceled) where T : class
        {
            if (TryGetActiveIndex(handle, out var index))
                _denseArray[index].OnCanceled = CallbackData.Create(state, onCanceled);
        }


        public void Run()
        {
            if (_denseArray.TotalCount == 0)
                return;

            _denseArray.ApplyAdd();

            var count = _denseArray.Count;
            for (int i = 0; i < count; i++)
            {
                ref readonly var element = ref _denseArray[i];
                if (element.IsCancellationRequested)
                {
                    if (!element.OnCanceled.IsEmpty)
                        _finishedQueue.Enqueue(element.OnCanceled);

                    _denseArray.RemoveAt(i);
                    continue;
                }

                try
                {
                    element.Action();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            _denseArray.ApplyRemove();

            while (_finishedQueue.Count > 0)
            {
                try
                {
                    _finishedQueue.Dequeue().Invoke();
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
            => TryGetActiveIndex(handle, out _);

        public RunnerMetrics GetMetrics()
            => new(_denseArray.Count, _denseArray.PendingCount, _denseArray.TotalCount);

        private bool TryGetActiveIndex(ElementHandle handle, out int index)
            => _denseArray.TryGetIndex(handle, out index) && !_denseArray[index].IsCancellationRequested;
    }
}
