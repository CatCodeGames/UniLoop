using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.TestTools;

namespace CatCode.PlayerLoops.Tests
{
    public sealed class UniLoopCountedTests
    {
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

        private readonly List<WhileHandle> _handles = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var handle in _handles)
                handle.Dispose();
            _handles.Clear();
        }

        private WhileHandle Track(WhileHandle handle)
        {
            _handles.Add(handle);
            return handle;
        }


        [UnityTest]
        public IEnumerator Complete_AfterCount(
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var calls = 0;
            var completed = false;
            Track(UniLoop.Schedule(() => calls++, TestSettings.FramesCount, timing, phase)
                .SetOnCompleted(() => completed = true));

            for (int i = 0; i < TestSettings.FramesCount; i++)
            {
                Assert.AreEqual(i, calls);
                Assert.IsFalse(completed);
                yield return TestUtility.WaitFrames(1, timing);
            }

            Assert.AreEqual(TestSettings.FramesCount, calls);
            Assert.IsTrue(completed);

            yield return TestUtility.WaitFrames(1, timing);
            Assert.AreEqual(TestSettings.FramesCount, calls);
        }

        [UnityTest]
        public IEnumerator Once_SingleRun(
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var calls = 0;
            var completed = false;
            Track(UniLoop.ScheduleOnce(() => calls++, timing, phase)
                .SetOnCompleted(() => completed = true));

            Assert.AreEqual(0, calls);
            Assert.IsFalse(completed);

            yield return TestUtility.WaitFrames(1, timing);

            Assert.AreEqual(1, calls);
            Assert.IsTrue(completed);

            yield return TestUtility.WaitFrames(TestSettings.HalfFramesCount, timing);
            Assert.AreEqual(1, calls);
        }

        [UnityTest]
        public IEnumerator Cancel_Halfway(
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var calls = 0;
            var completed = false;
            var canceled = false;
            var handle = Track(UniLoop.Schedule(() => calls++, TestSettings.FramesCount, timing, phase)
                .SetOnCompleted(() => completed = true)
                .SetOnCanceled(() => canceled = true));

            for (int i = 0; i < TestSettings.HalfFramesCount; i++)
            {
                Assert.AreEqual(i, calls);
                yield return TestUtility.WaitFrames(1, timing);
            }

            handle.Dispose();
            Assert.IsFalse(canceled);

            yield return TestUtility.WaitFrames(1, timing);

            Assert.AreEqual(TestSettings.HalfFramesCount, calls);
            Assert.IsTrue(canceled);
            Assert.IsFalse(completed);

            yield return TestUtility.WaitFrames(TestSettings.HalfFramesCount, timing);

            Assert.AreEqual(TestSettings.HalfFramesCount, calls);
            Assert.IsFalse(completed);
        }
    }
}
