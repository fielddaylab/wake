using System;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public class SceneBackButton : MonoBehaviour
    {
        [SerializeField, Required] private Button m_Button = null;

        private void Awake()
        {
            m_Button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            StateUtil.LoadPreviousSceneWithWipe();
        }
    }
}