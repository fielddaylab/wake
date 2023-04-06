using System.Collections.Generic;
using UnityEngine;

namespace Aqua
{
    public class ManualResourceUnload : MonoBehaviour {
        public UnityEngine.Object[] Resources;

        private void OnDestroy() {
            UnloadAll();
        }

        private void UnloadAll() {
            Assets.FullyUnload(ref Resources);
        }

    }
}