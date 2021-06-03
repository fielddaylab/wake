using System.Collections.Generic;
using UnityEngine;

namespace Aqua
{
    public abstract class BFBehavior : BFBase
    {
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
        protected TextId SentenceFormat() { return !m_SentenceOverride.IsEmpty ? m_SentenceOverride : DefaultSentence(); }
        protected TextId FragmentFormat() { return !m_FragmentsOverride.IsEmpty ? m_FragmentsOverride : DefaultFragments(); }

        protected virtual TextId DefaultVerb() { return null; }
        protected virtual TextId DefaultSentence() { return null; }
        protected virtual TextId DefaultFragments() { return null; }

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
                sort = Id().CompareTo(other.Id());
            return sort;
        }
    }
}