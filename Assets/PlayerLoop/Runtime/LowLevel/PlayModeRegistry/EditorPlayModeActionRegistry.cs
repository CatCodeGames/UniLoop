using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CatCode.Common
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    /// <summary>
    /// Executes registered actions on Unity Editor Play Mode state changes,
    /// e.g. cleaning up PlayerLoop systems when exiting Play Mode.
    /// </summary>

    public static partial class EditorPlayModeActionRegistry
    {
        private static readonly IActionRegistryStrategy _strategy;

        static EditorPlayModeActionRegistry
        ()
        {
            _strategy =
#if UNITY_EDITOR
            new EditorStrategy();
#else
            new DummyStrategy();
#endif
        }

        public static void Register(Action action, params PlayModeEvent[] events)
            => _strategy.Register(action, events);
    }
}