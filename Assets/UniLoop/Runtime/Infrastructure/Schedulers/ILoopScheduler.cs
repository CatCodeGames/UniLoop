using System;
using System.Threading;

namespace CatCode.PlayerLoops
{
    public interface ILoopScheduler
    {
        SlimLoopHandle ScheduleSlim(Action action);
        LoopHandle Schedule(Action action);
        TokenLoopHandle Schedule(Action action, CancellationToken cancellationToken);
        WhileHandle Schedule(Action action, int count);
        WhileHandle ScheduleOnce(Action action);
    }
}