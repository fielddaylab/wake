using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using BeauPools;

namespace Aqua
{
    public class BehaviorFactDisplay : MonoBehaviour, IPoolAllocHandler
    {
        #region Inspector

        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField, Required] private FactSentenceDisplay m_Sentence = null;
        [SerializeField, Required] private RectTransform m_StressedBadge = null;

        #endregion // Inspector

        public void Populate(BFBehavior inFact)
        {
            m_Icon.sprite = inFact.Icon();
            m_Icon.gameObject.SetActive(inFact.Icon());
            m_Sentence.Populate(inFact);

            m_StressedBadge.gameObject.SetActive(inFact.OnlyWhenStressed());
        }

        void IPoolAllocHandler.OnAlloc()
        {
        }

        void IPoolAllocHandler.OnFree()
        {
            m_Sentence.Clear();
        }
    }
}