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
        private struct Entry
        {
            public Action action;

            public CallbackData onCanceled;


            public bool isCancellationRequested;

            public Entry(Action action)
            {
                this.action = action;
                onCanceled = CallbackData.Empty;
                isCancellationRequested = false;
            }
        }

        private readonly DeferredDenseArray<Entry> _denseArray;

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
            => _denseArray[handle].isCancellationRequested = true;


        public void SetOnCanceled(ElementHandle handle, Action onCanceled)
            => _denseArray[handle].onCanceled = CallbackData.Create(onCanceled);

        public void SetOnCanceled<T>(ElementHandle handle, T state, Action<T> onCanceled) where T : class
            => _denseArray[handle].onCanceled = CallbackData.Create(state, onCanceled);


        public void Run()
        {
            var handle = UnsafeGenericPool<Queue<CallbackData>>.Get(out var finishedQueue);
            _denseArray.ApplyAdd();

            var count = _denseArray.Count;
            for (int i = 0; i < count; i++)
            {
                var element = _denseArray[i];
                if (element.isCancellationRequested)
                {
                    finishedQueue.Enqueue(element.onCanceled);
                    _denseArray.RemoveAt(i);
                }
                else
                    element.action();
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