using System;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua
{
    public class SceneButton : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private string m_TargetScene = null;
        [SerializeField] private Button m_Button = null;
        [SerializeField] private TMP_Text m_Label = null;

        #endregion // Inspector

        [NonSerialized] private SceneBinding m_CurrentScene;

        public void Initialize(SceneBinding inScene)
        {
            m_CurrentScene = inScene;
            if (!inScene.IsValid())
            {
                m_Button.interactable = false;
                m_Label.SetText("No Scene");
            }
            else
            {
                m_Button.interactable = true;
                m_Label.SetText(inScene.Name);
            }
        }

        private void Awake()
        {
            if (!m_CurrentScene.IsValid())
            {
                Initialize(SceneHelper.FindSceneByName(m_TargetScene, SceneCategories.Build));
            }
            m_Button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            Services.State.LoadScene(m_CurrentScene);
        }
    }
}