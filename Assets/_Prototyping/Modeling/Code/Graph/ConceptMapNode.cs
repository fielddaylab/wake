using UnityEngine;
using BeauPools;
using UnityEngine.UI;
using TMPro;
using Aqua;

namespace ProtoAqua.Modeling
{
    public class ConceptMapNode : MonoBehaviour, IPooledObject<ConceptMapNode>
    {
        #region Inspector

        [SerializeField] private Image m_Icon = null;
        [SerializeField] private TMP_Text m_Label = null;
        [SerializeField] private float m_Radius = 64;

        #endregion // Inspector

        public float Radius() { return m_Radius; }

        public void Load(Vector2 inPosition, in ConceptMapNodeData inData)
        {
            ((RectTransform) transform).anchoredPosition = inPosition;

            BestiaryDesc bestiary = inData.Tag as BestiaryDesc;
            if (bestiary != null)
            {
                m_Icon.sprite = bestiary.Icon();
            }
            else
            {
                WaterPropertyDesc prop = inData.Tag as WaterPropertyDesc;
                if (prop != null)
                {
                    m_Icon.sprite = prop.Icon();
                }
            }
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
        }

        #endregion // IPooledObject
    }
}