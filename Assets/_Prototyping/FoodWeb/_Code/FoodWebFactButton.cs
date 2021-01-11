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

        [NonSerialized] private BestiaryFactBase m_Fact = null;

        #endregion // InspectorF
<<<<<<< HEAD
        public void InitializeFW(BestiaryFactBase inFact, PlayerFactParams inParams)
=======
        public void InitializeFW(BFBase inFact, PlayerFactParams inParams, bool inbButtonMode)
>>>>>>> 66100018027bce35bb281b09dd38caa8afa16af8
        {
            m_Sentence.Populate(inFact, inParams);
            m_Fact = inFact;
            m_Button.onClick.AddListener(() => TargetOnClick());
        }
        public void TargetOnClick()
        {

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

