using System;
using UnityEngine;
using BeauRoutine;
using BeauUtil;
using UnityEngine.UI;
using TMPro;
using Aqua;

namespace ProtoAqua.Foodweb
{
    public class FoodWebFactButton : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private Button m_Button = null;
        [SerializeField] private FactSentenceDisplay m_Sentence = null;

        #endregion // InspectorF
        public void InitializeFW(BFBase inFact, PlayerFactParams inParams)
        {
            m_Sentence.Populate(inFact, inParams);
            m_Button.onClick.AddListener(() => TargetOnClick());
        }
        public void TargetOnClick() {

        }

        public void OnFree()
        {
           m_Button.onClick.RemoveAllListeners();
        }

        public bool has_Sentence()
        {
            if (m_Sentence != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

