using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using ScriptableBake;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    public abstract class BFBehavior : BFBase
    {
        #region Consts

        static protected readonly TextId DetailsHeader = "fact.behavior.header";

        #endregion // Consts

        #region Inspector

        [Header("Behavior")]
        [ShowIfField("DisplayStressed")] public bool OnlyWhenStressed = false;
        internal bool AutoGive = false;

        #endregion // Inspector

        protected BFBehavior(BFTypeId inType) : base(inType) { }

        static protected int CompareStressedPair(BFBase x, BFBase y)
        {
            BFBehavior bx = (BFBehavior) x, by = (BFBehavior) y;
            if (bx.OnlyWhenStressed)
                return 1;
            if (by.OnlyWhenStressed)
                return -1;
            return 0;
        }

        #if UNITY_EDITOR

        // int IBaked.Order => 15;

        // bool IBaked.Bake(BakeFlags flags, BakeContext context) {
        //     return true;
        // }

        private bool DisplayStressed()
        {
            return Type != BFTypeId.Parasite;
        }

        #endif // UNITY_EDITOR
    }
}