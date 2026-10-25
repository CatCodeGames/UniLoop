using NUnit.Framework;
using System;
using System.Threading;

namespace CatCode.PlayerLoops.Tests
{
    public sealed class UniLoopAccessTests
    {
        [Test]
        public void Schedule_FromWorkerThread()
        {
            Exception error = null;

            var thread = new Thread(() =>
            {
                try
                {
                    UniLoop.Schedule(() => { }, PlayerLoopTiming.Update, PlayerLoopPhase.Early);
                }
                catch (Exception e)
                {
                    error = e;
                }
            });
            thread.Start();
            thread.Join();

            Assert.IsInstanceOf<InvalidOperationException>(error);
        }
    }
}
