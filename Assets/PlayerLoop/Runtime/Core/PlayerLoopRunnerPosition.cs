namespace CatCode.PlayerLoops
{
    public readonly struct PlayerLoopRunnerPosition
    {
        public readonly PlayerLoopTiming Timing;
        public readonly PlayerLoopPhase Phase;

        public PlayerLoopRunnerPosition(PlayerLoopTiming timing, PlayerLoopPhase position)
        {
            Timing = timing;
            Phase = position;
        }
    }
}