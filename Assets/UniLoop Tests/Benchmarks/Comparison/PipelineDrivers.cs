using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CatCode.PlayerLoops.Benchmarks
{
    /// <summary>
    /// Starts and stops single tasks for the pipeline scenario, where tasks start and end every frame.
    /// </summary>
    internal interface IPipelineDriver : IDisposable
    {
        /// <summary>Starts a task that runs until <see cref="Cancel"/> and returns its id.</summary>
        int StartCancelable();

        void Cancel(int id);

        /// <summary>Starts a task that runs the given number of frames and then ends by itself.</summary>
        void StartFinite(int frames);
    }

    /// <summary>
    /// Fixed pool of integer ids, so drivers can store their tasks in plain arrays.
    /// </summary>
    internal sealed class IdPool
    {
        private readonly int[] _freeIds;
        private int _freeCount;

        public IdPool(int capacity)
        {
            _freeIds = new int[capacity];
            for (int id = capacity - 1; id >= 0; id--)
            {
                _freeIds[_freeCount] = id;
                _freeCount++;
            }
        }

        public int Rent()
        {
            _freeCount--;
            return _freeIds[_freeCount];
        }

        public void Return(int id)
        {
            _freeIds[_freeCount] = id;
            _freeCount++;
        }
    }


    internal sealed class EmptyPipelineDriver : IPipelineDriver
    {
        public int StartCancelable() => 0;
        public void Cancel(int id) { }
        public void StartFinite(int frames) { }
        public void Dispose() { }
    }

    /// <summary>
    /// Cancelable tasks are Loop tasks stopped by their handle, finite tasks are counted loops.
    /// </summary>
    internal sealed class UniLoopPipelineDriver : IPipelineDriver
    {
        private readonly LoopHandle[] _handles;
        private readonly IdPool _ids;

        public UniLoopPipelineDriver(int capacity)
        {
            _handles = new LoopHandle[capacity];
            _ids = new IdPool(capacity);
        }

        public int StartCancelable()
        {
            var id = _ids.Rent();
            _handles[id] = UniLoop.Update.Loop.Schedule(BenchmarkWork.Tick);
            return id;
        }

        public void Cancel(int id)
        {
            _handles[id].Dispose();
            _ids.Return(id);
        }

        public void StartFinite(int frames)
            => UniLoop.Update.Loop.Schedule(BenchmarkWork.Tick, frames);

        public void Dispose()
        {
            for (int i = 0; i < _handles.Length; i++)
                _handles[i].Dispose();
        }
    }

    /// <summary>
    /// Finite tasks as While tasks with a frame counter captured in a closure,
    /// the way the While API is usually used. Only for the Complete scenario.
    /// </summary>
    internal sealed class UniLoopWhilePipelineDriver : IPipelineDriver
    {
        public int StartCancelable() => throw new NotSupportedException();

        public void Cancel(int id) => throw new NotSupportedException();

        public void StartFinite(int frames)
        {
            var remaining = frames;
            UniLoop.Update.While.Schedule(() =>
            {
                BenchmarkWork.Tick();
                remaining--;
                return remaining > 0;
            });
        }

        public void Dispose() { }
    }

    /// <summary>
    /// A hand-written update manager with O(1) removal: the removed task is replaced by the last one.
    /// </summary>
    internal sealed class UpdateManagerPipelineDriver : IPipelineDriver
    {
        private readonly GameObject _host;
        private readonly PipelineManagerBehaviour _manager;

        public UpdateManagerPipelineDriver(int capacity)
        {
            _host = new GameObject(nameof(UpdateManagerPipelineDriver));
            _manager = _host.AddComponent<PipelineManagerBehaviour>();
            _manager.Init(capacity);
        }

        public int StartCancelable() => _manager.Add(BenchmarkWork.Tick, PipelineManagerBehaviour.Infinite);

        public void Cancel(int id) => _manager.Remove(id);

        public void StartFinite(int frames) => _manager.Add(BenchmarkWork.Tick, frames);

        public void Dispose() => Object.Destroy(_host);
    }

    /// <summary>
    /// One coroutine per task. A finite task is a coroutine with a fixed number of iterations.
    /// </summary>
    internal sealed class CoroutinePipelineDriver : IPipelineDriver
    {
        private readonly GameObject _host;
        private readonly CoroutineHostBehaviour _runner;
        private readonly Coroutine[] _coroutines;
        private readonly IdPool _ids;

        public CoroutinePipelineDriver(int capacity)
        {
            _host = new GameObject(nameof(CoroutinePipelineDriver));
            _runner = _host.AddComponent<CoroutineHostBehaviour>();
            _coroutines = new Coroutine[capacity];
            _ids = new IdPool(capacity);
        }

        public int StartCancelable()
        {
            var id = _ids.Rent();
            _coroutines[id] = _runner.StartCoroutine(Endless());
            return id;
        }

        public void Cancel(int id)
        {
            _runner.StopCoroutine(_coroutines[id]);
            _coroutines[id] = null;
            _ids.Return(id);
        }

        public void StartFinite(int frames)
            => _runner.StartCoroutine(Finite(frames));

        public void Dispose() => Object.Destroy(_host);

        private static IEnumerator Endless()
        {
            while (true)
            {
                BenchmarkWork.Tick();
                yield return null;
            }
        }

        private static IEnumerator Finite(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                BenchmarkWork.Tick();
                yield return null;
            }
        }
    }

    /// <summary>
    /// One async loop per task. Every cancelable task has its own CancellationTokenSource,
    /// as it would in real code where tasks end for different reasons.
    /// </summary>
    internal sealed class UniTaskPipelineDriver : IPipelineDriver
    {
        private readonly CancellationTokenSource[] _sources;
        private readonly IdPool _ids;

        public UniTaskPipelineDriver(int capacity)
        {
            _sources = new CancellationTokenSource[capacity];
            _ids = new IdPool(capacity);
        }

        public int StartCancelable()
        {
            var id = _ids.Rent();
            var source = new CancellationTokenSource();
            _sources[id] = source;

            Endless(source.Token).Forget();
            return id;
        }

        public void Cancel(int id)
        {
            var source = _sources[id];
            source.Cancel();
            source.Dispose();

            _sources[id] = null;
            _ids.Return(id);
        }

        public void StartFinite(int frames)
            => Finite(frames).Forget();

        public void Dispose()
        {
            for (int i = 0; i < _sources.Length; i++)
            {
                var source = _sources[i];
                if (source == null)
                    continue;

                source.Cancel();
                source.Dispose();
                _sources[i] = null;
            }
        }

        // PlayerLoopTiming here is the UniTask one, not the UniLoop enum with the same name.
        private static async UniTaskVoid Endless(CancellationToken token)
        {
            while (true)
            {
                BenchmarkWork.Tick();

                var isCanceled = await UniTask
                    .Yield(Cysharp.Threading.Tasks.PlayerLoopTiming.Update, token)
                    .SuppressCancellationThrow();

                if (isCanceled)
                    return;
            }
        }

        private static async UniTaskVoid Finite(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                BenchmarkWork.Tick();
                await UniTask.Yield(Cysharp.Threading.Tasks.PlayerLoopTiming.Update);
            }
        }
    }
}
