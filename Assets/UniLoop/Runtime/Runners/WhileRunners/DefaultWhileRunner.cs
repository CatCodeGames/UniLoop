using CatCode.PlayerLoops.Collections;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Pool;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// PlayerLoop runner for predicate-based while-processes that end when the predicate returns false or are canceled.
    /// </summary>
    internal sealed class DefaultWhileRunner : IWhileController
    {
        private struct Entry
        {
            public Func<bool> predicate;

            public CallbackData onCompleted;
            public CallbackData onCanceled;

            public bool isCancellationRequested;

            public Entry(Func<bool> predicate)
            {
                this.predicate = predicate;

                onCompleted = CallbackData.Empty;
                onCanceled = CallbackData.Empty;

                isCancellationRequested = false;
            }
        }

        private readonly DeferredDenseArray<Entry> _denseArray;

        public DefaultWhileRunner(int capacity = 32)
        {
            _denseArray = new DeferredDenseArray<Entry>(capacity);
        }

        public ElementHandle Schedule(Func<bool> predicate)
        {
            var entry = new Entry(predicate);
            return _denseArray.Add(entry);
        }

        public void Cancel(ElementHandle handle)
            => _denseArray[handle].isCancellationRequested = true;


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
                var element = _denseArray[i];
                if (element.isCancellationRequested)
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

        public bool IsValid(ElementHandle handle)
            => _denseArray.IsValid(handle);

        public RunnerMetrics GetMetrics()
            => new(_denseArray.Count, _denseArray.PendingCount, _denseArray.TotalCount);
    }
}