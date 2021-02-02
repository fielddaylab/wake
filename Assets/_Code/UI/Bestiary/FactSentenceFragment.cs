using UnityEngine;
using BeauPools;
using TMPro;
using BeauUtil;
using System;

namespace Aqua
{
    public sealed class FactSentenceFragment : MonoBehaviour, IPooledObject<FactSentenceFragment>
    {
        #region Inspector

        [SerializeField, Required] private LocText m_Text = null;
        [SerializeField, Required] private ColorGroup m_Background = null;

        #endregion // Inspector

        [NonSerialized] private IPool<FactSentenceFragment> m_Pool;
        [NonSerialized] private bool m_Allocated;

        public void PreConfigure(Color inBackgroundColor, Color inTextColor)
        {
            m_Background.Color = inBackgroundColor;
            m_Text.Graphic.color = inTextColor;
        }

        public void Configure(StringSlice inText)
        {
            m_Text.SetText(inText.ToString());
        }
        
        public void Recycle()
        {
            if (m_Allocated)
            {
                m_Pool.Free(this);
            }
        }

        #region IPooledObject

        void IPooledObject<FactSentenceFragment>.OnAlloc()
        {
            m_Allocated = true;
        }

        void IPooledObject<FactSentenceFragment>.OnConstruct(IPool<FactSentenceFragment> inPool)
        {
            m_Pool = inPool;
        }

        void IPooledObject<FactSentenceFragment>.OnDestruct()
        {
        }

        void IPooledObject<FactSentenceFragment>.OnFree()
        {
            m_Allocated = false;
        }

        #endregion // IPooledObject
    }
}