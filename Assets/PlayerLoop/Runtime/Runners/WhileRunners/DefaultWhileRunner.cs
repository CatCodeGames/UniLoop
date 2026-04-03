using CatCode.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// PlayerLoop runner for predicate-based while-processes that end when the predicate returns false or are canceled.
    /// </summary>
    internal sealed class DefaultWhileRunner : IWhileController
    {
        private class Entry
        {
            public Func<bool> predicate;

            public Action onCompleted;
            public Action onCanceled;

            public IStatefulCallback onCompletedState;
            public IStatefulCallback onCanceledState;

            public bool isCancellationRequested;

            public void Release()
            {
                predicate = null;

                onCompleted = null;
                onCanceled = null;

                onCompletedState?.Release();
                onCompletedState = null;
                onCanceledState?.Release();
                onCanceledState = null;

                isCancellationRequested = false;
            }
        }

        private struct FinishedEntry
        {
            public Entry entry;
            public bool isCanceled;

            public FinishedEntry(Entry entry, bool isCanceled)
            {
                this.entry = entry;
                this.isCanceled = isCanceled;
            }
        }

        private readonly ObjectPool<Entry> _pool;
        private readonly DeferredDenseArrayFast<Entry> _denseArray;
        private readonly Queue<FinishedEntry> _finishedQueue;

        public DefaultWhileRunner(int startSize = 32, int growSize = 32)
        {
            _denseArray = new DeferredDenseArrayFast<Entry>(startSize, growSize);
            _pool = new ObjectPool<Entry>(
                createFunc: () => new(),
                actionOnRelease: item => item.Release(),
                collectionCheck: false,
                defaultCapacity: startSize);
            _finishedQueue = new Queue<FinishedEntry>(startSize);
        }

        public ElementHandle Schedule(Func<bool> predicate)
        {
            var entry = _pool.Get();
            entry.predicate = predicate;
            return _denseArray.Add(entry);
        }

        public void Cancel(ElementHandle handle)
            => _denseArray[handle].isCancellationRequested = true;


        public void SetOnCompleted(ElementHandle handle, Action onCompleted)
            => _denseArray[handle].onCompleted = onCompleted;
        public void SetOnCompleted<T>(ElementHandle handle, Action<T> onCompleted, T state)
            => _denseArray[handle].onCompletedState = StatefulCallback<T>.Get(onCompleted, state);
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
                    _finishedQueue.Enqueue(new(element, true));
                    _denseArray.RemoveAt(i);
                    continue;
                }
                if (!element.predicate())
                {
                    _finishedQueue.Enqueue(new(element, false));
                    _denseArray.RemoveAt(i);
                }
            }

            _denseArray.ApplyRemove();

            foreach (var entry in _finishedQueue)
            {
                var element = entry.entry;
                if (entry.isCanceled)
                {
                    element.onCanceled?.Invoke();
                    element.onCanceledState?.Invoke();
                }
                else
                {
                    element.onCompleted?.Invoke();
                    element.onCompletedState?.Invoke();
                }
                _pool.Release(element);
            }
            _finishedQueue.Clear();
        }


        public RunnerMetrics GetMetrics()
            => new(_denseArray.Count, _denseArray.PendingCount, _denseArray.TotalCount);
    }
}