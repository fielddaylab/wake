using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using BeauUtil.Variants;
using BeauPools;

namespace Aqua
{
    public class FactSentenceDisplay : MonoBehaviour
    {
        [Serializable]
        public class Pool : SerializablePool<FactSentenceDisplay> { }

        #region Inspector

        [SerializeField] private LayoutGroup m_Layout = null;

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

        public void Populate(BFBehavior inFact)
        {
            if (!m_Tweaks)
            {
                m_Tweaks = Services.Tweaks.Get<FactSentenceTweaks>();
            }

            Clear();

            foreach(var fragment in inFact.GenerateFragments())
            {
                TryAllocFragment(fragment);
            }

            m_Layout.ForceRebuild();
        }

        private bool TryAllocFragment(in BestiaryFactFragment inFragment)
        {
            StringSlice str = inFragment.String;

            FactSentenceFragment fragment = m_Tweaks.Alloc(inFragment, m_Layout.transform);
            fragment.Configure(inFragment.String);
            m_AllocatedFragments.Add(fragment);

            return true;
        }
    }
}