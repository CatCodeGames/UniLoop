using System;

namespace CatCode.PlayerLoops
{
    public readonly struct CallbackData
    {
        public readonly object State;
        public readonly Delegate Callback;
        public readonly Action<CallbackData> Invoker;

        public void Invoke()
            => Invoker(this);

        public CallbackData(object state, Delegate callback, Action<CallbackData> invoker)
        {
            State = state;
            Callback = callback;
            Invoker = invoker;
        }

        public static CallbackData Create(Action callback)
        {
            return callback != null
                ? new(null, callback, _onlyAction)
                : new CallbackData(null, null, _emptyAction);
        }

        public static CallbackData Create<T>(T state, Action<T> callback)
            where T : class
        {
            return callback != null
                ? new(state, callback, CallbackDataInvoker<T>.Instance)
                : new(null, null, _emptyAction);
        }

        private readonly static Action<CallbackData> _onlyAction;
        private readonly static Action<CallbackData> _emptyAction;

        static CallbackData() {
            _onlyAction = InvokeAction;
            _emptyAction = EmptyAction;
        }

        public static CallbackData Empty
            => new(null, null, _emptyAction);

        private static void InvokeAction(CallbackData data)
            => ((Action)data.Callback).Invoke();
               
        private static void EmptyAction(CallbackData data)
        { }

        private static class CallbackDataInvoker<T>
            where T : class
        {
            public static readonly Action<CallbackData> Instance = data =>
                ((Action<T>)data.Callback).Invoke((T)data.State);
        }
    }
}