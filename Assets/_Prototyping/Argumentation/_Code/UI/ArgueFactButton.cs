using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using Aqua;
using System;
using BeauPools;

namespace ProtoAqua.Argumentation
{
    public class ArgueFactButton : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private Button m_Button = null;
        [SerializeField] private BestiaryDescCategory m_Category = BestiaryDescCategory.Critter;

        #endregion // Inspector

        private void Awake()
        {
            m_Button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            Services.Events.Dispatch(ArgueActivity.Event_OpenFactSelect, m_Category);
        }
    }
}
