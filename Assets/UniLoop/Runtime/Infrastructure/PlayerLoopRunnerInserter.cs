using CatCode.PlayerLoops.LowLevel;
using System;
using UnityEngine.LowLevel;

namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Inserts and removes PlayerLoop runner systems according to predefined bindings.
    /// </summary>
    internal class PlayerLoopRunnerInserter : IDisposable
    {
        private readonly PlayerLoopRunnerRegistry _registry;
        private readonly RunnerBinding[] _bindings;

        private PlayerLoopInsertRequest[] _requests;

        private bool _inserted;
        private bool _disposed;

        internal PlayerLoopRunnerInserter(PlayerLoopRunnerRegistry registry, RunnerBinding[] bindings)
        {
            _registry = registry;
            _bindings = bindings;
        }

        internal void InserRunners()
        {
            if (_inserted || _disposed)
                return;
            _inserted = true;
            _requests = CreateRequests();
            PlayerLoopHelper.InsertLoopSystems(_requests);
        }

        public void Dispose()
        {
            if (!_inserted || _disposed)
                return;
            _disposed = true;
            PlayerLoopHelper.RemoveLoopSystems(_requests);
        }


        private PlayerLoopInsertRequest[] CreateRequests()
        {
            var requests = new PlayerLoopInsertRequest[_bindings.Length];

            for (int i = 0; i < _bindings.Length; i++)
            {
                var binding = _bindings[i];
                var playerLoopRunner = _registry[binding.Position];
                requests[i] = CreateInsertRequest(playerLoopRunner.Run, binding.Insert);
            }

            return requests;
        }

        private static PlayerLoopInsertRequest CreateInsertRequest(
            PlayerLoopSystem.UpdateFunction updateDelegate,
            PlayerLoopInsertSettings insertSettings)
        {
            var playerLoopSystem = new PlayerLoopSystem()
            {
                type = insertSettings.insertType,
                updateDelegate = updateDelegate
            };

            var request = new PlayerLoopInsertRequest()
            {
                System = playerLoopSystem,

                ParentType = insertSettings.parentType,
                TargetType = insertSettings.targetType,
                Position = insertSettings.position,

                AutoCleanupInEditor = true,
                OnRemove = null,
            };

            return request;
        }
    }
}