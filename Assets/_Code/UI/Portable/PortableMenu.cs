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
        #region Inspector

        [SerializeField, Required] private Canvas m_Canvas = null;
        [SerializeField, Required] private CanvasGroup m_Fader = null;

        [Header("Animation")]
        [SerializeField] private float m_OffPosition = 0;
        [SerializeField] private TweenSettings m_ToOnAnimSettings = new TweenSettings(0.2f, Curve.CubeOut);
        [SerializeField] private float m_OnPosition = 0;
        [SerializeField] private TweenSettings m_ToOffAnimSettings = new TweenSettings(0.2f, Curve.CubeIn);

        [Header("Tabs")]
        [SerializeField, Required] private CanvasGroup m_AppNavigationGroup = null;
        [SerializeField, Required] private ToggleGroup m_AppButtonToggleGroup = null;
        [SerializeField, Required] private PortableTabToggle[] m_AppButtons = null;

        #endregion // Inspector

        static private bool s_RegisteredHandler = false;

        [NonSerialized] private BaseInputLayer m_Input;
        [NonSerialized] private PortableRequest m_Request;
        [NonSerialized] private float m_ActiveOnPosition;
        [NonSerialized] private bool? m_InputOverrideSetting;

        #region Unity Events

        protected override void Awake() {
            base.Awake();
            m_Input = BaseInputLayer.Find(this);

            m_Fader.EnsureComponent<PointerListener>().onClick.AddListener((p) => Hide());

            if (!s_RegisteredHandler) {
                Services.Script.RegisterChoiceSelector("fact", RequestFact);
            }

            Services.UI.Popup.OnShowEvent.AddListener((s) => OnPopupOpened());
            Services.UI.Popup.OnHideCompleteEvent.AddListener((s) => OnPopupClosed());
        }

        #endregion // Unity Events

        #region Requests

        public void Open(PortableRequest inRequest) {
            m_Request = inRequest;
            if (!IsShowing()) {
                Show();
            } else {
                AdjustInputForRequest();
                HandleRequest();
            }
        }

        private void HandleRequest() {
            PortableTabToggle requestTab = null;
            if (m_Request.Type > 0) {
                requestTab = GetAppButton(m_Request.App);
                if (m_Request.Type == PortableRequestType.SelectFact || m_Request.Type == PortableRequestType.SelectFactSet) {
                    GetAppButton(PortableAppId.Organisms).App.HandleRequest(m_Request);
                    GetAppButton(PortableAppId.Environments).App.HandleRequest(m_Request);
                }
            } else {
                requestTab = GetAppButton(PortableAppId.Job);
            }

            requestTab.Toggle.isOn = true;
            m_AppNavigationGroup.interactable = (m_Request.Flags & PortableRequestFlags.DisableNavigation) == 0;
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

        private void AdjustInputForRequest() {
            if (m_Request.Type > 0 && Script.ShouldBlock() && (m_Request.Flags & PortableRequestFlags.ForceInputEnabled) != 0) {
                m_Input.Override = true;
                Services.Input.PushFlags(InputLayerFlags.Portable, this);
                m_ActiveOnPosition = m_OnPosition * 0.25f;
                BringToFront(GameSortingLayers.AboveCutscene);
            } else {
                m_Input.Override = null;
                m_ActiveOnPosition = m_OnPosition;
            }

            m_InputOverrideSetting = m_Input.Override;
        }

        #endregion // Requests

        #region BasePanel

        protected override void OnShow(bool inbInstant) {
            Services.Data.SetVariable("portable:open", true);

            AdjustInputForRequest();

            m_Canvas.enabled = true;
            m_AppButtonToggleGroup.allowSwitchOff = false;
            m_Input.PushPriority();

            base.OnShow(inbInstant);

            Services.Script.TriggerResponse(GameTriggers.PortableOpened);
        }

        protected override void OnHide(bool inbInstant) {
            Services.Data?.SetVariable("portable:open", false);

            m_Input.PopPriority();
            if (m_InputOverrideSetting.HasValue) {
                Services.Input?.PopFlags(this);
                m_Input.Override = null;
                m_InputOverrideSetting = null;
            }


            m_Request.Dispose();
            m_AppNavigationGroup.interactable = true;

            foreach(var button in m_AppButtons) {
                button.App.ClearRequest();
            }

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
                m_RootTransform.AnchorPosTo(m_ActiveOnPosition, m_ToOnAnimSettings, Axis.X),
                m_Fader.FadeTo(1, m_ToOnAnimSettings.Time)
            );
        }

        protected override void InstantTransitionToShow() {
            m_Fader.alpha = 1;
            m_Fader.gameObject.SetActive(true);
            m_RootTransform.SetAnchorPos(m_ActiveOnPosition, Axis.X);
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

        #region Handlers

        private void OnPopupOpened() {
            if (IsShowing()) {
                m_Input.Override = false;
            }
        }

        private void OnPopupClosed() {
            m_Input.Override = m_InputOverrideSetting;
        }

        #endregion // Handlers

        static public void OpenApp(PortableAppId inId) {
            Services.UI.FindPanel<PortableMenu>().Open(PortableRequest.OpenApp(inId));
        }

        static public void Request(PortableRequest inRequest) {
            Services.UI.FindPanel<PortableMenu>().Open(inRequest);
        }

        static public Future<StringHash32> RequestFact() {
            var request = PortableRequest.SelectFact();
            Services.UI.FindPanel<PortableMenu>().Open(request);
            return request.Response;
        }
    }
}