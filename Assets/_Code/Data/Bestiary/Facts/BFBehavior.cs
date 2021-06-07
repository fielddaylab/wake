using System.Collections.Generic;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    public abstract class BFBehavior : BFBase
    {
        #region Consts

        static private readonly TextId[] s_QualitativeWords = new TextId[]
        {
            null, "words.less", "words.fewer", "words.more", "words.slower", "words.faster"
        };
        static private readonly TextId[] s_QualitativeWordsLower = new TextId[]
        {
            null, "words.less.lower", "words.fewer.lower", "words.more.lower", "words.slower.lower", "words.faster.lower"
        };

        public enum QualCompare : byte
        {
            Null,

            Less,
            Fewer,
            More,

            Slower,
            Faster,
        }

        static protected QualCompare MapDescriptor(float inDifference, QualCompare inLess, QualCompare inMore)
        {
            return inDifference > 0 ? inMore : inLess;
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
        [SerializeField] private bool m_Stressed = false;
        [SerializeField] protected bool m_AutoGive = false;

        [Header("Text")]
        [SerializeField] private TextId m_VerbOverride = null;
        [SerializeField] private TextId m_SentenceOverride = null;
        [SerializeField] private TextId m_FragmentsOverride = null;

        #endregion // Inspector

        public bool OnlyWhenStressed() { return m_Stressed; }
        public override BFMode Mode()
        {
            return m_AutoGive ? BFMode.Always : base.Mode();
        }

        #region Text

        public TextId Verb() { return !m_VerbOverride.IsEmpty ? m_VerbOverride : DefaultVerb(); }

        protected TextId SentenceOverride() { return m_SentenceOverride; }
        protected TextId FragmentsOverride() { return m_FragmentsOverride; }

        protected virtual TextId DefaultVerb() { return null; }

        #endregion // Text

        protected T FindPairedFact<T>() where T : BFBehavior
        {
            foreach(var behavior in Parent().FactsOfType<T>())
            {
                if (behavior == this)
                    continue;

                if (IsPair(behavior))
                    return behavior;
            }

            return null;
        }

        protected virtual bool IsPair(BFBehavior inOther)
        {
            return inOther.GetType() == GetType();
        }

        public abstract IEnumerable<BFFragment> GenerateFragments();

        public override int CompareTo(BFBase other)
        {
            int sort = GetSortingOrder().CompareTo(other.GetSortingOrder());
            if (sort == 0)
            {
                BFBehavior behavior = other as BFBehavior;
                if (behavior != null && IsPair(behavior))
                {
                    if (m_Stressed)
                        return 1;
                    return -1;
                }
                sort = Id().CompareTo(other.Id());
            }
            return sort;
        }
    }
}