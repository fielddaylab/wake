using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using BeauPools;
using BeauRoutine;

namespace Aqua
{
    public class BehaviorFactDisplay : MonoBehaviour, IPoolAllocHandler
    {
        #region Inspector

        [SerializeField, Required] private Image m_Icon = null;
        [SerializeField, Required] private Graphic m_Background = null;
        [SerializeField, Required] private FactSentenceDisplay m_Sentence = null;
        [SerializeField, Required] private RectTransform m_StressedBadge = null;
        [SerializeField, Required] private RectTransform m_Layout = null;

        #endregion // Inspector

        public void Populate(BFBehavior inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference)
        {
            Sprite icon = inFact.Icon;
            if (BFType.IsBorrowed(inFact, inReference)) {
                icon = Services.Assets.Bestiary.DefaultBorrowedIcon(inFact);
            }

            m_Icon.sprite = icon;
            m_Icon.gameObject.SetActive(icon);
            m_Background.color = Services.Assets.Bestiary.BehaviorColor(inFact, inFlags);
            
            m_Sentence.Populate(inFact, inFlags, inReference);

            if (inFact.OnlyWhenStressed) {
                m_StressedBadge.gameObject.SetActive(true);
                m_Layout.SetSizeDelta(52, Axis.Y);
            } else {
                m_StressedBadge.gameObject.SetActive(false); 
                m_Layout.SetSizeDelta(36, Axis.Y);
            }
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