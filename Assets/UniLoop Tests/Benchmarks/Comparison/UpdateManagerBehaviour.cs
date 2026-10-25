using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatCode.PlayerLoops.Benchmarks
{
    public sealed class UpdateManagerBehaviour : MonoBehaviour
    {
        public readonly List<Action> Actions = new();

        private void Update()
        {
            for (int i = 0; i < Actions.Count; i++)
                Actions[i]();
        }
    }
}
