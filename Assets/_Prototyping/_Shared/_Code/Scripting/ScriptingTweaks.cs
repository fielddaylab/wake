using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Blocks;
using UnityEngine;

namespace ProtoAqua
{
    [CreateAssetMenu(menuName = "Prototype/Scripting Tweaks")]
    public class ScriptingTweaks : TweakAsset
    {
        #region Inspector

        [SerializeField] private float m_CutsceneEndNextTriggerDelay = 0.2f;

        #endregion // Inspector

        public float CutsceneEndNextTriggerDelay() { return m_CutsceneEndNextTriggerDelay; }
    }
}