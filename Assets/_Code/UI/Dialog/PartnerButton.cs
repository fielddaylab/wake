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
        [SerializeField] private Transform m_Eyes = null;
        [SerializeField] private float m_OffscreenX = 80;
        [SerializeField] private TweenSettings m_ShowHideAnim = new TweenSettings(0.2f, Curve.Smooth);

        #endregion // Inspector

        [NonSerialized] private float m_OnscreenX;

        protected override void Awake() {
            m_Button.onClick.AddListener(OnButtonClicked);

            m_OnscreenX = Root.anchoredPosition.x;

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

        #region Animations

        protected override void InstantTransitionToHide() {
            Root.gameObject.SetActive(false);
            Root.SetAnchorPos(m_OffscreenX, Axis.X);
        }

        protected override void InstantTransitionToShow() {
            Root.gameObject.SetActive(true);
            Root.SetAnchorPos(m_OnscreenX, Axis.X);
            m_Eyes.SetScale(1);
        }

        protected override IEnumerator TransitionToHide() {
            yield return Root.AnchorPosTo(m_OffscreenX, m_ShowHideAnim, Axis.X);
            Root.gameObject.SetActive(false);
        }

        protected override IEnumerator TransitionToShow() {
            Root.gameObject.SetActive(true);
            m_Eyes.SetScale(new Vector3(0.8f, 0, 1));
            yield return Root.AnchorPosTo(m_OnscreenX, m_ShowHideAnim, Axis.X);
            yield return m_Eyes.ScaleTo(1, 0.2f).Ease(Curve.BackOut);
        }

        #endregion // Animations
    }
}