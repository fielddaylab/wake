using BeauPools;
using BeauRoutine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Argumentation {
    public class EvidenceFactDisplay : MonoBehaviour, IPoolAllocHandler {
        public MonoBehaviour Display = null;
        
        [SerializeField] private RectTransform m_RootTransform = null;
        [SerializeField] private Behaviour[] m_InnerToDisable = null;
        [SerializeField] private GameObject[] m_InnerToDeactivate = null;
        [SerializeField] private TMP_Text[] m_LayoutTexts = null;
        [SerializeField] private GameObject[] m_EmptyClaim = null;
        [SerializeField] private FactSentenceDisplay m_Sentence = null;
        [SerializeField] private float m_EmptyHeight = 36;

        public void Clear() {
            foreach(var obj in m_InnerToDisable) {
                obj.enabled = false;
            }
            foreach(var go in m_InnerToDeactivate) {
                go.SetActive(false);
            }
            foreach(var go in m_EmptyClaim) {
                go.SetActive(true);
            }
            foreach(var text in m_LayoutTexts) {
                text.text = " ";
            }
            if (m_Sentence != null) {
                m_Sentence.Clear();
            }
            m_RootTransform.SetSizeDelta(m_EmptyHeight, Axis.Y);
        }

        public void Populate(BFBase inFact, BFDiscoveredFlags inFlags) {
            foreach(var go in m_InnerToDeactivate) {
                go.SetActive(true);
            }
            foreach(var obj in m_InnerToDisable) {
                obj.enabled = true;
            }
            foreach(var go in m_EmptyClaim) {
                go.SetActive(false);
            }

            FactPools.Populate(Display, inFact, inFlags, null);
        }

        void IPoolAllocHandler.OnAlloc() {
            Clear();
        }

        void IPoolAllocHandler.OnFree() {
            
        }
    }
}