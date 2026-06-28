namespace CatCode.PlayerLoops
{
    internal static class UniLoopRunner
    {
        
        public struct UniLoopRunnerInitializationBegin { }
        public struct UniLoopRunnerEarlyUpdateBegin { }
        public struct UniLoopRunnerFixedUpdateBegin { }
        public struct UniLoopRunnerPreUpdateBegin { }
        public struct UniLoopRunnerUpdateBegin { }
        public struct UniLoopRunnerPreLateUpdateBegin { }
        public struct UniLoopRunnerPostLateUpdateBegin { }

        public struct UniLoopRunnerInitializationEnd { }
        public struct UniLoopRunnerEarlyUpdateEnd { }
        public struct UniLoopRunnerFixedUpdateEnd { }
        public struct UniLoopRunnerPreUpdateEnd { }
        public struct UniLoopRunnerUpdateEnd { }
        public struct UniLoopRunnerPreLateUpdateEnd { }
        public struct UniLoopRunnerPostLateUpdateEnd { }

#if UNITY_2020_2_OR_NEWER
        public struct TimeUpdateBegin { }
        public struct TimeUpdateEnd { }
#endif
    }
}