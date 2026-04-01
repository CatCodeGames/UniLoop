namespace CatCode.PlayerLoops
{
    internal static class UniLoopRunner
    {
        public struct InitializationBegin { }
        public struct EarlyUpdateBegin { }
        public struct FixedUpdateBegin { }
        public struct PreUpdateBegin { }
        public struct UpdateBegin { }
        public struct PreLateUpdateBegin { }
        public struct PostLateUpdateBegin { }

        public struct InitializationEnd { }
        public struct EarlyUpdateEnd { }
        public struct FixedUpdateEnd { }
        public struct PreUpdateEnd { }
        public struct UpdateEnd { }
        public struct PreLateUpdateEnd { }
        public struct PostLateUpdateEnd { }

#if UNITY_2020_2_OR_NEWER
        public struct TimeUpdateBegin { }
        public struct TimeUpdateEnd { }
#endif
    }
}