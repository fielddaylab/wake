using System;
using UnityEngine;

namespace Aqua.Animation
{
    [Serializable]
    public struct SpriteFrame
    {
        #region Inspector

        public Sprite Sprite;

        [Header("Events")]
        public string AudioEvent;
        public string ExtraEvents;

        #endregion // Inspector

        static public readonly char[] EventSplitChars = new char[] { ';' };
    }
}