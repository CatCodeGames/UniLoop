using System.Collections;
using UnityEngine;

namespace CatCode.PlayerLoops.Benchmarks
{
    /// <summary>
    /// One MonoBehaviour that calls every task from its Update. The common hand-written "update manager".
    /// Stop clears the whole list at once, which is the best case for this approach.
    /// </summary>
    internal sealed class UpdateManagerDriver : ITaskDriver
    {
        private readonly GameObject _host;
        private readonly UpdateManagerBehaviour _manager;
        private readonly int _count;

        public UpdateManagerDriver(int count)
        {
            _count = count;
            _host = new GameObject(nameof(UpdateManagerDriver));
            _manager = _host.AddComponent<UpdateManagerBehaviour>();
            _manager.Actions.Capacity = count;
        }

        public void Start()
        {
            for (int i = 0; i < _count; i++)
                _manager.Actions.Add(BenchmarkWork.Tick);
        }

        public void Stop() => _manager.Actions.Clear();

        public void Dispose() => Object.Destroy(_host);
    }

    /// <summary>
    /// One coroutine per task: an endless loop with yield return null.
    /// </summary>
    internal sealed class CoroutineDriver : ITaskDriver
    {
        private readonly GameObject _host;
        private readonly CoroutineHostBehaviour _runner;
        private readonly Coroutine[] _coroutines;

        public CoroutineDriver(int count)
        {
            _host = new GameObject(nameof(CoroutineDriver));
            _runner = _host.AddComponent<CoroutineHostBehaviour>();
            _coroutines = new Coroutine[count];
        }

        public void Start()
        {
            for (int i = 0; i < _coroutines.Length; i++)
                _coroutines[i] = _runner.StartCoroutine(Loop());
        }

        public void Stop()
        {
            for (int i = 0; i < _coroutines.Length; i++)
                _runner.StopCoroutine(_coroutines[i]);
        }

        public void Dispose() => Object.Destroy(_host);

        private static IEnumerator Loop()
        {
            while (true)
            {
                BenchmarkWork.Tick();
                yield return null;
            }
        }
    }
}
