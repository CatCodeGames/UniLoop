using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatCode.PlayerLoops.Tests
{

    public class TestUtility
    {
        public static IEnumerator WaitFrames(int framesCount, PlayerLoopTiming timing)
        {
            var instruction = GetInstruction(timing);

            for (int i = 0; i < framesCount; i++)
                yield return instruction;
        }

        private static YieldInstruction GetInstruction(PlayerLoopTiming timing)
        {
            return timing switch
            {
                PlayerLoopTiming.FixedUpdate => new WaitForFixedUpdate(),
                PlayerLoopTiming.Update => null,
                PlayerLoopTiming.LateUpdate => new WaitForEndOfFrame(),
                _ => null,
            };
        }

        public static void Shuffle<T>(T[] array)
        {
            var count = array.Length;
            for (int i = 0; i < count - 1; i++)
            {
                int j = Random.Range(i, count);
                (array[j], array[i]) = (array[i], array[j]);
            }
        }
    }
}