using CatCode.PlayerLoops.Collections;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Pool;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// PlayerLoop runner for predicate-based while-processes controlled by an external cancellation token.
    /// </summary>
    internal sealed class TokenWhileRunner : IWhileRegistry
    {
        private struct Entry
        {
            public Func<bool> predicate;
            public CancellationToken cancellationToken;

            public CallbackData onCompleted;
            public CallbackData onCanceled;

            public Entry(Func<bool> predicate, CancellationToken cancellationToken)
            {
                this.predicate = predicate;

                onCompleted = CallbackData.Empty;
                onCanceled = CallbackData.Empty;

                this.cancellationToken = cancellationToken;
            }
        }

        private readonly DeferredDenseArray<Entry> _denseArray;

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
            => _denseArray[handle].onCompleted = CallbackData.Create(onCompleted);        
        public void SetOnCompleted<T>(ElementHandle handle, T state, Action<T> onCompleted) where T : class
            => _denseArray[handle].onCompleted = CallbackData.Create(state, onCompleted);

        public void SetOnCanceled(ElementHandle handle, Action onCanceled)
            => _denseArray[handle].onCanceled = CallbackData.Create(onCanceled);        
        public void SetOnCanceled<T>(ElementHandle handle, T state, Action<T> onCanceled) where T : class
            => _denseArray[handle].onCanceled = CallbackData.Create(state, onCanceled);


        public void Run()
        {
            var queueHandle = UnsafeGenericPool<Queue<CallbackData>>.Get(out var finishedQueue);
            _denseArray.ApplyAdd();

            var count = _denseArray.Count;
            for (int i = 0; i < count; i++)
            {
                ref var element = ref _denseArray[i];
                if (element.cancellationToken.IsCancellationRequested)
                {
                    finishedQueue.Enqueue(element.onCanceled);
                    _denseArray.RemoveAt(i);
                    continue;
                }
                if (!element.predicate())
                {
                    finishedQueue.Enqueue(element.onCompleted);
                    _denseArray.RemoveAt(i);
                }
            }

            _denseArray.ApplyRemove();

            foreach (var data in finishedQueue)
                data.Invoke();

            finishedQueue.Clear();
        }


        public RunnerMetrics GetMetrics()
            => new(_denseArray.Count, _denseArray.PendingCount, _denseArray.TotalCount);

        public bool IsValid(ElementHandle handle)
            => _denseArray.IsValid(handle);
    }
}