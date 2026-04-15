using CatCode.PlayerLoops.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Default runner for PlayerLoop actions with cancellation support.
    /// </summary>
    internal sealed class DefaultLoopRunner : ILoopController
    {
        private class Entry
        {
            public Action action;

            public Action onCanceled;
            public IStatefulCallback onCanceledState;

            public bool isCancellationRequested;

            public void Release()
            {
                action = null;

                onCanceled = null;
                onCanceledState?.Release();
                onCanceledState = null;

                isCancellationRequested = false;
            }
        }

        private readonly DeferredDenseArray<Entry> _denseArray;
        private readonly Queue<Entry> _finishedQueue;
        private readonly ObjectPool<Entry> _pool;

        public DefaultLoopRunner(int capacity = 32)
        {
            _denseArray = new DeferredDenseArray<Entry>(capacity);
            _pool = new ObjectPool<Entry>(
                createFunc: () => new(),
                actionOnRelease: item => item.Release(),
                collectionCheck: false,
                defaultCapacity: capacity);
            _finishedQueue = new Queue<Entry>(capacity);
        }

        public ElementHandle Schedule(Action action)
        {
            var entry = _pool.Get();
            entry.action = action;
            return _denseArray.Add(entry);
        }

        public void Cancel(ElementHandle handle)
            => _denseArray[handle].isCancellationRequested = true;


        public void SetOnCanceled(ElementHandle handle, Action onCanceled)
            => _denseArray[handle].onCanceled = onCanceled;

        public void SetOnCanceled<T>(ElementHandle handle, Action<T> onCanceled, T state)
            => _denseArray[handle].onCanceledState = StatefulCallback<T>.Get(onCanceled, state);


        public void Run()
        {
            _denseArray.ApplyAdd();

            var count = _denseArray.Count;
            for (int i = 0; i < count; i++)
            {
                var element = _denseArray[i];
                if (element.isCancellationRequested)
                {
                    _finishedQueue.Enqueue(element);
                    _denseArray.RemoveAt(i);
                }
                else
                    element.action();
            }

            _denseArray.ApplyRemove();

            foreach (var entry in _finishedQueue)
            {
                entry.onCanceled?.Invoke();
                entry.onCanceledState?.Invoke();

                _pool.Release(entry);
            }
            _finishedQueue.Clear();
        }

        public bool IsValid(ElementHandle handle)
            => _denseArray.IsValid(handle);

        public RunnerMetrics GetMetrics()
            => new(_denseArray.Count, _denseArray.PendingCount, _denseArray.TotalCount);
    }
}