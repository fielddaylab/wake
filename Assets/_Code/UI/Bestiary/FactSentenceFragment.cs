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
        [SerializeField, Required] private CanvasGroup m_InputGroup = null;

        #endregion // Inspector

        [NonSerialized] private IPool<FactSentenceFragment> m_Pool;

        public void PreConfigure(Color inBackgroundColor, Color inTextColor, bool inbInteractive)
        {
            m_InputGroup.blocksRaycasts = inbInteractive;
            m_Background.Color = inBackgroundColor;
            m_Text.Graphic.color = inTextColor;
        }

        public void Configure(StringSlice inText)
        {
            m_Text.SetText(inText.ToString());
        }
        
        public void Recycle()
        {
            m_Pool.Free(this);
        }

        #region IPooledObject

        void IPooledObject<FactSentenceFragment>.OnAlloc()
        {
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
        }

        #endregion // IPooledObject
    }
}