using System.Collections.Generic;
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

        static private readonly TextId[] s_QualitativeWords = new TextId[]
        {
            default, "words.less", "words.fewer", "words.more", "words.slower", "words.faster", "words.sameAmount", "words.sameRate"
        };
        static private readonly TextId[] s_QualitativeWordsLower = new TextId[]
        {
            default, "words.less.lower", "words.fewer.lower", "words.more.lower", "words.slower.lower", "words.faster.lower", "words.sameAmount.lower", "words.sameRate.lower"
        };

        public enum QualCompare : byte
        {
            Null,

            Less,
            Fewer,
            More,

            Slower,
            Faster,

            SameAmount,
            SameRate
        }

        static protected QualCompare MapDescriptor(float inDifference, QualCompare inLess, QualCompare inMore, QualCompare inEquals)
        {
            return Mathf.Approximately(inDifference, 0) ? inEquals : (inDifference > 0 ? inMore : inLess);
        }

        static public TextId QualitativeId(QualCompare inDescriptor)
        {
            return s_QualitativeWords[(int) inDescriptor];
        }

        static public TextId QualitativeLowerId(QualCompare inDescriptor)
        {
            return s_QualitativeWordsLower[(int) inDescriptor];
        }

        #endregion // Consts

        #region Inspector

        [Header("Behavior")]
        public bool OnlyWhenStressed = false;
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

        public abstract bool Bake(BakeFlags flags);

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

        #endif // UNITY_EDITOR
    }
}