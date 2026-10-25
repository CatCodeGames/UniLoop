using CatCode.PlayerLoops.Collections;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// PlayerLoop runner for actions controlled by an external cancellation token.
    /// </summary>
    internal sealed class TokenLoopRunner : ILoopRegistry
    {
        private struct Entry
        {
            public Action Action;
            public CancellationToken CancellationToken;
            public CallbackData OnCanceled;

            public Entry(Action action, CancellationToken cancellationToken)
            {
                Action = action;
                OnCanceled = CallbackData.Empty;
                CancellationToken = cancellationToken;
            }
        }

        private readonly DeferredDenseArray<Entry> _denseArray;
        private readonly Queue<CallbackData> _finishedQueue = new();

        public TokenLoopRunner(int capacity = 32)
        {
            _denseArray = new DeferredDenseArray<Entry>(capacity);
        }

        public ElementHandle Schedule(Action action, CancellationToken token)
        {
            var entry = new Entry(action, token);
            return _denseArray.Add(entry);
        }


        public void SetOnCanceled(ElementHandle handle, Action onCanceled)
        {
            if (_denseArray.TryGetIndex(handle, out var index))
                _denseArray[index].OnCanceled = CallbackData.Create(onCanceled);
        }

        public void SetOnCanceled<T>(ElementHandle handle, T state, Action<T> onCanceled) where T : class
        {
            if (_denseArray.TryGetIndex(handle, out var index))
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
                if (element.CancellationToken.IsCancellationRequested)
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

        public bool IsValid(ElementHandle handle) => _denseArray.IsValid(handle);

        // Active until the token is canceled, without waiting for the next Run to remove the task.
        public bool IsActive(ElementHandle handle)
            => _denseArray.TryGetIndex(handle, out var index)
               && !_denseArray[index].CancellationToken.IsCancellationRequested;

        public RunnerMetrics GetMetrics()
            => new(_denseArray.Count, _denseArray.PendingCount, _denseArray.TotalCount);
    }
}
