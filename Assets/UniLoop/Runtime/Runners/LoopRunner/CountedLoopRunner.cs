using CatCode.PlayerLoops.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// PlayerLoop runner for actions that run a fixed number of times and then complete.
    /// </summary>
    internal sealed class CountedLoopRunner : IWhileController
    {
        private struct Entry
        {
            public Action Action;
            public int Remaining;

            public CallbackData OnCompleted;
            public CallbackData OnCanceled;

            public bool IsCancellationRequested;

            public Entry(Action action, int count)
            {
                Action = action;
                Remaining = count;

                OnCompleted = CallbackData.Empty;
                OnCanceled = CallbackData.Empty;

                IsCancellationRequested = false;
            }
        }

        private readonly DeferredDenseArray<Entry> _denseArray;
        private readonly Queue<CallbackData> _finishedQueue = new();

        public CountedLoopRunner(int capacity = 32)
        {
            _denseArray = new DeferredDenseArray<Entry>(capacity);
        }

        public ElementHandle Schedule(Action action, int count)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive.");

            var entry = new Entry(action, count);
            return _denseArray.Add(entry);
        }
                
        public void Cancel(ElementHandle handle)
        {
            if (_denseArray.TryGetIndex(handle, out var index))
                _denseArray[index].IsCancellationRequested = true;
        }


        public void SetOnCompleted(ElementHandle handle, Action onCompleted)
        {
            if (TryGetActiveIndex(handle, out var index))
                _denseArray[index].OnCompleted = CallbackData.Create(onCompleted);
        }

        public void SetOnCompleted<T>(ElementHandle handle, T state, Action<T> onCompleted) where T : class
        {
            if (TryGetActiveIndex(handle, out var index))
                _denseArray[index].OnCompleted = CallbackData.Create(state, onCompleted);
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
                ref var element = ref _denseArray[i];
                if (element.IsCancellationRequested)
                {
                    if (!element.OnCanceled.IsEmpty)
                        _finishedQueue.Enqueue(element.OnCanceled);

                    _denseArray.RemoveAt(i);
                    continue;
                }
                                
                element.Remaining--;
                var isLastRun = element.Remaining == 0;

                try
                {
                    element.Action();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                if (isLastRun)
                {                                        
                    ref readonly var onCompleted = ref _denseArray[i].OnCompleted;
                    if (!onCompleted.IsEmpty)
                        _finishedQueue.Enqueue(onCompleted);

                    _denseArray.RemoveAt(i);
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
