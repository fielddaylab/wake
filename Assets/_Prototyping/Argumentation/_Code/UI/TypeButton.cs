using System.Collections;
using System.Collections.Generic;
using BeauPools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Argumentation {
public class TypeButton : MonoBehaviour, IPoolAllocHandler {

    [SerializeField] Button m_button = null;
    [SerializeField] TextMeshProUGUI m_TextMesh = null;
    [SerializeField] Image m_spriteImage = null;



    //private TypeManager typeManager = null;
    
        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        public void InitalizeButton(string inId, string inDisplayText, Sprite inSprite, TypeManager inTypeManager) {
            Debug.Log("Making button");
            m_TextMesh.text = inDisplayText;
            m_spriteImage.sprite = inSprite;
            m_button.onClick.AddListener(() => inTypeManager.ToggleType(inId));
        }

        public void OnAlloc()
        {
           
        }

        public void OnFree()
        {
           m_button.onClick.RemoveAllListeners();
        }
    }
}