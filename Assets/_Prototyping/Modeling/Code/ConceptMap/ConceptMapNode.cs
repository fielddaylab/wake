using UnityEngine;
using BeauPools;
using UnityEngine.UI;
using Aqua;
using System;
using BeauUtil;
using BeauUtil.UI;
using UnityEngine.EventSystems;

namespace ProtoAqua.Modeling
{
    public class ConceptMapNode : MonoBehaviour, IPooledObject<ConceptMapNode>
    {
        #region Inspector

        [SerializeField] private Image m_ClickZone = null;
        [SerializeField] private Image m_Glow = null;
        [SerializeField] private Image m_Icon = null;
        [SerializeField] private LocText m_Label = null;
        [SerializeField] private float m_Radius = 64;

        #endregion // Inspector

        private object m_Tag;

        public Action<object> OnClick;

        public float Radius() { return m_Radius; }

        #region Handlers

        private void Awake()
        {
            m_ClickZone.EnsureComponent<PointerListener>().onClick.AddListener(HandleOnClick);
        }

        private void HandleOnClick(PointerEventData unused)
        {
            OnClick?.Invoke(m_Tag);
        }

        #endregion // Handlers

        public void Load(Vector2 inPosition, in ConceptMapNodeData inData)
        {
            m_Tag = inData.Tag;
            
            ((RectTransform) transform).anchoredPosition = inPosition;

            BestiaryDesc bestiary = inData.Tag as BestiaryDesc;
            if (bestiary != null)
            {
                m_Icon.sprite = bestiary.Icon();
                if (m_Label)
                {
                    m_Label.SetText(bestiary.CommonName());
                }
            }
            else
            {
                WaterPropertyDesc prop = inData.Tag as WaterPropertyDesc;
                if (prop != null)
                {
                    m_Icon.sprite = prop.Icon();
                    if (m_Label)
                    {
                        m_Label.SetText(prop.LabelId());
                    }
                }
            }
        }

        public void SetGlowing(bool inbGlowing)
        {
            m_Glow.gameObject.SetActive(inbGlowing);
        }

        #region IPooledObject

        void IPooledObject<ConceptMapNode>.OnAlloc()
        {
        }

        void IPooledObject<ConceptMapNode>.OnConstruct(IPool<ConceptMapNode> inPool)
        {
        }

        void IPooledObject<ConceptMapNode>.OnDestruct()
        {
        }

        void IPooledObject<ConceptMapNode>.OnFree()
        {
            SetGlowing(false);
            m_Tag = null;
        }

        #endregion // IPooledObject
    }
}