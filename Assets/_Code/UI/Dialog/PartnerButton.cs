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
        static public readonly TableKeyPair RequestCounter = TableKeyPair.Parse("guide:help.requests");

        #region Inspector

        [SerializeField] private Button m_Button = null;

        #endregion // Inspector

        protected override void Awake()
        {
            m_Button.onClick.AddListener(OnButtonClicked);

            Services.Script.OnTargetedThreadStarted += OnTargetedThreadStart;
            Services.Script.OnTargetedThreadKilled += OnTargetedThreadEnd;

            OnTargetedThreadStart(Services.Script.GetTargetThread(GameConsts.Target_V1ctor));
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
            if (inHandle.TargetId() != GameConsts.Target_V1ctor)
                return;

            m_RootGroup.alpha = 0.5f;
            SetInputState(false);
        }

        private void OnTargetedThreadEnd(StringHash32 inTarget)
        {
            if (inTarget != GameConsts.Target_V1ctor)
                return;

            m_RootGroup.alpha = 1;
            SetInputState(IsShowing());
        }

        private void OnButtonClicked()
        {
            Services.Data.AddVariable(RequestCounter, 1);
            Services.Script.TriggerResponse(GameTriggers.RequestPartnerHelp, GameConsts.Target_V1ctor);
        }

        #endregion // Handlers
    }
}