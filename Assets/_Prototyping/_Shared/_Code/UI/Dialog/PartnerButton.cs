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

namespace ProtoAqua
{
    public class PartnerButton : BasePanel
    {
        static public readonly TableKeyPair RequestCounter = TableKeyPair.Parse("kevin:help.requests");

        #region Inspector

        [SerializeField] private Button m_Button = null;

        #endregion // Inspector

        [NonSerialized] private Routine m_ResponseRoutine;

        protected override void Awake()
        {
            Services.Events.Register(GameEvents.CutsceneStart, OnCutsceneStart)
                .Register(GameEvents.CutsceneEnd, OnCutsceneEnd);
            
            m_Button.onClick.AddListener(OnButtonClicked);
        }

        private void OnDestroy()
        {
            if (Services.Events)
            {
                Services.Events.Deregister(GameEvents.CutsceneStart, OnCutsceneStart)
                    .Deregister(GameEvents.CutsceneEnd, OnCutsceneEnd);
            }
        }

        #region Handlers

        private void OnButtonClicked()
        {
            m_ResponseRoutine.Replace(this, ExecuteSequence());
        }

        private void OnCutsceneStart()
        {
            Hide();
        }

        private void OnCutsceneEnd()
        {
            Show();
        }

        #endregion // Handlers

        #region Sequences

        private IEnumerator ExecuteSequence()
        {
            Services.Data.AddVariable(RequestCounter, 1);
            var sequence = Services.Script.TriggerResponse(GameTriggers.RequestPartnerHelp);
            SetInputState(false);
            m_RootGroup.alpha = 0.5f;
            yield return sequence.Routine();
            SetInputState(IsShowing());
            m_RootGroup.alpha = 1;
        }

        #endregion // Sequences
    }
}