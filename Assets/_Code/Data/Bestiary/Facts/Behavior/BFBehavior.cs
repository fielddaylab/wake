using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using ScriptableBake;
using UnityEngine;
using UnityEngine.Serialization;

namespace Aqua
{
    public abstract class BFBehavior : BFBase, IBaked
    {
        #region Consts

        static protected readonly TextId DetailsHeader = "fact.behavior.header";

        #endregion // Consts

        #region Inspector

        [Header("Behavior")]
        [ShowIfField("DisplayStressed")] public bool OnlyWhenStressed = false;
        [HideInEditor] public StringHash32 PairId = null;
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

        int IBaked.Order { get { return 15; } }

        public abstract bool Bake(BakeFlags flags, BakeContext context);

        protected T FindPairedFact<T>() where T : BFBehavior
        {
            foreach(var behavior in Parent.OwnedFacts)
            {
                if (behavior == this)
                    continue;

                T asT = behavior as T;
                if (asT == null)
                    continue;

                if (IsPair(asT))
                    return asT;
            }

            return null;
        }

        protected virtual bool IsPair(BFBehavior inOther)
        {
            return inOther.Type == Type;
        }

        private bool DisplayStressed()
        {
            return Type != BFTypeId.Parasite;
        }

        #endif // UNITY_EDITOR
    }
}