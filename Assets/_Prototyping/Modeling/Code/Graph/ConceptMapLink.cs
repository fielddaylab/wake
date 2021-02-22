using UnityEngine;
using BeauPools;
using UnityEngine.UI;
using TMPro;
using BeauRoutine;
using Aqua;

namespace ProtoAqua.Modeling
{
    public class ConceptMapLink : MonoBehaviour, IPooledObject<ConceptMapLink>
    {
        #region Inspector

        [SerializeField] private RectTransform m_LineTransform = null;
        [SerializeField] private Image m_Icon = null;
        [SerializeField] private TMP_Text m_Label = null;

        #endregion // Inspector

        public void Load(ConceptMapNode inStart, ConceptMapNode inEnd, in ConceptMapLinkData inData)
        {
            Vector2 startPos = ((RectTransform) inStart.transform).anchoredPosition;
            Vector2 endPos = ((RectTransform) inEnd.transform).anchoredPosition;
            Vector2 vector = endPos - startPos;

            float distance = vector.magnitude;
            Vector2 normalizedVector = vector.normalized;;

            RectTransform self = (RectTransform) transform;
            self.SetAnchorPos(startPos + vector * 0.5f);
            m_LineTransform.SetSizeDelta(distance - inStart.Radius() - inEnd.Radius(), Axis.X);
            m_LineTransform.SetRotation(Mathf.Atan2(vector.y, vector.x) * Mathf.Rad2Deg, Axis.Z, Space.Self);

            BFBase fact = inData.Tag as BFBase;
            m_Icon.sprite = fact?.Icon();
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
        }

        #endregion // IPooledObject
    }
}