using System;
using System.Collections;
using Aqua;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using BeauUtil.UI;
using BeauUtil.Variants;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Portable {
    public class PortableMenu : SharedPanel {
        #region Persistence

        static public readonly TableKeyPair Var_LastOpenTab = TableKeyPair.Parse("global:portable.lastOpenTab");

        #endregion // Persistence

        #region Inspector

        [SerializeField, Required] private Canvas m_Canvas = null;
        [SerializeField, Required] private CanvasGroup m_Fader = null;

        [Header("Animation")]
        [SerializeField] private float m_OffPosition = 0;
        [SerializeField] private TweenSettings m_ToOnAnimSettings = new TweenSettings(0.2f, Curve.CubeOut);
        [SerializeField] private float m_OnPosition = 0;
        [SerializeField] private TweenSettings m_ToOffAnimSettings = new TweenSettings(0.2f, Curve.CubeIn);

        [Header("Bottom Buttons")]
        [SerializeField, Required] private Button m_CloseButton = null;
        [Space]
        [SerializeField, Required] private CanvasGroup m_AppNavigationGroup = null;
        [SerializeField, Required] private ToggleGroup m_AppButtonToggleGroup = null;
        [SerializeField, Required] private PortableTabToggle[] m_AppButtons = null;

        #endregion // Inspector

        [NonSerialized] private BaseInputLayer m_Input;
        [NonSerialized] private PortableRequest m_Request;

        #region Unity Events

        protected override void Awake() {
            base.Awake();
            m_Input = BaseInputLayer.Find(this);

            m_CloseButton.onClick.AddListener(() => Hide());
            m_Fader.EnsureComponent<PointerListener>().onClick.AddListener((p) => Hide());
        }

        protected override void OnEnable() {
            base.OnEnable();
        }

        protected override void OnDisable() {
            base.OnDisable();
        }

        protected override void OnDestroy() {
            base.OnDestroy();
        }

        #endregion // Unity Events

        #region Requests

        public void Open(PortableRequest inRequest) {
            m_Request = inRequest;
            Show();
        }

        private void HandleRequest() {
            PortableTabToggle requestTab = null;
            if (m_Request.Type > 0) {
                requestTab = GetAppButton(m_Request.App);
            } else {
                PortableAppId lastKnownApp = (PortableAppId) Services.Data.GetVariable(Var_LastOpenTab).AsInt();
                requestTab = GetAppButton(lastKnownApp);
            }

            requestTab.Toggle.isOn = true;
            m_AppNavigationGroup.interactable = (m_Request.Flags & PortableRequestFlags.DisableNavigation) == 0;
            m_CloseButton.interactable = (m_Request.Flags & PortableRequestFlags.DisableClose) == 0;
            requestTab.App.HandleRequest(m_Request);

            Services.Events.Dispatch(GameEvents.PortableOpened, m_Request);
        }

        private PortableTabToggle GetAppButton(PortableAppId inId) {
            for (int i = 0; i < m_AppButtons.Length; ++i) {
                var button = m_AppButtons[i];
                if (button.Id() == inId) {
                    return button;
                }
            }
            return null;
        }

        #endregion // Requests

        #region BasePanel

        protected override void OnShow(bool inbInstant) {
            Services.Data.SetVariable("portable:open", true);

            if (m_Request.Type > 0 && Services.UI.IsLetterboxed() && (m_Request.Flags & PortableRequestFlags.ForceInputEnabled) != 0) {
                m_Input.Override = true;
                BringToFront();
            } else {
                m_Input.Override = null;
            }

            m_Canvas.enabled = true;
            m_AppButtonToggleGroup.allowSwitchOff = false;
            m_Input.PushPriority();

            base.OnShow(inbInstant);

            Services.Script.TriggerResponse(GameTriggers.PortableOpened);
        }

        protected override void OnHide(bool inbInstant) {
            Services.Data?.SetVariable("portable:open", false);

            m_Input.PopPriority();
            m_Input.Override = null;

            m_Request.Dispose();
            m_CloseButton.interactable = true;
            m_AppNavigationGroup.interactable = true;

            Services.Events?.Dispatch(GameEvents.PortableClosed);

            base.OnHide(inbInstant);
        }

        protected override void OnHideComplete(bool inbInstant) {
            m_Canvas.enabled = false;
            m_Input.Override = false;

            Streaming.UnloadUnusedAsync();

            base.OnHideComplete(inbInstant);
        }

        protected override IEnumerator TransitionToShow() {
            if (!m_RootTransform.gameObject.activeSelf) {
                m_RootTransform.SetAnchorPos(m_OffPosition, Axis.X);
                m_RootTransform.gameObject.SetActive(true);

                m_Fader.alpha = 0;
                m_Fader.gameObject.SetActive(true);
            }

            HandleRequest();

            yield return Routine.Combine(
                m_RootTransform.AnchorPosTo(m_OnPosition, m_ToOnAnimSettings, Axis.X),
                m_Fader.FadeTo(1, m_ToOnAnimSettings.Time)
            );
        }

        protected override void InstantTransitionToShow() {
            m_Fader.alpha = 1;
            m_Fader.gameObject.SetActive(true);
            m_RootTransform.SetAnchorPos(m_OnPosition, Axis.X);
            m_RootTransform.gameObject.SetActive(true);
            HandleRequest();
        }

        protected override IEnumerator TransitionToHide() {
            yield return Routine.Combine(
                m_RootTransform.AnchorPosTo(m_OffPosition, m_ToOffAnimSettings, Axis.X),
                m_Fader.FadeTo(0, m_ToOffAnimSettings.Time)
            );
            m_RootTransform.gameObject.SetActive(false);
            m_Fader.gameObject.SetActive(false);
        }

        protected override void InstantTransitionToHide() {
            m_Fader.gameObject.SetActive(false);
            m_RootTransform.gameObject.SetActive(false);
            m_RootTransform.SetAnchorPos(m_OffPosition, Axis.X);
        }

        #endregion // BasePanel

        static public void OpenApp(PortableAppId inId) {
            Services.UI.FindPanel<PortableMenu>().Open(PortableRequest.OpenApp(inId));
        }

        static public Future<StringHash32> RequestFact() {
            var request = PortableRequest.SelectFact();
            Services.UI.FindPanel<PortableMenu>().Open(request);
            return request.Response;
        }
    }
}