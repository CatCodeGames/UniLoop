using CatCode.PlayerLoops;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

namespace CatCode.PlayerLoops.Tests
{
    public sealed class TestWorker
    {
        private int _elapsedTime;
        private int _duration;
        private readonly Func<bool> _cachedPredicate;

        public Func<bool> CachedPredicate => _cachedPredicate;

        public TestWorker()
        {
            _cachedPredicate = Predicate;
        }

        public void Init(int duration)
        {
            _duration = duration;
        }

        public void Reset()
        {
            _elapsedTime = 0;
        }

        public bool InversePredicate() => !Predicate();

        public bool Predicate()
        {
            if (_elapsedTime > _duration)
                return false;
            _elapsedTime++;
            return true;
        }
    }

    public readonly struct PoolContext
    {
        public readonly TestWorker worker;
        public readonly ObjectPool<TestWorker> pool;

        public PoolContext(TestWorker worker, ObjectPool<TestWorker> pool)
        {
            this.worker = worker;
            this.pool = pool;
        }
    }


    public sealed class AllocTester : MonoBehaviour
    {
        private ObjectPool<TestWorker> _pool;
        private CancellationTokenSource _cts;

        private const int Count = 100;
        private const int Duration = 100;


        private Action<PoolContext> _cachedOnFinished;

        private void Awake()
        {
            _cts = new CancellationTokenSource();
            _cachedOnFinished = OnFinished;
            _pool = new ObjectPool<TestWorker>(createFunc: () => new(),
                actionOnRelease: (instance) => instance.Reset(),
                collectionCheck: false,
                maxSize: 100000);
        }

        private void Update()
        {
            StartUniLoop();
        }

        private void OnFinished(PoolContext context)
            => context.pool.Release(context.worker);

        private void StartUniLoop()
        {
            for (int i = 0; i < Count; i++)
            {
                var worker = _pool.Get();
                worker.Init(Duration);

                UniLoop.Update.While.Schedule(worker.CachedPredicate, _cts.Token)
                    .SetOnCanceled(_cachedOnFinished, new(worker, _pool))
                    .SetOnCompleted(_cachedOnFinished, new(worker, _pool));
            }
        }
    }
}