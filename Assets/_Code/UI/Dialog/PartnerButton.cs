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
    [RequireComponent(typeof(UnlockableUI))]
    public class PartnerButton : BasePanel
    {
        static public readonly TableKeyPair Var_RequestCounter = TableKeyPair.Parse("guide:help.requests");
        static public readonly StringHash32 Event_WorldAvatarPresent = "guide:avatar-present";
        static public readonly StringHash32 Event_WorldAvatarHidden = "guide:avatar-hidden";

        #region Inspector

        [SerializeField] private Button m_Button = null;
        [SerializeField] private Transform m_Eyes = null;
        [SerializeField] private float m_OffscreenX = 80;
        [SerializeField] private TweenSettings m_ShowHideAnim = new TweenSettings(0.2f, Curve.Smooth);
        [SerializeField] private UnlockableUI m_UnlockableUI = null;

        #endregion // Inspector

        [NonSerialized] private float m_OnscreenX;
        [NonSerialized] private int m_OnscreenAvatarCount;

        protected override void Awake() {
            m_Button.onClick.AddListener(OnButtonClicked);

            m_OnscreenX = Root.anchoredPosition.x;

            Services.Script.OnTargetedThreadStarted += OnTargetedThreadStart;
            Services.Script.OnTargetedThreadKilled += OnTargetedThreadEnd;

            OnTargetedThreadStart(Services.Script.GetTargetThread(GameConsts.Target_V1ctor));

            this.CacheComponent(ref m_UnlockableUI).IsUnlocked = (o) => m_OnscreenAvatarCount <= 0;

            Services.Events.Register(Event_WorldAvatarPresent, OnWorldPartnerEnabled, this)
                .Register(Event_WorldAvatarHidden, OnWorldPartnerDisabled, this);
        }

        private void OnDestroy()
        {
            if (Services.Script)
            {
                Services.Script.OnTargetedThreadStarted -= OnTargetedThreadStart;
                Services.Script.OnTargetedThreadKilled -= OnTargetedThreadEnd;
            }

            Services.Events?.DeregisterAll(this);
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
            Services.Data.AddVariable(Var_RequestCounter, 1);
            Services.Script.TriggerResponse(GameTriggers.RequestPartnerHelp, GameConsts.Target_V1ctor);
        }

        private void OnWorldPartnerEnabled() {
            m_OnscreenAvatarCount++;
            if (m_OnscreenAvatarCount == 1) {
                m_UnlockableUI.Reload();
            }
        }

        private void OnWorldPartnerDisabled() {
            if (m_OnscreenAvatarCount > 0) {
                m_OnscreenAvatarCount--;
                if (m_OnscreenAvatarCount == 0) {
                    m_UnlockableUI.Reload();
                }
            }
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