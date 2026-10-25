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

        private readonly List<WhileTestRunner> _runners = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var runner in _runners)
                runner.Cancel();
            _runners.Clear();
        }

        private WhileTestRunner CreateRunner(WhileRunnerStrategy strategy)
        {
            var runner = new WhileTestRunner(CreateStrategy(strategy), TestSettings.FramesCount);
            _runners.Add(runner);
            return runner;
        }

        private IWhileStrategy CreateStrategy(WhileRunnerStrategy strategy)
            => strategy switch
            {
                WhileRunnerStrategy.WithToken => new WhileTokenStrategy(),
                WhileRunnerStrategy.WithHandle => new WhileHandleStrategy(),
                _ => null,
            };


        [UnityTest]
        public IEnumerator Complete_AfterFrames(
            [ValueSource(nameof(Strategies))] WhileRunnerStrategy strategy,
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var runner = CreateRunner(strategy);
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
        public IEnumerator Cancel_AfterFrames(
            [ValueSource(nameof(Strategies))] WhileRunnerStrategy strategy,
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var runner = CreateRunner(strategy);
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
        public IEnumerator Cancel_SameFrame(
            [ValueSource(nameof(Strategies))] WhileRunnerStrategy strategy,
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var runner = CreateRunner(strategy);
            runner.Start(timing, phase);

            runner.Cancel();

            yield return TestUtility.WaitFrames(1, timing);

            Assert.AreEqual(0, runner.CurrentValue);
            Assert.IsTrue(runner.Canceled);
            Assert.IsFalse(runner.Completed);
        }

        [UnityTest]
        public IEnumerator Process_StopsAfterComplete(
            [ValueSource(nameof(Strategies))] WhileRunnerStrategy strategy,
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var runner = CreateRunner(strategy);
            runner.Start(timing, phase);

            for (int i = 0; i < TestSettings.FramesCount; i++)
            {
                Assert.AreEqual(i, runner.CurrentValue);
                yield return TestUtility.WaitFrames(1, timing);
            }

            Assert.AreEqual(TestSettings.FramesCount, runner.CurrentValue);
            yield return TestUtility.WaitFrames(1, timing);
            Assert.AreEqual(TestSettings.FramesCount, runner.CurrentValue);
        }

        [UnityTest]
        public IEnumerator Process_StopsAfterCancel(
            [ValueSource(nameof(Strategies))] WhileRunnerStrategy strategy,
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var runner = CreateRunner(strategy);
            runner.Start(timing, phase);

            for (int i = 0; i < TestSettings.HalfFramesCount; i++)
            {
                Assert.AreEqual(i, runner.CurrentValue);
                yield return TestUtility.WaitFrames(1, timing);
            }
            runner.Cancel();

            Assert.AreEqual(TestSettings.HalfFramesCount, runner.CurrentValue);

            yield return TestUtility.WaitFrames(TestSettings.HalfFramesCount, timing);

            Assert.AreEqual(TestSettings.HalfFramesCount, runner.CurrentValue);
        }

        [UnityTest]
        public IEnumerator Cancel_OneInGroup(
            [ValueSource(nameof(Strategies))] WhileRunnerStrategy strategy,
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var runner1 = CreateRunner(strategy);
            runner1.Start(timing, phase);

            var runner2 = CreateRunner(strategy);
            runner2.Start(timing, phase);

            var runner3 = CreateRunner(strategy);
            runner3.Start(timing, phase);

            for (int i = 0; i < TestSettings.HalfFramesCount; i++)
            {
                Assert.AreEqual(i, runner1.CurrentValue);
                Assert.AreEqual(i, runner2.CurrentValue);
                Assert.AreEqual(i, runner3.CurrentValue);

                yield return TestUtility.WaitFrames(1, timing);
            }

            runner2.Cancel();

            Assert.AreEqual(TestSettings.HalfFramesCount, runner1.CurrentValue);
            Assert.AreEqual(TestSettings.HalfFramesCount, runner2.CurrentValue);
            Assert.AreEqual(TestSettings.HalfFramesCount, runner3.CurrentValue);

            yield return TestUtility.WaitFrames(1, timing);

            for (int i = TestSettings.HalfFramesCount + 1; i < TestSettings.FramesCount; i++)
            {
                Assert.AreEqual(i, runner1.CurrentValue);
                Assert.AreEqual(TestSettings.HalfFramesCount, runner2.CurrentValue);
                Assert.AreEqual(i, runner3.CurrentValue);

                yield return TestUtility.WaitFrames(1, timing);
            }
            Assert.AreEqual(TestSettings.FramesCount, runner1.CurrentValue);
        }

        [UnityTest]
        public IEnumerator CompleteAndCancel_RandomInGroup(
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
                    var runner = CreateRunner(strategy);
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
