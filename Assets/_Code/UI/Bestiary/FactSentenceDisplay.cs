using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using BeauPools;

namespace Aqua
{
    public class FactSentenceDisplay : MonoBehaviour
    {
        [Serializable]
        public class Pool : SerializablePool<FactSentenceDisplay> { }

        #region Inspector

        [SerializeField] private LayoutGroup m_Layout = null;
        // [SerializeField] private RectMask2D m_Mask = null;

        #endregion // Inspector

        [NonSerialized] private List<FactSentenceFragment> m_AllocatedFragments = new List<FactSentenceFragment>();
        [NonSerialized] private FactSentenceTweaks m_Tweaks = null;

        public void Clear()
        {
            foreach(var frag in m_AllocatedFragments)
            {
                frag.Recycle();
            }

            m_AllocatedFragments.Clear();
        }

        public void Populate(BFBehavior inFact, BestiaryDesc inReference, BFDiscoveredFlags inFlags)
        {
            if (!m_Tweaks)
            {
                m_Tweaks = Services.Tweaks.Get<FactSentenceTweaks>();
            }

            Clear();

            foreach(var fragment in BFType.GenerateFragments(inFact, inReference, inFlags))
            {
                TryAllocFragment(fragment);
            }

            m_Layout.ForceRebuild();
        }

        private bool TryAllocFragment(in BFFragment inFragment)
        {
            StringSlice str = inFragment.String;

            FactSentenceFragment fragment = m_Tweaks.Alloc(inFragment, m_Layout.transform);
            fragment.Configure(inFragment.String);
            m_AllocatedFragments.Add(fragment);

            return true;
        }
    }
}