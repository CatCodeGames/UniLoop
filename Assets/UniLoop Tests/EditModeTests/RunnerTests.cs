using NUnit.Framework;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is =UnityEngine.TestTools.Constraints.Is;

namespace CatCode.PlayerLoops.Tests
{
    public class RunnerTests
    {
        public enum RunnerType
        {
            Slim,
            Loop,
            TokenLoop,
            CountedLoop,
            While,
            TokenWhile
        }

        private const string ErrorMessage = "UniLoop test exception";
        private static readonly Regex ErrorPattern = new(ErrorMessage);

        private static readonly Action EmptyAction = () => { };
        private static readonly Func<bool> CompletePredicate = () => false;

        private static void ThrowAction() => throw new InvalidOperationException(ErrorMessage);
        private static bool ThrowPredicate() => throw new InvalidOperationException(ErrorMessage);


        [Test]
        public void Exception_InAction()
        {
            var runner = new DefaultLoopRunner();
            var calls = 0;

            runner.Schedule(ThrowAction);
            runner.Schedule(() => calls++);

            LogAssert.Expect(LogType.Exception, ErrorPattern);
            runner.Run();
            LogAssert.Expect(LogType.Exception, ErrorPattern);
            runner.Run();

            Assert.AreEqual(2, calls);
        }

        [Test]
        public void Exception_InPredicate()
        {
            var runner = new DefaultWhileRunner();
            var completed = false;

            var throwing = runner.Schedule(ThrowPredicate);
            var normal = runner.Schedule(CompletePredicate);
            runner.SetOnCompleted(normal, () => completed = true);

            LogAssert.Expect(LogType.Exception, ErrorPattern);
            runner.Run();

            Assert.IsTrue(completed);
            Assert.IsTrue(runner.IsValid(throwing));
        }

        [Test]
        public void Exception_InCallback()
        {
            var runner = new DefaultLoopRunner();
            var canceled = false;

            var h1 = runner.Schedule(EmptyAction);
            var h2 = runner.Schedule(EmptyAction);
            runner.SetOnCanceled(h1, ThrowAction);
            runner.SetOnCanceled(h2, () => canceled = true);

            runner.Cancel(h1);
            runner.Cancel(h2);

            LogAssert.Expect(LogType.Exception, ErrorPattern);
            runner.Run();

            Assert.IsTrue(canceled);
            Assert.IsFalse(runner.IsValid(h1));
            Assert.IsFalse(runner.IsValid(h2));
        }

        [Test]
        public void SlimCancel_BeforeRun()
        {
            var runner = new SlimLoopRunner(4);
            var calls = 0;

            var handle = runner.Add(() => calls++);
            runner.Run();

            runner.Cancel(handle);
            runner.Run();

            Assert.AreEqual(1, calls);
            Assert.IsFalse(runner.IsValid(handle));
        }

        [Test]
        public void CountedLoop_CompletesAfterCount()
        {
            var runner = new CountedLoopRunner();
            var calls = 0;
            var completed = false;

            var handle = runner.Schedule(() => calls++, 3);
            runner.SetOnCompleted(handle, () => completed = true);

            for (int i = 0; i < 5; i++)
                runner.Run();

            Assert.AreEqual(3, calls);
            Assert.IsTrue(completed);
            Assert.IsFalse(runner.IsValid(handle));
        }

        [Test]
        public void IsDisposed_RightAfterDispose()
        {
            var slimRunner = new SlimLoopRunner(4);
            var slim = new SlimLoopHandle(slimRunner, slimRunner.Add(EmptyAction));

            var loopRunner = new DefaultLoopRunner();
            var loop = new LoopHandle(loopRunner, loopRunner.Schedule(EmptyAction));

            var whileRunner = new DefaultWhileRunner();
            var whileHandle = new WhileHandle(whileRunner, whileRunner.Schedule(() => true));

            Assert.IsFalse(slim.IsDisposed);
            Assert.IsFalse(loop.IsDisposed);
            Assert.IsFalse(whileHandle.IsDisposed);

            slim.Dispose();
            loop.Dispose();
            whileHandle.Dispose();

            Assert.IsTrue(slim.IsDisposed);
            Assert.IsTrue(loop.IsDisposed);
            Assert.IsTrue(whileHandle.IsDisposed);
        }

        [Test]
        public void GCAlloc_ScheduleAndFinish([Values] RunnerType type)
        {
            var cycle = CreateCycle(type);

            cycle();
            cycle();

            Assert.That(() => cycle(), Is.Not.AllocatingGCMemory());
        }

        // One cycle schedules an element and runs the runner, so the element finishes
        // through the cancel/complete path and its callback goes through the finished queue.
        private static Action CreateCycle(RunnerType type)
        {
            switch (type)
            {
                case RunnerType.Slim:
                    {
                        var runner = new SlimLoopRunner(4);
                        return () =>
                        {
                            runner.Cancel(runner.Add(EmptyAction));
                            runner.Run();
                        };
                    }
                case RunnerType.Loop:
                    {
                        var runner = new DefaultLoopRunner();
                        return () =>
                        {
                            runner.Cancel(runner.Schedule(EmptyAction));
                            runner.Run();
                        };
                    }
                case RunnerType.TokenLoop:
                    {
                        var runner = new TokenLoopRunner();
                        var token = new CancellationToken(true);
                        return () =>
                        {
                            runner.Schedule(EmptyAction, token);
                            runner.Run();
                        };
                    }
                case RunnerType.CountedLoop:
                    {
                        var runner = new CountedLoopRunner();
                        return () =>
                        {
                            runner.Schedule(EmptyAction, 1);
                            runner.Run();
                        };
                    }
                case RunnerType.While:
                    {
                        var runner = new DefaultWhileRunner();
                        return () =>
                        {
                            runner.Schedule(CompletePredicate);
                            runner.Run();
                        };
                    }
                case RunnerType.TokenWhile:
                    {
                        var runner = new TokenWhileRunner();
                        return () =>
                        {
                            runner.Schedule(CompletePredicate, CancellationToken.None);
                            runner.Run();
                        };
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
