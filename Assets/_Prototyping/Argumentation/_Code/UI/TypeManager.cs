using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using BeauPools;

namespace ProtoAqua.Argumentation
{
    public class TypeManager : MonoBehaviour
    {

        [Serializable]
        public class TypeDefinition : IKeyValuePair<StringHash32, TypeDefinition>
        {
            public string type;
            public string DisplayLabel;
            public Sprite Icon;

            StringHash32 IKeyValuePair<StringHash32, TypeDefinition>.Key { get { return type; } }

            TypeDefinition IKeyValuePair<StringHash32, TypeDefinition>.Value { get { return this; } }
        }

        [Serializable]
        public class TypePool : SerializablePool<TypeButton> { }

        [SerializeField] LinkManager m_LinkManager = null;

        [SerializeField] private GameObject m_BehaviorButtons = null;
        [SerializeField] private GameObject m_EcosystemButtons = null;
        [SerializeField] private GameObject m_ModelButtons = null;
        [SerializeField] private TypePool m_TypePool = null;
        [SerializeField] private TypeDefinition[] m_TypeButtons = null;



        Dictionary<string, List<String>> typeList = new Dictionary<string, List<String>>();

        // Start is called before the first frame update
        void Start()
        {
            m_TypePool.Initialize();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void ToggleType(string type)
        {
            m_LinkManager.ToggleType(type);
        }

        public void ToggleButtons(string tab)
        {
            m_TypePool.Reset();
            if (tab == "behavior")
            {
                m_BehaviorButtons.SetActive(true);
                m_EcosystemButtons.SetActive(false);
                m_ModelButtons.SetActive(false);
                InitializePoolButtons(m_BehaviorButtons, "behavior");
            }
            else if (tab == "ecosystem")
            {
                m_BehaviorButtons.SetActive(false);
                m_EcosystemButtons.SetActive(true);
                m_ModelButtons.SetActive(false);
                InitializePoolButtons(m_EcosystemButtons, "ecosystem");
            }
            else if (tab == "model")
            {
                m_BehaviorButtons.SetActive(false);
                m_EcosystemButtons.SetActive(false);
                m_ModelButtons.SetActive(true);
                InitializePoolButtons(m_ModelButtons, "model");
            }
        }

        private void InitializePoolButtons(GameObject container, string inType)
        {
            Boolean setFirstType = false; //Used to "click" the first type that will appear at the top. This is to load something when switching tabs
            List<String> value;
            if (typeList.TryGetValue(inType, out value))
            {
                foreach (String val in value)
                {
                    if (!setFirstType)
                    {
                        ToggleType(val);
                        setFirstType = true;
                    }

                    TypeDefinition newType;
                    StringHash32 typeHash = new StringHash32(val);
                    if (m_TypeButtons.TryGetValue(typeHash, out newType))
                    {
                        TypeButton newButton = m_TypePool.Alloc(container.transform);
                        newButton.InitalizeButton(val, newType.DisplayLabel, newType.Icon, this);
                    }
                }
            } else {
                   ToggleType("otter");
            }



        }


        public void SetupTagButtons(List<GameObject> responses)
        {

            foreach (GameObject gameObject in responses)
            {
                string tag = gameObject.GetComponent<ChatBubble>().linkTag;
                string typeTag = gameObject.GetComponent<ChatBubble>().typeTag;
                List<String> value;
                if (!typeList.TryGetValue(tag, out value))
                {
                    value = new List<String>();
                    value.Add(typeTag);
                    typeList.Add(tag, value);
                }
                else if (!value.Contains(typeTag))
                {
                    value.Add(typeTag);
                    typeList[tag] = value;
                }
            }

        }
    }
}