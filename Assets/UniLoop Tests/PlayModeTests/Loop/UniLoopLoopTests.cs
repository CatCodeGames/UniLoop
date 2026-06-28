using JetBrains.Annotations;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace CatCode.PlayerLoops.Tests
{
    public sealed class UniLoopLoopTests
    {
        public enum LoopRunnerStrategy
        {
            WithToken,
            WithHandle
        }

        public static readonly LoopRunnerStrategy[] Strategies = new[]
        {
            LoopRunnerStrategy.WithToken,
            LoopRunnerStrategy.WithHandle
        };

        public static readonly PlayerLoopTiming[] Timings =
{
            PlayerLoopTiming.Update,
            PlayerLoopTiming.LateUpdate,
            PlayerLoopTiming.FixedUpdate
        };

        public static readonly PlayerLoopPhase[] Phases =
        {
            PlayerLoopPhase.Early,
            PlayerLoopPhase.Late
        };

        private ILoopStrategy CreateStrategy(LoopRunnerStrategy strategy)
            => strategy switch
            {
                LoopRunnerStrategy.WithToken => new LoopTokenStrategy(),
                LoopRunnerStrategy.WithHandle => new LoopHandleStrategy(),
                _ => null,
            };

        [UnityTest]
        public IEnumerator Check_Cancel(
           [ValueSource(nameof(Strategies))] LoopRunnerStrategy strategy,
           [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
           [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var runner = new LoopTestRunner(CreateStrategy(strategy));
            runner.Start(timing, phase);

            Assert.IsFalse(runner.Canceled);
            yield return TestUtility.WaitFrames(TestSettings.FramesCount, timing);
            runner.Cancel();
            yield return TestUtility.WaitFrames(1, timing);
            Assert.IsTrue(runner.Canceled);
        }
       
        [UnityTest]
        public IEnumerator Check_Cancel_EveryFrame(
            [ValueSource(nameof(Strategies))] LoopRunnerStrategy strategy,
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var runner = new LoopTestRunner(CreateStrategy(strategy));
            runner.Start(timing, phase);

            for (int i = 0; i < TestSettings.FramesCount; i++)
            {
                Assert.IsFalse(runner.Canceled);
                yield return TestUtility.WaitFrames(1, timing);
            }
            runner.Cancel();
            yield return TestUtility.WaitFrames(1, timing);
            Assert.IsTrue(runner.Canceled);
        }

        [UnityTest]
        public IEnumerator Check_Process_WithCancel_EveryFrame(
            [ValueSource(nameof(Strategies))] LoopRunnerStrategy strategy,
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var runner = new LoopTestRunner(CreateStrategy(strategy));
            runner.Start(timing, phase);

            for (int i = 0; i < TestSettings.HalfFramesCount; i++)
            {
                Assert.AreEqual(runner.CurrentValue, i);
                yield return TestUtility.WaitFrames(1, timing);
            }
            runner.Cancel();
            yield return TestUtility.WaitFrames(1, timing);
            Assert.AreEqual(runner.CurrentValue, TestSettings.HalfFramesCount);
        }

        [UnityTest]
        public IEnumerator Check_Process_Instant_Cancel(
            [ValueSource(nameof(Strategies))] LoopRunnerStrategy strategy,
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var runner = new LoopTestRunner(CreateStrategy(strategy));
            runner.Start(timing, phase);

            runner.Cancel();

            yield return TestUtility.WaitFrames(1, timing);

            Assert.AreEqual(runner.CurrentValue,0);
            Assert.IsTrue(runner.Canceled);
        }

        [UnityTest]
        public IEnumerator Check_Group_RandomCancel_Repeat(
            [ValueSource(nameof(Strategies))] LoopRunnerStrategy strategy,
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            Random.InitState(0);

            yield return TestUtility.WaitFrames(1, timing);

            const int totalFrames = 10;
            const int createPerFrame = 8;
            const int cancelPerFrame = 4;

            var active = new List<LoopRunnerInfo>();
            var canceled = new List<LoopRunnerInfo>();

            for (int frame = 0; frame < totalFrames; frame++)
            {
                for (int i = 0; i < createPerFrame; i++)
                {
                    var runner = new LoopTestRunner(CreateStrategy(strategy));
                    runner.Start(timing, phase);
                    active.Add(new LoopRunnerInfo(runner, frame));
                }

                yield return TestUtility.WaitFrames(1, timing);

                for (int i = 0; i < cancelPerFrame; i++)
                {
                    int index = Random.Range(0, active.Count);

                    var info = active[index];
                    info.StopFrame = frame + 1;

                    info.Runner.Cancel();

                    canceled.Add(info);
                    active.RemoveAt(index);
                }
            }

            foreach (var info in active)
            {
                info.StopFrame = totalFrames;
                info.Runner.Cancel();
                canceled.Add(info);
            }

            yield return TestUtility.WaitFrames(1, timing);

            foreach (var info in canceled)
            {
                Assert.IsTrue(info.Runner.Canceled);
                Assert.AreEqual(
                    info.StopFrame - info.StartFrame,
                    info.Runner.CurrentValue);
            }
        }
    }
}