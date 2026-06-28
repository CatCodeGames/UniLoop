namespace CatCode.PlayerLoops.Tests
{
    public sealed class LoopRunnerInfo
    {
        public LoopTestRunner Runner { get; }
        public int StartFrame { get; }
        public int StopFrame { get; set; }

        public LoopRunnerInfo(LoopTestRunner runner, int startFrame)
        {
            Runner = runner;
            StartFrame = startFrame;
            StopFrame = -1;
        }
    }
}