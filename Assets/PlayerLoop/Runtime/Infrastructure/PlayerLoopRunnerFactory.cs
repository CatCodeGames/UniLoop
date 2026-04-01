namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Factory for creating a PlayerLoopRunner with default runner configurations.
    /// </summary>
    internal static class PlayerLoopRunnerFactory
    {
        public const int StartSize = 64;
        public const int GrowSize = 64;

        public static PlayerLoopRunner Create()
        {
            return new PlayerLoopRunner(
                new SlimLoopRunner(StartSize, GrowSize),
                new DefaultLoopRunner(StartSize, GrowSize),
                new TokenLoopRunner(StartSize, GrowSize),
                new DefaultWhileRunner(StartSize, GrowSize),
                new TokenWhileRunner(StartSize, GrowSize));
        }
    }
}