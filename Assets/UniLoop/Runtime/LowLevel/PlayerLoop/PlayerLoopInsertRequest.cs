using System;
using UnityEngine.LowLevel;

namespace CatCode.PlayerLoops.LowLevel
{
    /// <summary>
    /// Describes a request to insert a system into the PlayerLoop,
    /// including target location and insertion details.
    /// </summary>
    public class PlayerLoopInsertRequest
    {
        public Type ParentType;
        public Type TargetType;
        public PlayerLoopSystem System;
        public InsertPosition Position;

        public Action OnRemove;
        public bool AutoCleanupInEditor;
    }
}