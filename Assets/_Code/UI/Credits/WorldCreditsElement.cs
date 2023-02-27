using System;
using System.Collections;
using Aqua.Character;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil.Debugger;
using EasyAssetStreaming;
using BeauPools;

namespace Aqua {
    public class WorldCreditsElement : MonoBehaviour {
        public ColorGroup Fade;
        public float FadeInOffset = 1;
        public float MoveTime = 2;
        public float LingerTime = 2;

        public Routine FadeRoutine;

        #if UNITY_EDITOR

        private void Reset() {
            Fade = GetComponent<ColorGroup>();
        }

        #endif // UNITY_EDITOR
    }
}