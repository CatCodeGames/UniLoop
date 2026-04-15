namespace CatCode.PlayerLoops
{
    public interface ITimingScheduler
    {
        ILoopScheduler Loop { get; }
        IWhileScheduler While { get; }
    }
}