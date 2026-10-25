using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.TestTools;

namespace CatCode.PlayerLoops.Tests
{
    public sealed class UniLoopSlimTests
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

        private readonly List<SlimLoopHandle> _handles = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var handle in _handles)
                handle.Dispose();
            _handles.Clear();
        }

        private SlimLoopHandle Track(SlimLoopHandle handle)
        {
            _handles.Add(handle);
            return handle;
        }


        [UnityTest]
        public IEnumerator Process_EveryFrame(
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var calls = 0;
            Track(UniLoop.ScheduleSlim(() => calls++, timing, phase));

            for (int i = 0; i < TestSettings.FramesCount; i++)
            {
                Assert.AreEqual(i, calls);
                yield return TestUtility.WaitFrames(1, timing);
            }

            Assert.AreEqual(TestSettings.FramesCount, calls);
        }

        [UnityTest]
        public IEnumerator Process_StopsAfterCancel(
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var calls = 0;
            var handle = Track(UniLoop.ScheduleSlim(() => calls++, timing, phase));

            for (int i = 0; i < TestSettings.HalfFramesCount; i++)
            {
                Assert.AreEqual(i, calls);
                yield return TestUtility.WaitFrames(1, timing);
            }

            handle.Dispose();
            Assert.IsTrue(handle.IsDisposed);

            yield return TestUtility.WaitFrames(TestSettings.HalfFramesCount, timing);

            Assert.AreEqual(TestSettings.HalfFramesCount, calls);
        }

        [UnityTest]
        public IEnumerator Cancel_SameFrame(
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing,
            [ValueSource(nameof(Phases))] PlayerLoopPhase phase)
        {
            yield return TestUtility.WaitFrames(1, timing);

            var calls = 0;
            var handle = Track(UniLoop.ScheduleSlim(() => calls++, timing, phase));
            handle.Dispose();

            yield return TestUtility.WaitFrames(TestSettings.HalfFramesCount, timing);

            Assert.AreEqual(0, calls);
            Assert.IsTrue(handle.IsDisposed);
        }
    }
}
