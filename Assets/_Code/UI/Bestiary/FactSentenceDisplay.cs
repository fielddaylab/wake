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

        public void Populate(BFBehavior inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference)
        {
            if (!m_Tweaks)
            {
                m_Tweaks = Services.Tweaks.Get<FactSentenceTweaks>();
            }

            Clear();

            foreach(var fragment in BFType.GenerateFragments(inFact, inFlags, inReference))
            {
                TryAllocFragment(fragment, (inFlags & BFDiscoveredFlags.IsEncrypted) != 0);
            }

            m_Layout.ForceRebuild();
        }

        private bool TryAllocFragment(in BFFragment inFragment, bool encrypt)
        {
            StringSlice str = inFragment.String;
            if (encrypt) {
                str = Formatting.Scramble(str);
            }

            FactSentenceFragment fragment = m_Tweaks.Alloc(inFragment, m_Layout.transform);
            fragment.Configure(str);
            m_AllocatedFragments.Add(fragment);

            return true;
        }
    }
}