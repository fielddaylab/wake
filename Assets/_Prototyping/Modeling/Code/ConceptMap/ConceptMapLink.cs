using Aqua;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.UI;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProtoAqua.Modeling
{
    public class ConceptMapLink : MonoBehaviour, IPooledObject<ConceptMapLink>
    {
        #region Inspector

        [SerializeField] private RectTransform m_LineTransform = null;
        [SerializeField] private Image m_Icon = null;
        [SerializeField] private LocText m_Label = null;
        [SerializeField] private TiledRawImage m_LineTiler = null;

        #endregion // Inspector

        private object m_Tag;

        public Action<object> OnClick;
        
        private void Awake()
        {
            PointerListener listener = m_Icon.EnsureComponent<PointerListener>();
            listener.onClick.AddListener(OnIconClick);
        }

        private void OnIconClick(PointerEventData ignored)
        {
            OnClick?.Invoke(m_Tag);
        }

        public void Load(ConceptMapNode inStart, ConceptMapNode inEnd, in ConceptMapLinkData inData, Texture2D inLineTexture)
        {
            m_Tag = inData.Tag;

            Vector2 startPos = ((RectTransform) inStart.transform).anchoredPosition;
            Vector2 endPos = ((RectTransform) inEnd.transform).anchoredPosition;
            Vector2 vector = endPos - startPos;

            float distance = vector.magnitude;
            Vector2 normalizedVector = vector.normalized;;

            RectTransform self = (RectTransform) transform;
            self.SetAnchorPos(startPos + vector * 0.5f);
            m_LineTransform.SetSizeDelta(distance - inStart.Radius() - inEnd.Radius(), Axis.X);
            m_LineTransform.SetRotation(Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg, Axis.Z, Space.Self);
            m_LineTiler.texture = inLineTexture;

            BFBase fact = inData.Tag as BFBase;
            m_Icon.sprite = fact?.GraphIcon();

            BFBehavior behavior = fact as BFBehavior;
            if (behavior != null)
            {
                if (m_Label)
                {
                    m_Label.SetText(behavior.Verb());
                }
            }
            else
            {
                if (m_Label)
                {
                    m_Label.SetText(null);
                }
            }
        }

        #region IPooledObject

        void IPooledObject<ConceptMapLink>.OnAlloc()
        {
        }

        void IPooledObject<ConceptMapLink>.OnConstruct(IPool<ConceptMapLink> inPool)
        {
        }

        void IPooledObject<ConceptMapLink>.OnDestruct()
        {
        }

        void IPooledObject<ConceptMapLink>.OnFree()
        {
            OnClick = null;
            m_Tag = null;
        }

        #endregion // IPooledObject
    }
}