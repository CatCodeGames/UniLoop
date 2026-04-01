using CatCode.Collections;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Pool;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// PlayerLoop runner for actions controlled by an external cancellation token.
    /// </summary>
    internal sealed class TokenLoopRunner : ILoopRegistry
    {
        private class Entry
        {
            public Action action;
            public CancellationToken cancellationToken;

            public Action onCanceled;
            public IStatefulCallback onCanceledState;


            public void Init(Action action, CancellationToken cancellationToken)
            {
                this.action = action;
                this.cancellationToken = cancellationToken;
            }

            public void Release()
            {
                action = null;
                cancellationToken = default;

                onCanceled = null;
                onCanceledState?.Release();
                onCanceledState = null;
            }
        }

        private readonly DeferredDenseArray<Entry> _denseArray;
        private readonly Queue<Entry> _finishedQueue;
        private readonly ObjectPool<Entry> _pool;

        public TokenLoopRunner(int startSize = 32, int growSize = 32)
        {
            _denseArray = new DeferredDenseArray<Entry>(startSize, growSize);
            _pool = new ObjectPool<Entry>(
                createFunc: () => new(),
                actionOnRelease: item => item.Release(),
                collectionCheck: false,
                defaultCapacity: startSize);
            _finishedQueue = new Queue<Entry>(startSize);
        }

        public ElementHandle Schedule(Action action, CancellationToken token)
        {
            var entry = _pool.Get();
            entry.Init(action, token);
            return _denseArray.Add(entry);
        }


        public void SetOnCanceled(ElementHandle handle, Action onCanceled)
            => _denseArray[handle].onCanceled = onCanceled;

        public void SetOnCanceled<T>(ElementHandle handle, Action<T> onCanceled, T state)
            => _denseArray[handle].onCanceledState = StatefulCallback<T>.Get(onCanceled, state);


        public void Run()
        {
            _denseArray.ApplyAdd();

            for (int i = 0; i < _denseArray.Count; i++)
            {
                var element = _denseArray[i];
                if (element.cancellationToken.IsCancellationRequested)
                {
                    _finishedQueue.Enqueue(element);
                    _denseArray.RemoveAt(i);
                }
                else
                    element.action();
            }

            _denseArray.ApplyRemove();

            foreach (var element in _finishedQueue)
            {
                element.onCanceled?.Invoke();
                element.onCanceledState?.Invoke();

                _pool.Release(element);
            }
            _finishedQueue.Clear();
        }

        public RunnerMetrics GetMetrics()
            => new(_denseArray.Count, _denseArray.PendingCount, _denseArray.TotalCount);
    }
}