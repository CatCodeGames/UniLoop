#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEngine.Pool;

using UnityEditor;

namespace CatCode.Common
{
    public static partial class EditorPlayModeActionRegistry
    {
        private class EditorStrategy : IActionRegistryStrategy
        {
            private class RegisteredAction
            {
                public Action action;
                public PlayModeStateChange[] states;

                public RegisteredAction(Action action, PlayModeStateChange[] states)
                {
                    this.action = action;
                    this.states = states;
                }
            }

            private readonly List<RegisteredAction> _actions = new();
            
            public EditorStrategy()
            {
                EditorApplication.playModeStateChanged += OnStateChanged;
            }

            public void Register(Action action, PlayModeEvent[] events)
            {
                if (action == null || events == null || events.Length == 0)
                    return;
                var states = Convert(events);
                _actions.Add(new RegisteredAction(action, states));
            }

            private void OnStateChanged(PlayModeStateChange state)
            {
                using var handle = ListPool<Action>.Get(out var toRun);

                for (int i = 0; i < _actions.Count; i++)
                {
                    var reg = _actions[i];
                    foreach (var s in reg.states)
                    {
                        if (s == state)
                        {
                            toRun.Add(reg.action);
                            _actions[i] = null;
                            break;
                        }
                    }
                }

                _actions.RemoveAll(x => x == null);

                foreach (var action in toRun)
                    action();
            }

            private PlayModeStateChange[] Convert(PlayModeEvent[] events)
            {
                var count = events.Length;
                var states = new PlayModeStateChange[count];

                for (int i = 0; i < count; i++)
                    states[i] = Convert(events[i]);

                return states;
            }

            private PlayModeStateChange Convert(PlayModeEvent playModeEvent)
                => playModeEvent switch
                {
                    PlayModeEvent.EnteredEditMode => PlayModeStateChange.EnteredEditMode,
                    PlayModeEvent.ExitingEditMode => PlayModeStateChange.ExitingEditMode,
                    PlayModeEvent.EnteredPlayMode => PlayModeStateChange.EnteredPlayMode,
                    PlayModeEvent.ExitingPlayMode => PlayModeStateChange.ExitingPlayMode,
                    _ => PlayModeStateChange.ExitingPlayMode,
                };
        }
    }
}
#endif