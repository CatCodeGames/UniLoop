using NUnit.Framework;
using System;
using System.Collections;
using Unity.PerformanceTesting;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;

namespace CatCode.PlayerLoops.Benchmarks
{
    /// <summary>
    /// Compares UniLoop with the standard ways to run code every frame.
    /// Every method runs the same work in Update for the same number of tasks.
    /// </summary>
    /// <remarks>
    /// Sample groups:
    /// Start / Stop — time of the call that starts or stops all tasks;
    /// Stop Next Frame — the frame after Stop, where deferred removal and cancellation happen;
    /// Frame — full frame time while all tasks run (compare with the Empty method);
    /// Start GC / Frame GC — managed allocations of the frame (Editor and development builds only).
    ///
    /// GC values count every allocation of the frame, including the Editor itself.
    /// Read them relative to the Empty method: a value equal to Empty means the method allocates nothing.
    /// </remarks>
    public sealed class ComparisonBenchmarks
    {
        public enum Method
        {
            Empty,
            UniLoopSlim,
            UniLoopLoop,
            UniLoopTokenLoop,
            UniLoopWhile,
            UpdateManager,
            Coroutine,
            UniTaskYield
        }

        public enum PipelineCancelMethod
        {
            Empty,
            UniLoopLoop,
            UpdateManager,
            Coroutine,
            UniTaskYield
        }

        public enum PipelineCompleteMethod
        {
            Empty,
            UniLoopCounted,
            UniLoopWhile,
            UpdateManager,
            Coroutine,
            UniTaskYield
        }

        private const int TaskCount = 10_000;
        private const int Cycles = 5;
        private const int WarmupFrames = 5;
        private const int ExecutionFrames = 30;

        // In the pipeline about TasksPerFrame * Lifetime = 10 000 tasks are active at any time.
        private const int PipelineTasksPerFrame = 1000;
        private const int PipelineLifetime = 10;
        private const int PipelineFrames = 100;
        private const int PipelineCapacity = PipelineTasksPerFrame * (PipelineLifetime + 2);

        private int _vSyncCount;
        private int _targetFrameRate;

        // VSync or a frame rate cap would make every frame take the same time and hide the difference.
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _vSyncCount = QualitySettings.vSyncCount;
            _targetFrameRate = Application.targetFrameRate;

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            QualitySettings.vSyncCount = _vSyncCount;
            Application.targetFrameRate = _targetFrameRate;
        }


        [UnityTest, Performance]
        public IEnumerator Compare([Values] Method method)
        {
            // A SampleGroup stores its own samples, so the groups are created per test and never shared.
            var startGroup = new SampleGroup("Start", SampleUnit.Millisecond);
            var stopGroup = new SampleGroup("Stop", SampleUnit.Millisecond);
            var stopNextFrameGroup = new SampleGroup("Stop Next Frame", SampleUnit.Millisecond);
            var frameGroup = new SampleGroup("Frame", SampleUnit.Millisecond);
            var startGCGroup = new SampleGroup("Start GC", SampleUnit.Byte);
            var frameGCGroup = new SampleGroup("Frame GC", SampleUnit.Byte);

            using var gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            using var driver = CreateDriver(method);

            // Stopwatch instead of Measure.Scope: Measure.Scope boxes a struct on every call
            // and that allocation would show up in Frame GC of every method.
            var watch = new System.Diagnostics.Stopwatch();

            GC.Collect();
            yield return WaitFrames(WarmupFrames);

            for (int cycle = 0; cycle < Cycles; cycle++)
            {
                watch.Restart();
                driver.Start();
                watch.Stop();
                Measure.Custom(startGroup, watch.Elapsed.TotalMilliseconds);

                // After one frame LastValue holds the allocations of the frame with Start.
                yield return null;
                Measure.Custom(startGCGroup, gcRecorder.LastValue);

                yield return WaitFrames(WarmupFrames);

                for (int i = 0; i < ExecutionFrames; i++)
                {
                    watch.Restart();
                    yield return null;
                    watch.Stop();

                    Measure.Custom(frameGroup, watch.Elapsed.TotalMilliseconds);
                    Measure.Custom(frameGCGroup, gcRecorder.LastValue);
                }

                watch.Restart();
                driver.Stop();
                watch.Stop();
                Measure.Custom(stopGroup, watch.Elapsed.TotalMilliseconds);

                watch.Restart();
                yield return null;
                watch.Stop();
                Measure.Custom(stopNextFrameGroup, watch.Elapsed.TotalMilliseconds);

                yield return WaitFrames(WarmupFrames);
            }
        }


        /// <summary>
        /// Pipeline where tasks are stopped by outside code: every frame 1000 tasks start
        /// and the tasks started 10 frames ago are canceled one by one.
        /// </summary>
        [UnityTest, Performance]
        public IEnumerator PipelineCancel([Values] PipelineCancelMethod method)
        {
            using var driver = CreateCancelDriver(method);
            yield return RunPipeline(driver, isCancelScenario: true);
        }

        /// <summary>
        /// Pipeline where tasks end by themselves: every frame 1000 tasks start and each runs 10 frames.
        /// </summary>
        [UnityTest, Performance]
        public IEnumerator PipelineComplete([Values] PipelineCompleteMethod method)
        {
            using var driver = CreateCompleteDriver(method);
            yield return RunPipeline(driver, isCancelScenario: false);
        }

        /// <remarks>
        /// Sample groups:
        /// Operations — time of all start (and cancel) calls of one frame;
        /// Frame — the rest of the frame, where tasks run and finished tasks are removed;
        /// Frame GC — managed allocations of the frame, read relative to Empty (Editor and development builds only).
        /// </remarks>
        private static IEnumerator RunPipeline(IPipelineDriver driver, bool isCancelScenario)
        {
            var operationsGroup = new SampleGroup("Operations", SampleUnit.Millisecond);
            var frameGroup = new SampleGroup("Frame", SampleUnit.Millisecond);
            var frameGCGroup = new SampleGroup("Frame GC", SampleUnit.Byte);

            using var gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");

            // Ids of cancelable tasks by the frame they started in, reused as a ring buffer.
            var startedIds = new int[PipelineLifetime][];
            for (int i = 0; i < PipelineLifetime; i++)
                startedIds[i] = new int[PipelineTasksPerFrame];

            var watch = new System.Diagnostics.Stopwatch();

            GC.Collect();
            yield return WaitFrames(WarmupFrames);

            // The first Lifetime frames fill the pipeline, then a few frames let it settle before measuring.
            var measureFrom = PipelineLifetime + WarmupFrames;
            var totalFrames = measureFrom + PipelineFrames;

            for (int frame = 0; frame < totalFrames; frame++)
            {
                var isMeasured = frame >= measureFrom;

                watch.Restart();

                if (isCancelScenario)
                {
                    var ids = startedIds[frame % PipelineLifetime];

                    if (frame >= PipelineLifetime)
                    {
                        for (int i = 0; i < PipelineTasksPerFrame; i++)
                            driver.Cancel(ids[i]);
                    }

                    for (int i = 0; i < PipelineTasksPerFrame; i++)
                        ids[i] = driver.StartCancelable();
                }
                else
                {
                    for (int i = 0; i < PipelineTasksPerFrame; i++)
                        driver.StartFinite(PipelineLifetime);
                }

                watch.Stop();
                if (isMeasured)
                    Measure.Custom(operationsGroup, watch.Elapsed.TotalMilliseconds);

                watch.Restart();
                yield return null;
                watch.Stop();

                if (isMeasured)
                {
                    Measure.Custom(frameGroup, watch.Elapsed.TotalMilliseconds);
                    Measure.Custom(frameGCGroup, gcRecorder.LastValue);
                }
            }

            // In the cancel scenario every ring slot still holds running tasks.
            // Finite tasks run out on their own within Lifetime frames.
            if (isCancelScenario)
            {
                foreach (var ids in startedIds)
                {
                    for (int i = 0; i < PipelineTasksPerFrame; i++)
                        driver.Cancel(ids[i]);
                }
            }

            yield return WaitFrames(PipelineLifetime + 1);
        }


        private static IPipelineDriver CreateCancelDriver(PipelineCancelMethod method)
            => method switch
            {
                PipelineCancelMethod.Empty => new EmptyPipelineDriver(),
                PipelineCancelMethod.UniLoopLoop => new UniLoopPipelineDriver(PipelineCapacity),
                PipelineCancelMethod.UpdateManager => new UpdateManagerPipelineDriver(PipelineCapacity),
                PipelineCancelMethod.Coroutine => new CoroutinePipelineDriver(PipelineCapacity),
                PipelineCancelMethod.UniTaskYield => new UniTaskPipelineDriver(PipelineCapacity),
                _ => throw new ArgumentOutOfRangeException(nameof(method)),
            };

        private static IPipelineDriver CreateCompleteDriver(PipelineCompleteMethod method)
            => method switch
            {
                PipelineCompleteMethod.Empty => new EmptyPipelineDriver(),
                PipelineCompleteMethod.UniLoopCounted => new UniLoopPipelineDriver(PipelineCapacity),
                PipelineCompleteMethod.UniLoopWhile => new UniLoopWhilePipelineDriver(),
                PipelineCompleteMethod.UpdateManager => new UpdateManagerPipelineDriver(PipelineCapacity),
                PipelineCompleteMethod.Coroutine => new CoroutinePipelineDriver(PipelineCapacity),
                PipelineCompleteMethod.UniTaskYield => new UniTaskPipelineDriver(PipelineCapacity),
                _ => throw new ArgumentOutOfRangeException(nameof(method)),
            };

        private static ITaskDriver CreateDriver(Method method)
            => method switch
            {
                Method.Empty => new EmptyDriver(),
                Method.UniLoopSlim => new UniLoopSlimDriver(TaskCount),
                Method.UniLoopLoop => new UniLoopLoopDriver(TaskCount),
                Method.UniLoopTokenLoop => new UniLoopTokenLoopDriver(TaskCount),
                Method.UniLoopWhile => new UniLoopWhileDriver(TaskCount),
                Method.UpdateManager => new UpdateManagerDriver(TaskCount),
                Method.Coroutine => new CoroutineDriver(TaskCount),
                Method.UniTaskYield => new UniTaskYieldDriver(TaskCount),
                _ => throw new ArgumentOutOfRangeException(nameof(method)),
            };

        private static IEnumerator WaitFrames(int count)
        {
            for (int i = 0; i < count; i++)
                yield return null;
        }
    }
}
