using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Argumentation {
    public class TypeManager : MonoBehaviour {

        [SerializeField] LinkManager m_LinkManager = null;

        [SerializeField] private GameObject m_BehaviorButtons = null;
        [SerializeField] private GameObject m_EcosystemButtons = null;
        [SerializeField] private GameObject m_ModelButtons = null;

        [SerializeField] private Button m_OtterButton = null;
        [SerializeField] private Button m_UrchinButton = null;


        Dictionary<string,bool> typeList = new Dictionary<string, bool>();

        // Start is called before the first frame update
        void Start()
        {
            m_OtterButton.onClick.AddListener(() => ToggleType("otter"));
            m_UrchinButton.onClick.AddListener(() => ToggleType("urchin"));

        }

        // Update is called once per frame
        void Update()
        {
            
        }

        private void ToggleType(string type) {
            m_LinkManager.ToggleType(type);
        }

        public void ToggleButtons(string tab) {
            if(tab == "behavior") {
                m_BehaviorButtons.SetActive(true);
                m_EcosystemButtons.SetActive(false);
                m_ModelButtons.SetActive(false);
            } else if(tab == "ecosystem") {
                m_BehaviorButtons.SetActive(false);
                m_EcosystemButtons.SetActive(true);
                m_ModelButtons.SetActive(false); 
            } else if(tab == "model") {
                 m_BehaviorButtons.SetActive(false);
                m_EcosystemButtons.SetActive(false);
                m_ModelButtons.SetActive(true);
            }
        }

        public void SetupTagButtons(List<GameObject> responses) {
            foreach (GameObject gameObject in responses) 
            {
                string typeTag = gameObject.GetComponent<ChatBubble>().typeTag;
                bool value = false;
                if(!typeList.TryGetValue(typeTag, out value)) {
                     typeList.Add(typeTag, true);
                } 
            }

             foreach (KeyValuePair<string, bool> kvp in typeList)
                {
        
                    Debug.Log(kvp.Key);
                }
        }
    }
}