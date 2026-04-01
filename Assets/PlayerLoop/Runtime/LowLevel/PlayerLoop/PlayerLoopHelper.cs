using System;
using UnityEngine.LowLevel;

namespace CatCode.PlayerLoops.LowLevel
{
    /// <summary>
    /// Utility for inserting and removing systems in the PlayerLoop.
    /// </summary>
    public static class PlayerLoopHelper
    {
        /// <summary>
        /// Inserts multiple systems into the PlayerLoop.
        /// </summary>
        public static void InsertLoopSystems(PlayerLoopInsertRequest[] requests)
        {
            var playerLoop = GetCurrentPlayerLoop();
            for (int i = 0; i < requests.Length; i++)
                InsertLoopSystem(ref playerLoop, requests[i]);
            PlayerLoop.SetPlayerLoop(playerLoop);
        }
        
        /// <summary>
        /// Removes systems previously inserted into the PlayerLoop.
        /// </summary>
        public static void RemoveLoopSystems(PlayerLoopInsertRequest[] requests)
        {
            var playerLoop = GetCurrentPlayerLoop();
            for (int i = 0; i < requests.Length; i++)
            {
                var request = requests[i];
                request.OnRemove?.Invoke();
                if (request.AutoCleanupInEditor)
                    RemoveChildSystem(ref playerLoop, request.ParentType, request.System.type);
            }

            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        public static PlayerLoopSystem GetCurrentPlayerLoop()
        {
#if UNITY_2019_3_OR_NEWER
            return PlayerLoop.GetCurrentPlayerLoop();
#else
            return PlayerLoop.GetDefaultPlayerLoop();
#endif
        }

        private static void InsertLoopSystem(ref PlayerLoopSystem parentSystem, PlayerLoopInsertRequest request)
        {
            if (!TryFindSubLoopSystemIndex(ref parentSystem, request.ParentType, out var index))
                throw new Exception("Target PlayerLoopSystem does not found. Type:" + request.ParentType.FullName);

            var sourceSystem = parentSystem.subSystemList[index];
            InsertLoopSystem(ref sourceSystem, request.System, request.TargetType, request.Position);
            parentSystem.subSystemList[index] = sourceSystem;
        }

        private static bool TryFindSubLoopSystemIndex(
            ref PlayerLoopSystem parentSystem,
            Type targetSystemType,
            out int index)
        {
            if (targetSystemType == null)
            {
                index = -1;
                return false;
            }

            var systems = parentSystem.subSystemList;
            for (int i = 0; i < systems.Length; i++)
                if (systems[i].type == targetSystemType)
                {
                    index = i;
                    return true;
                }
            index = -1;
            return false;
        }

        private static void InsertLoopSystem(
            ref PlayerLoopSystem rootSystem,
            PlayerLoopSystem systemToInsert,
            Type targetSystemType,
            InsertPosition position)
        {
            int insertIndex;

            if (targetSystemType != null && TryFindSubLoopSystemIndex(ref rootSystem, targetSystemType, out int targetIndex))
            {
                insertIndex = position switch
                {
                    InsertPosition.Begin => targetIndex,
                    InsertPosition.End => targetIndex + 1,
                    _ => targetIndex
                };
            }
            else
            {
                insertIndex = position switch
                {
                    InsertPosition.Begin => 0,
                    InsertPosition.End => rootSystem.subSystemList.Length,
                    _ => 0
                };
            }
            InsertAtIndex(ref rootSystem, systemToInsert, insertIndex);
        }

        private static void InsertAtIndex(
            ref PlayerLoopSystem system,
            PlayerLoopSystem systemToInsert,
            int insertIndex)
        {
            var source = system.subSystemList;
            int count = source.Length;

            var dest = new PlayerLoopSystem[count + 1];

            Array.Copy(source, 0, dest, 0, insertIndex);
            dest[insertIndex] = systemToInsert;
            Array.Copy(source, insertIndex, dest, insertIndex + 1, count - insertIndex);
            system.subSystemList = dest;
        }



        private static void RemoveChildSystem(ref PlayerLoopSystem root, Type targetParentType, Type targetChildType)
        {
            var subSystems = root.subSystemList;

            if (subSystems == null || subSystems.Length == 0)
                return;

            for (int i = 0; i < subSystems.Length; i++)
            {
                ref var parent = ref subSystems[i];
                if (parent.type != targetParentType)
                    continue;

                TryRemoveSubSystem(ref parent, targetChildType);

                break;
            }
        }

        private static void RemoveSystemRecursive(ref PlayerLoopSystem parent, Type targetSystemType)
        {
            var subSystems = parent.subSystemList;

            if (subSystems == null || subSystems.Length == 0)
                return;

            for (int i = 0; i < subSystems.Length; i++)
                RemoveSystemRecursive(ref subSystems[i], targetSystemType);

            TryRemoveSubSystem(ref parent, targetSystemType);
        }

        private static bool TryRemoveSubSystem(ref PlayerLoopSystem parent, Type targetSystemType)
        {
            var source = parent.subSystemList;

            int keepCount = 0;
            for (int i = 0; i < source.Length; i++)
                if (source[i].type != targetSystemType)
                    keepCount++;

            if (keepCount == source.Length)
                return false;

            var dest = new PlayerLoopSystem[keepCount];
            int dst = 0;
            for (int i = 0; i < source.Length; i++)
                if (source[i].type != targetSystemType)
                    dest[dst++] = source[i];

            parent.subSystemList = dest;
            return true;
        }
    }
}