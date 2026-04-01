using System;
using UnityEngine.Pool;

namespace CatCode.PlayerLoops
{

    /// <summary>
    /// Pooled callback that stores an action with its state.
    /// Used to invoke typed callbacks without allocations.
    /// </summary>
    public sealed class StatefulCallback<T> : IStatefulCallback
    {
        private readonly static ObjectPool<StatefulCallback<T>> s_pool;
        private bool _released;

        private Action<T> _callback;
        private T _state;

        static StatefulCallback()
        {
            s_pool = new ObjectPool<StatefulCallback<T>>(
                createFunc: () => new(),
                collectionCheck: false);
        }

        public static StatefulCallback<T> Get(Action<T> action, T state)
        {
            var callback = s_pool.Get();
            callback.Init(action, state);
            return callback;
        }

        private StatefulCallback() { }

        private void Init(Action<T> callback, T state)
        {
            _callback = callback;
            _state = state;
            _released = false;
        }

        public void Invoke()
        {
            _callback(_state);
        }

        public void Release()
        {
            if (_released)
                return;
            _released = true;

            _callback = null;
            _state = default;
            s_pool.Release(this);
        }

        public void InvokeAndRelease()
        {
            Invoke();
            Release();
        }
    }
}