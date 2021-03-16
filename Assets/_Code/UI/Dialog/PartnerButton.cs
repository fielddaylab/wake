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

namespace Aqua
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
            m_Button.onClick.AddListener(OnButtonClicked);
        }

        #region Handlers

        private void OnButtonClicked()
        {
            m_ResponseRoutine.Replace(this, ExecuteSequence()).TryManuallyUpdate(0);
        }

        #endregion // Handlers

        #region Sequences

        private IEnumerator ExecuteSequence()
        {
            Services.Data.AddVariable(RequestCounter, 1);
            var sequence = Services.Script.TriggerResponse(GameTriggers.RequestPartnerHelp, "kevin");
            SetInputState(false);
            m_RootGroup.alpha = 0.75f;
            yield return sequence.Wait();
            SetInputState(IsShowing());
            m_RootGroup.alpha = 1;
        }

        #endregion // Sequences
    }
}