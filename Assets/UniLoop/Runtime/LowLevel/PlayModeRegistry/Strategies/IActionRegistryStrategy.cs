using System;

namespace CatCode.Common
{
    public static partial class EditorPlayModeActionRegistry
    {
        private interface IActionRegistryStrategy
        {
            void Register(Action action, PlayModeEvent[] events);
        }
    }
}