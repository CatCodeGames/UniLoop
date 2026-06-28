using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace CatCode.PlayerLoops.Tests
{
    public sealed class UniLoopWhileTests
    {
        public enum WhileRunnerStrategy
        {
            WithToken,
            WithHandle
        }

        public static readonly WhileRunnerStrategy[] Strategies = new[]
        {
            WhileRunnerStrategy.WithToken,
            WhileRunnerStrategy.WithHandle
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

        private IWhileStrategy CreateStrategy(WhileRunnerStrategy strategy)
            => strategy switch
            {
                WhileRunnerStrategy.WithToken => new WhileTokenStrategy(),
                WhileRunnerStrategy.WithHandle => new WhileHandleStrategy(),
                _ => null,
            };


        [UnityTest]
        public IEnumerator Check_Complete(
            [ValueSource(nameof(Strategies))] WhileRunnerStrategy strategy,
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var whileStrategy = CreateStrategy(strategy);
            var runner = new WhileTestRunner(whileStrategy, TestSettings.FramesCount);
            runner.Start(timing, phase);

            yield return TestUtility.WaitFrames(TestSettings.FramesCount, timing);

            Assert.IsTrue(runner.Completed);
            Assert.IsFalse(runner.Canceled);
        }

        [UnityTest]
        public IEnumerator Check_Complete_EveryFrame(
            [ValueSource(nameof(Strategies))] WhileRunnerStrategy strategy,
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var whileStrategy = CreateStrategy(strategy);
            var runner = new WhileTestRunner(whileStrategy, TestSettings.FramesCount);
            runner.Start(timing, phase);

            for (int i = 0; i < TestSettings.FramesCount; i++)
            {
                Assert.IsFalse(runner.Completed);
                Assert.IsFalse(runner.Canceled);
                yield return TestUtility.WaitFrames(1, timing);
            }

            Assert.IsTrue(runner.Completed);
            Assert.IsFalse(runner.Canceled);
        }



        [UnityTest]
        public IEnumerator Check_Cancel(
            [ValueSource(nameof(Strategies))] WhileRunnerStrategy strategy,
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var whileStrategy = CreateStrategy(strategy);
            var runner = new WhileTestRunner(whileStrategy, TestSettings.FramesCount);
            runner.Start(timing, phase);

            yield return TestUtility.WaitFrames(TestSettings.HalfFramesCount, timing);

            runner.Cancel();

            yield return TestUtility.WaitFrames(1, timing);

            Assert.IsFalse(runner.Completed);
            Assert.IsTrue(runner.Canceled);
        }

        [UnityTest]
        public IEnumerator Check_Cancel_EveryFrame(
            [ValueSource(nameof(Strategies))] WhileRunnerStrategy strategy,
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var whileStrategy = CreateStrategy(strategy);
            var runner = new WhileTestRunner(whileStrategy, TestSettings.FramesCount);
            runner.Start(timing, phase);

            for (int i = 0; i < TestSettings.HalfFramesCount; i++)
            {
                Assert.IsFalse(runner.Completed);
                Assert.IsFalse(runner.Canceled);
                yield return TestUtility.WaitFrames(1, timing);
            }
            runner.Cancel();

            Assert.IsFalse(runner.Completed);
            Assert.IsFalse(runner.Canceled);

            yield return TestUtility.WaitFrames(1, timing);

            Assert.IsFalse(runner.Completed);
            Assert.IsTrue(runner.Canceled);
        }



        [UnityTest]
        public IEnumerator Check_Process_EveryFrame(
           [ValueSource(nameof(Strategies))] WhileRunnerStrategy strategy,
           [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
           [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var whileStrategy = CreateStrategy(strategy);
            var runner = new WhileTestRunner(whileStrategy, TestSettings.FramesCount);
            runner.Start(timing, phase);

            for (int i = 0; i < TestSettings.FramesCount; i++)
            {
                Assert.AreEqual(runner.CurrentValue, i);
                yield return TestUtility.WaitFrames(1, timing);
            }

            Assert.AreEqual(runner.CurrentValue, TestSettings.FramesCount);
            yield return TestUtility.WaitFrames(1, timing);
            Assert.AreEqual(runner.CurrentValue, TestSettings.FramesCount);
        }

        [UnityTest]
        public IEnumerator Check_Process_WithCancel_EveryFrame(
            [ValueSource(nameof(Strategies))] WhileRunnerStrategy strategy,
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var whileStrategy = CreateStrategy(strategy);
            var runner = new WhileTestRunner(whileStrategy, TestSettings.FramesCount);
            runner.Start(timing, phase);

            for (int i = 0; i < TestSettings.HalfFramesCount; i++)
            {
                Assert.AreEqual(runner.CurrentValue, i);
                yield return TestUtility.WaitFrames(1, timing);
            }
            runner.Cancel();

            Assert.AreEqual(runner.CurrentValue, TestSettings.HalfFramesCount);

            yield return TestUtility.WaitFrames(TestSettings.HalfFramesCount, timing);

            Assert.AreEqual(runner.CurrentValue, TestSettings.HalfFramesCount);
        }


        [UnityTest]
        public IEnumerator Check_Process_Instant_Cancel(
          [ValueSource(nameof(Strategies))] WhileRunnerStrategy strategy,
          [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
          [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var whileStrategy = CreateStrategy(strategy);
            var runner = new WhileTestRunner(whileStrategy, TestSettings.FramesCount);
            runner.Start(timing, phase);

            runner.Cancel();

            yield return TestUtility.WaitFrames(1, timing);

            Assert.AreEqual(runner.CurrentValue, 0);
            Assert.IsTrue(runner.Canceled);
            Assert.IsFalse(runner.Completed);
        }


        [UnityTest]
        public IEnumerator Check_Group_SingleCancel(
            [ValueSource(nameof(Strategies))] WhileRunnerStrategy strategy,
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var runner1 = new WhileTestRunner(CreateStrategy(strategy), TestSettings.FramesCount);
            runner1.Start(timing, phase);

            var runner2 = new WhileTestRunner(CreateStrategy(strategy), TestSettings.FramesCount);
            runner2.Start(timing, phase);

            var runner3 = new WhileTestRunner(CreateStrategy(strategy), TestSettings.FramesCount);
            runner3.Start(timing, phase);

            for (int i = 0; i < TestSettings.HalfFramesCount; i++)
            {
                Assert.AreEqual(runner1.CurrentValue, i);
                Assert.AreEqual(runner2.CurrentValue, i);
                Assert.AreEqual(runner3.CurrentValue, i);

                yield return TestUtility.WaitFrames(1, timing);
            }

            runner2.Cancel();

            Assert.AreEqual(runner1.CurrentValue, TestSettings.HalfFramesCount);
            Assert.AreEqual(runner2.CurrentValue, TestSettings.HalfFramesCount);
            Assert.AreEqual(runner3.CurrentValue, TestSettings.HalfFramesCount);

            yield return TestUtility.WaitFrames(1, timing);

            for (int i = TestSettings.HalfFramesCount + 1; i < TestSettings.FramesCount; i++)
            {
                Assert.AreEqual(runner1.CurrentValue, i);
                Assert.AreNotEqual(runner2.CurrentValue, i);
                Assert.AreEqual(runner3.CurrentValue, i);

                yield return TestUtility.WaitFrames(1, timing);
            }
            Assert.AreEqual(runner1.CurrentValue, TestSettings.FramesCount);
        }

        [UnityTest]
        public IEnumerator Check_Group_CompleteAndCancel_Repeat(
            [ValueSource(nameof(Strategies))] WhileRunnerStrategy strategy,
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            Random.InitState(0);

            yield return TestUtility.WaitFrames(1, timing);

            int startRunnersCount = 23;
            int iterations = 4;

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                var runnersCount = (iteration + 1) * startRunnersCount;
                var runners = new WhileTestRunner[runnersCount];
                for (int i = 0; i < runnersCount; i++)
                {
                    var runner = new WhileTestRunner(CreateStrategy(strategy), TestSettings.FramesCount);
                    runner.Start(timing, phase);
                    runners[i] = runner;
                }

                TestUtility.Shuffle(runners);

                int half = runnersCount / 2;

                var completeGroup = runners[0..half];
                var cancelGroup = runners[half..runnersCount];

                yield return TestUtility.WaitFrames(TestSettings.HalfFramesCount, timing);

                foreach (var runner in cancelGroup)
                    runner.Cancel();

                yield return TestUtility.WaitFrames(TestSettings.HalfFramesCount, timing);

                foreach (var runner in completeGroup)
                {
                    Assert.AreEqual(TestSettings.FramesCount, runner.CurrentValue);
                    Assert.IsTrue(runner.Completed);
                    Assert.IsFalse(runner.Canceled);
                }

                foreach (var runner in cancelGroup)
                {
                    Assert.AreEqual(TestSettings.HalfFramesCount, runner.CurrentValue);
                    Assert.IsFalse(runner.Completed);
                    Assert.IsTrue(runner.Canceled);
                }
            }
        }
    }
}