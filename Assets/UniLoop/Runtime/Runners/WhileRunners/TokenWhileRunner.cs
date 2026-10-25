using CatCode.PlayerLoops.Collections;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// PlayerLoop runner for predicate-based while-processes controlled by an external cancellation token.
    /// </summary>
    internal sealed class TokenWhileRunner : IWhileRegistry
    {
        private struct Entry
        {
            public Func<bool> Predicate;
            public CancellationToken CancellationToken;
            public CallbackData OnCompleted;
            public CallbackData OnCanceled;

            public Entry(Func<bool> predicate, CancellationToken cancellationToken)
            {
                Predicate = predicate;
                OnCompleted = CallbackData.Empty;
                OnCanceled = CallbackData.Empty;
                CancellationToken = cancellationToken;
            }
        }

        private readonly DeferredDenseArray<Entry> _denseArray;
        private readonly Queue<CallbackData> _finishedQueue = new();

        public TokenWhileRunner(int capacity = 32)
        {
            _denseArray = new DeferredDenseArray<Entry>(capacity);
        }

        public ElementHandle Schedule(Func<bool> predicate, CancellationToken token)
        {
            var entry = new Entry(predicate, token);
            return _denseArray.Add(entry);
        }


        public void SetOnCompleted(ElementHandle handle, Action onCompleted)
        {
            if (_denseArray.TryGetIndex(handle, out var index))
                _denseArray[index].OnCompleted = CallbackData.Create(onCompleted);
        }

        public void SetOnCompleted<T>(ElementHandle handle, T state, Action<T> onCompleted) where T : class
        {
            if (_denseArray.TryGetIndex(handle, out var index))
                _denseArray[index].OnCompleted = CallbackData.Create(state, onCompleted);
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

                bool isRunning;
                try
                {
                    isRunning = element.Predicate();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    continue;
                }

                if (!isRunning)
                {
                    // The predicate may grow the array, so the element is read again instead of using the old ref.
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


        public RunnerMetrics GetMetrics()
            => new(_denseArray.Count, _denseArray.PendingCount, _denseArray.TotalCount);

        public bool IsValid(ElementHandle handle)
            => _denseArray.IsValid(handle);
                
        public bool IsActive(ElementHandle handle)
            => _denseArray.TryGetIndex(handle, out var index)
               && !_denseArray[index].CancellationToken.IsCancellationRequested;
    }
}
