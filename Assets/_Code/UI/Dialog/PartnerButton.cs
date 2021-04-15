using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using BeauRoutine.Extensions;
using TMPro;
using System.Collections;
using System;
using BeauUtil.Tags;
using BeauUtil;
using BeauUtil.Variants;
using Aqua.Scripting;

namespace Aqua
{
    public class PartnerButton : BasePanel
    {
        static public readonly TableKeyPair RequestCounter = TableKeyPair.Parse("kevin:help.requests");

        #region Inspector

        [SerializeField] private Button m_Button = null;
        [SerializeField] private bool m_IsHint = false;

        #endregion // Inspector

        protected override void Awake()
        {
            m_Button.onClick.AddListener(OnButtonClicked);

            Services.Script.OnTargetedThreadStarted += OnTargetedThreadStart;
            Services.Script.OnTargetedThreadKilled += OnTargetedThreadEnd;

            OnTargetedThreadStart(Services.Script.GetTargetThread(GameConsts.Target_Kevin));
        }

        private void OnDestroy()
        {
            if (Services.Script)
            {
                Services.Script.OnTargetedThreadStarted -= OnTargetedThreadStart;
                Services.Script.OnTargetedThreadKilled -= OnTargetedThreadEnd;
            }
        }

        #region Handlers

        private void OnTargetedThreadStart(ScriptThreadHandle inHandle)
        {
            if (inHandle.TargetId() != GameConsts.Target_Kevin)
                return;

            m_RootGroup.alpha = 0.5f;
            SetInputState(false);
        }

        private void OnTargetedThreadEnd(StringHash32 inTarget)
        {
            if (inTarget != GameConsts.Target_Kevin)
                return;

            m_RootGroup.alpha = 1;
            SetInputState(IsShowing());
        }

        private void OnButtonClicked()
        {
            Services.Data.AddVariable(RequestCounter, 1);
            if (m_IsHint)
            {
                Services.Script.TriggerResponse(GameTriggers.RequestPartnerHelp, GameConsts.Target_Kevin);
            }
            else
            {
                Services.Script.TriggerResponse(GameTriggers.PartnerTalk, GameConsts.Target_Kevin);
            }
        }

        #endregion // Handlers
    }
}