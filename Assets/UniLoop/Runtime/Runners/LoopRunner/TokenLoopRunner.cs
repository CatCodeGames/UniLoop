using CatCode.PlayerLoops.Collections;
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
        private struct Entry
        {
            public Action action;
            public CancellationToken cancellationToken;
            public CallbackData onCanceled;

            public Entry(Action action, CancellationToken cancellationToken)
            {
                this.action = action;

                onCanceled = CallbackData.Empty;

                this.cancellationToken = cancellationToken;
            }
        }

        private readonly DeferredDenseArray<Entry> _denseArray;        

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
                if (element.cancellationToken.IsCancellationRequested)
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

        public bool IsValid(ElementHandle handle) => _denseArray.IsValid(handle);

        public RunnerMetrics GetMetrics()
            => new(_denseArray.Count, _denseArray.PendingCount, _denseArray.TotalCount);
    }
}