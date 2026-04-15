using System;
using System.Threading;

namespace CatCode.PlayerLoops
{
    public interface IWhileScheduler
    {
        WhileHandle Schedule(Func<bool> predicate);
        TokenWhileHandle Schedule(Func<bool> predicate, CancellationToken cancellationToken);
    }
}