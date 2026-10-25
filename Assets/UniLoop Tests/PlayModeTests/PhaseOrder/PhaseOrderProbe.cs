using System.Collections.Generic;
using UnityEngine;

namespace CatCode.PlayerLoops.Tests
{
    public sealed class PhaseOrderProbe : MonoBehaviour
    {
        public const string Mark = "MonoBehaviour";

        private PlayerLoopTiming _timing;
        private List<string> _log;

        public void Init(PlayerLoopTiming timing, List<string> log)
        {
            _timing = timing;
            _log = log;
        }

        private void FixedUpdate() => Record(PlayerLoopTiming.FixedUpdate);
        private void Update() => Record(PlayerLoopTiming.Update);
        private void LateUpdate() => Record(PlayerLoopTiming.LateUpdate);

        private void Record(PlayerLoopTiming timing)
        {
            if (_log != null && _timing == timing)
                _log.Add(Mark);
        }
    }
}
