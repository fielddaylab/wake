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

        #endregion // Inspector

        [NonSerialized] private PortableMenu m_Menu;
        [NonSerialized] private Routine m_NewAnim;
        [NonSerialized] private RectTransformState m_OriginalAnimState;

        public Toggle Toggle { get { return m_Toggle; } }

        protected override void Awake()
        {
            base.Awake();

            m_Menu = Services.UI.FindPanel<PortableMenu>();

            m_Menu.OnHideEvent.AddListener(OnMenuClose);

            m_Toggle.onValueChanged.AddListener(OnToggleValue);

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

            m_OriginalAnimState = RectTransformState.Create(m_AnimationRoot);
        }

        protected override void OnDisable()
        {
            m_Toggle.isOn = false;
            m_NewAnim.Stop();

            m_OriginalAnimState.Apply(m_AnimationRoot);

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
            m_OriginalAnimState.Apply(m_AnimationRoot);

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
            yield return m_AnimationRoot.AnchorPosTo(m_AnimationRoot.anchoredPosition.y + 4, 0.3f, Axis.Y).Ease(Curve.CubeOut);
            yield return m_AnimationRoot.AnchorPosTo(m_OriginalAnimState.AnchoredPos.y, 0.5f, Axis.Y).Ease(Curve.BounceOut);
        }

        #endregion // Animation
    }
}