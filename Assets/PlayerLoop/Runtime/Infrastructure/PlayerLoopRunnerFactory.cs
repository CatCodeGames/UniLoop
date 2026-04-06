namespace CatCode.PlayerLoops
{
    /// <summary>
    /// Factory for creating a PlayerLoopRunner with default runner configurations.
    /// </summary>
    internal static class PlayerLoopRunnerFactory
    {
        public const int Capacity = 64;

        public static PlayerLoopRunner Create()
        {
            return new PlayerLoopRunner(
                new SlimLoopRunner(Capacity),
                new DefaultLoopRunner(Capacity),
                new TokenLoopRunner(Capacity),
                new DefaultWhileRunner(Capacity),
                new TokenWhileRunner(Capacity));
        }
    }
}