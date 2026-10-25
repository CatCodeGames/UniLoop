using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace CatCode.PlayerLoops.Tests
{
    public sealed class UniLoopPhaseOrderTests
    {
        private const string Early = "Early";
        private const string Late = "Late";
        private const int StepsCount = 5;

        public static readonly PlayerLoopTiming[] Timings =
        {
            PlayerLoopTiming.Update,
            PlayerLoopTiming.LateUpdate,
            PlayerLoopTiming.FixedUpdate
        };

        private readonly List<string> _log = new();
        private readonly List<LoopHandle> _handles = new();
        private GameObject _probeObject;

        [TearDown]
        public void TearDown()
        {
            foreach (var handle in _handles)
                handle.Dispose();
            _handles.Clear();

            if (_probeObject != null)
                Object.Destroy(_probeObject);
            _probeObject = null;

            _log.Clear();
        }

        [UnityTest]
        public IEnumerator Order_EarlyMonoBehaviourLate(
            [ValueSource(nameof(Timings))] PlayerLoopTiming timing)
        {
            _probeObject = new GameObject(nameof(PhaseOrderProbe));
            _probeObject.AddComponent<PhaseOrderProbe>().Init(timing, _log);

            _handles.Add(UniLoop.Schedule(() => _log.Add(Early), timing, PlayerLoopPhase.Early));
            _handles.Add(UniLoop.Schedule(() => _log.Add(Late), timing, PlayerLoopPhase.Late));

            yield return TestUtility.WaitFrames(StepsCount + 3, timing);

            // The first step may be partial because the probe and the actions start at different points.
            // The check starts from the first full step, so a wrong order never matches the start.
            var start = FindFirstStep();
            Assert.GreaterOrEqual(start, 0, string.Join(", ", _log));

            var steps = (_log.Count - start) / 3;
            Assert.GreaterOrEqual(steps, StepsCount);

            for (int i = 0; i < steps; i++)
            {
                var index = start + i * 3;
                Assert.AreEqual(Early, _log[index], $"Step {i}");
                Assert.AreEqual(PhaseOrderProbe.Mark, _log[index + 1], $"Step {i}");
                Assert.AreEqual(Late, _log[index + 2], $"Step {i}");
            }
        }

        private int FindFirstStep()
        {
            for (int i = 0; i < _log.Count - 1; i++)
                if (_log[i] == Early && _log[i + 1] == PhaseOrderProbe.Mark)
                    return i;
            return -1;
        }
    }
}
