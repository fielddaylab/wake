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
using EasyAssetStreaming;
using Leaf.Runtime;
using UnityEngine.Scripting;
using Aqua.Profile;

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

        [Header("Logo")]
        [SerializeField] private Mask m_LogoMask = null;
        [SerializeField] private CanvasGroup m_LogoColorGroup = null;
        [SerializeField] private Graphic[] m_LogoColorBlocks = null;

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

        [NonSerialized] private int m_LogoColorIndex = -1;
        [NonSerialized] private Routine m_LogoColorFlash;

        #region Unity Events

        protected override void Awake() {
            base.Awake();
            m_Input = BaseInputLayer.Find(this);

            m_Fader.EnsureComponent<PointerListener>().onClick.AddListener((p) => Hide());

            if (!s_RegisteredHandler) {
                Services.Script.RegisterChoiceSelector("fact", RequestFact);
            }

            Func<float> initialDelayFunc = () => {
                if (IsTransitioning()) {
                    return m_ToOnAnimSettings.Time;
                } else {
                    return 0;
                }
            };
            for(int i = 0; i < m_AppButtons.Length; i++) {
                m_AppButtons[i].SetInitialDelay(initialDelayFunc);
            }

            Services.UI.Popup.OnShowEvent.AddListener((s) => OnPopupOpened());
            Services.UI.Popup.OnHideCompleteEvent.AddListener((s) => OnPopupClosed());

            Services.Events.Register(GameEvents.SceneWillUnload, () => Hide(), this);

            m_Input.OnInputEnabled.AddListener(() => {
                Services.Secrets.AllowCheats("aqos");
            });
            m_Input.OnInputDisabled.AddListener(() => {
                Services.Secrets.DisallowCheats("aqos");
            });

            Services.Secrets.RegisterCheat("aqos_flag", SecretService.CheatType.Repeat, "aqos", "transrights", AdvanceLogoColoring, null, ClearLogoColoring);
        }

        protected override void OnDestroy() {
            Services.Events?.DeregisterAll(this);
            base.OnDestroy();
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
                    GetAppButton(PortableAppId.Specter).App.HandleRequest(m_Request);
                }
            } else {
                requestTab = GetAppButton(PortableAppId.Job);
            }

            requestTab.Toggle.isOn = true;
            m_AppNavigationGroup.interactable = (m_Request.Flags & PortableRequestFlags.DisableNavigation) == 0;
            requestTab.App.HandleRequest(m_Request);

            Services.Events.Dispatch(GameEvents.PortableOpened, m_Request);
        }

        private void UpdateAvailableTabs() {
            for (int i = 0; i < m_AppButtons.Length; ++i) {
                var button = m_AppButtons[i];
                switch(button.Id()) {
                    case PortableAppId.Organisms: {
                        button.gameObject.SetActive(Save.Bestiary.HasTab(BestiaryData.TabFlags.Critters));
                        break;
                    }
                    case PortableAppId.Environments: {
                        button.gameObject.SetActive(Save.Bestiary.HasTab(BestiaryData.TabFlags.Environments));
                        break;
                    }
                    case PortableAppId.Specter: {
                        button.gameObject.SetActive(Save.Bestiary.HasTab(BestiaryData.TabFlags.Specters));
                        break;
                    }
                    case PortableAppId.Tech: {
                        button.gameObject.SetActive(Save.Inventory.UpgradeCount() > 0);
                        break;
                    }
                }
            }
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
            Script.WriteVariable("portable:open", true);

            AdjustInputForRequest();

            m_Canvas.enabled = true;
            m_AppButtonToggleGroup.allowSwitchOff = false;
            m_Input.PushPriority();

            base.OnShow(inbInstant);

            Services.Audio.PostEvent("portable.open");
            Services.Script.TriggerResponse(GameTriggers.PortableOpened);
        }

        protected override void OnHide(bool inbInstant) {
            Script.WriteVariable("portable:open", false);

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

            if (WasShowing()) {
                Services.Audio?.PostEvent("portable.close");
                Services.Events?.Dispatch(GameEvents.PortableClosed);
            }

            base.OnHide(inbInstant);
        }

        protected override void OnHideComplete(bool inbInstant) {
            m_Canvas.enabled = false;
            m_Input.Override = false;

            ClearLogoColoring();
            Streaming.UnloadUnusedAsync(30);

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
            UpdateAvailableTabs();

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
            UpdateAvailableTabs();
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

        #region Logo

        static private readonly string LogoColorData = "#5bcff9#f5a8b8#ffffff#f5a8b8#5bcff9|#fdf436#fcfcfc#9d59d2#2c2c2c|#b57edc#ffffff#4a8123|";

        private void ClearLogoColoring() {
            m_LogoMask.enabled = false;
            m_LogoColorGroup.gameObject.SetActive(false);
            m_LogoColorIndex = -1;
            m_LogoColorFlash.Stop();
            m_LogoMask.GetComponent<Image>().color = Parsing.HexColor("#00eee1");
        }

        private void AdvanceLogoColoring() {
            m_LogoMask.enabled = true;
            m_LogoMask.GetComponent<Image>().color = Color.black;
            m_LogoColorGroup.gameObject.SetActive(true);
            
            m_LogoColorIndex = (m_LogoColorIndex + 1) % LogoColorData.Length;

            int blocksUsed = 0;

            while(LogoColorData[m_LogoColorIndex] != '|') {
                StringSlice colorStr = new StringSlice(LogoColorData, m_LogoColorIndex, 7);
                Color color = Parsing.HexColor(colorStr);

                Graphic block = m_LogoColorBlocks[blocksUsed];
                block.gameObject.SetActive(true);
                block.color = color;

                blocksUsed++;
                m_LogoColorIndex = (m_LogoColorIndex + 7) % LogoColorData.Length;
            }

            for(int i = blocksUsed; i < m_LogoColorBlocks.Length; i++) {
                m_LogoColorBlocks[i].gameObject.SetActive(false);
            }

            m_LogoColorGroup.alpha = 1;
            m_LogoColorFlash.Replace(this, m_LogoColorGroup.FadeTo(0, 0.15f).YoyoLoop(2));
        }

        #endregion // Logo

        [LeafMember("OpenPortableToApp"), Preserve]
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

        [LeafMember("ClosePortable"), Preserve]
        static public void Close() {
            Services.UI.FindPanel<PortableMenu>().Hide();
        }

        #region Leaf

        [LeafMember("OpenPortableToEntity"), Preserve]
        static private void LeafOpenToEntity(StringHash32 inEntityId) {
            Request(PortableRequest.ShowEntry(inEntityId));
        }

        [LeafMember("OpenPortableToFact"), Preserve]
        static private void LeafOpenToFact(StringHash32 inFactId) {
            Request(PortableRequest.ShowFact(inFactId));
        }

        #endregion // Leaf
    }
}