using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Aqua;
using TMPro;
using BeauUtil;

namespace ProtoAqua.Foodweb 
{

    public class PanelSetup : MonoBehaviour
    {

        #region Inspector

        [SerializeField] private ToggleGroup m_FactGroup = null;
        [SerializeField] private GameObject Panel = null;

        #endregion

        [NonSerialized] private FoodWebFactButton[] m_FactButtons;
        [NonSerialized] private List<BFBase> m_Facts;

        String debug_str = "[PanelSetup Debug Log] ------ ";

        private void Awake()
        {
            m_Facts = new List<BFBase>();


            foreach (BestiaryDesc critter in Services.Assets.Bestiary.AllEntriesForCategory(BestiaryDescCategory.Critter))
            {
                foreach (BFBase facts in critter.Facts)
                {
                    Debug.Log(debug_str + facts.ToString());
                    m_Facts.Add(facts);
                }
            }
            try {
                m_FactButtons = m_FactGroup.GetComponentsInChildren<FoodWebFactButton>();
            }
            catch (Exception e) {
                throw new Exception("Unable to retrieve children", e);
            }

            Debug.Log(debug_str + "TESTTTTTT");
            Debug.Log(debug_str + m_Facts.Count);

            if (m_FactButtons.Length < m_Facts.Count)
            {
                throw new Exception("fact buttons are less than facts.");
            }

            Vector3 scaleChange = new Vector3(0.47f, 0.31f, 1f);

            for (int i = 0; i < Math.Min(m_FactButtons.Length, m_Facts.Count); i++)
            {
                m_FactButtons[i].InitializeFW(m_Facts[i], null);
                m_FactButtons[i].gameObject.transform.localScale = scaleChange; //this is very bad

            }

            for (int i = m_Facts.Count; i < m_FactButtons.Length; i++)
            {
                m_FactButtons[i].gameObject.SetActive(false);
            }


        }
    }

}