using UnityEngine;
using BeauRoutine;
using BeauUtil;
using UnityEngine.UI;
using System;
using System.Collections;
using BeauRoutine.Extensions;

namespace Aqua.Portable
{
    public class PortableButton : BasePanel
    {
        #region Inspector

        [SerializeField, Required] private RectTransform m_AnimationRoot = null;
        [SerializeField, Required] private Toggle m_Toggle = null;
        [SerializeField, Required] private FlashAnim m_NewFlash = null;
        [SerializeField] private float m_OffscreenX = 80;
        [SerializeField] private TweenSettings m_ShowHideAnim = new TweenSettings(0.2f, Curve.Smooth);

        #endregion // Inspector

        [NonSerialized] private float m_OnscreenX;
        [NonSerialized] private PortableMenu m_Menu;
        [NonSerialized] private Routine m_NewAnim;

        public Toggle Toggle { get { return m_Toggle; } }

        protected override void Awake() {
            base.Awake();

            m_Menu = Services.UI.FindPanel<PortableMenu>();
            m_Menu.OnHideEvent.AddListener(OnMenuClose);
            m_Toggle.onValueChanged.AddListener(OnToggleValue);

            m_OnscreenX = Root.anchoredPosition.x;

            Services.Events.Register<BestiaryUpdateParams>(GameEvents.BestiaryUpdated, OnBestiaryUpdated, this)
                .Register<PortableRequest>(GameEvents.PortableOpened, OnPortableOpened, this)
                .Register(GameEvents.PortableClosed, OnPortableClosed, this);
        }

        private void OnDestroy()
        {
            m_Menu.OnHideEvent.RemoveListener(OnMenuClose);

            Services.Events?.DeregisterAll(this);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            m_Toggle.isOn = false;
            m_NewAnim.Stop();

            base.OnDisable();
        }

        #region Handlers

        private void OnMenuClose(SharedPanel.TransitionType inTransition)
        {
            m_Toggle.SetIsOnWithoutNotify(false);
        }

        private void OnBestiaryUpdated(BestiaryUpdateParams inBestiaryUpdate)
        {
            if (!m_NewAnim)
            {
                m_NewAnim.Replace(this, NewAnim());
            }
        }

        private void OnPortableOpened(PortableRequest inRequest)
        {
            m_NewAnim.Stop();

            m_Toggle.SetIsOnWithoutNotify(true);
            m_Toggle.interactable = (inRequest.Flags & PortableRequestFlags.DisableClose) == 0;
        }

        private void OnPortableClosed()
        {
            m_Toggle.SetIsOnWithoutNotify(false);
        }

        private void OnToggleValue(bool inbValue)
        {
            if (!m_Menu || !isActiveAndEnabled)
                return;
            
            if (inbValue)
            {
                m_Menu.Open(default);
            }
            else
            {
                m_Menu.Hide();
            }
        }

        #endregion // Handlers

        #region Animation

        private IEnumerator NewAnim()
        {
            while(Script.ShouldBlock())
                yield return null;
            
            Services.Audio.PostEvent("portable.ping.new");
            m_NewFlash.Ping();
        }

        protected override void InstantTransitionToHide() {
            Root.gameObject.SetActive(false);
            Root.SetAnchorPos(m_OffscreenX, Axis.X);
        }

        protected override void InstantTransitionToShow() {
            Root.gameObject.SetActive(true);
            Root.SetAnchorPos(m_OnscreenX, Axis.X);
        }

        protected override IEnumerator TransitionToHide() {
            yield return Root.AnchorPosTo(m_OffscreenX, m_ShowHideAnim, Axis.X);
            Root.gameObject.SetActive(false);
        }

        protected override IEnumerator TransitionToShow() {
            Root.gameObject.SetActive(true);
            yield return Root.AnchorPosTo(m_OnscreenX, m_ShowHideAnim, Axis.X);
        }

        #endregion // Animation
    }
}