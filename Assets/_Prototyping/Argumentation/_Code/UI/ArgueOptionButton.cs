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
    public class ArgueOptionButton : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private LayoutGroup m_Layout = null;
        [SerializeField] private LocText m_Text = null;
        [SerializeField] private Button m_Button = null;

        #endregion // Inspector

        [NonSerialized] private StringHash32 m_Id;

        private void Awake()
        {
            m_Button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            Services.Events.Dispatch(ArgueActivity.Event_SelectClaim, m_Id);
        }

        #region Commands

        public void Populate(string inText, StringHash32 inId)
        {
            m_Text.SetText(inText);
            m_Id = inId;

            m_Layout.ForceRebuild();
        }

        #endregion // Commands
    }
}
