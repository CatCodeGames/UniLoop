using System;

namespace CatCode.Common
{
    public static partial class EditorPlayModeActionRegistry
    {
        private class DummyStrategy : IActionRegistryStrategy
        {
            public void Register(Action action, PlayModeEvent[] events) { }
        }
    }
}