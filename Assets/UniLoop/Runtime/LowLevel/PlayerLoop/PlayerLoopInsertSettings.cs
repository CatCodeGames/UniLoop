using System;


namespace CatCode.PlayerLoops.LowLevel
{
    /// <summary>
    /// Insert settings for placing a system inside the PlayerLoop.
    /// </summary>
    internal readonly struct PlayerLoopInsertSettings
    {
        public readonly Type parentType;        // Parent PlayerLoopSystem
        public readonly Type insertType;        // PlayerLoopSystem being inserted
        public readonly Type targetType;        // PlayerLoopSystem to insert before/after, if specified

        public readonly InsertPosition position;

        public PlayerLoopInsertSettings(Type parentType, Type insertType, Type targetType, InsertPosition position)
        {
            this.parentType = parentType;
            this.insertType = insertType;
            this.targetType = targetType;
            this.position = position;
        }
    }
}