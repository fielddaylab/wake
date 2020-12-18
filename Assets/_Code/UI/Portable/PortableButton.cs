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
        [SerializeField, Required] private RectTransform m_NewIcon = null;

        #endregion // Inspector

        [NonSerialized] private PortableMenu m_Menu;
        [NonSerialized] private Routine m_NewAnim;
        [NonSerialized] private bool m_HasNew;
        [NonSerialized] private RectTransformState m_OriginalAnimState;

        [NonSerialized] private BaseInputLayer m_InputLayer;
        [NonSerialized] private IPortableRequest m_Request;

        public Toggle Toggle { get { return m_Toggle; } }

        protected override void Awake()
        {
            base.Awake();

            m_InputLayer = BaseInputLayer.Find(this);
            m_InputLayer.Device.OnUpdate += CheckInput;

            m_Menu = Services.UI.FindPanel<PortableMenu>();

            m_Menu.OnHideEvent.AddListener(OnMenuClose);

            m_Toggle.onValueChanged.AddListener(OnToggleValue);
            m_NewIcon.gameObject.SetActive(false);

            Services.Events.Register<BestiaryUpdateParams>(GameEvents.BestiaryUpdated, OnBestiaryUpdated, this)
                .Register(GameEvents.CutsceneStart, OnCutsceneStart, this)
                .Register(GameEvents.CutsceneEnd, OnCutsceneEnd, this)
                .Register<IPortableRequest>(GameEvents.PortableOpened, OnPortableOpened)
                .Register(GameEvents.PortableClosed, OnPortableClosed);
        }

        private void OnDestroy()
        {
            m_InputLayer.Device.OnUpdate -= CheckInput;

            m_Menu.OnHideEvent.RemoveListener(OnMenuClose);

            Services.Events?.DeregisterAll(this);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_OriginalAnimState = RectTransformState.Create(m_AnimationRoot);
            if (m_HasNew)
            {
                m_NewAnim.Replace(this, NewAnim());
            }
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

        private void CheckInput(DeviceInput inDevice)
        {
            if (inDevice.KeyPressed(KeyCode.Tab))
            {
                m_Toggle.isOn = !m_Toggle.isOn;
            }
        }

        private void OnBestiaryUpdated(BestiaryUpdateParams inBestiaryUpdate)
        {
            m_HasNew = true;
            if (!m_NewAnim)
            {
                m_NewAnim.Replace(this, NewAnim());
            }

            m_NewIcon.gameObject.SetActive(true);
            m_Request = new BestiaryApp.OpenToRequest(inBestiaryUpdate);
        }

        private void OnCutsceneStart()
        {
            Hide();
        }

        private void OnCutsceneEnd()
        {
            Show();
        }

        private void OnPortableOpened(IPortableRequest inRequest)
        {
            m_NewAnim.Stop();
            m_HasNew = false;
            m_Request = null;
            m_OriginalAnimState.Apply(m_AnimationRoot);
            m_NewIcon.gameObject.SetActive(false);

            m_Toggle.SetIsOnWithoutNotify(true);
            m_Toggle.interactable = inRequest == null || inRequest.CanClose();
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
                m_Menu.Open(m_Request);
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
            while(true)
            {
                yield return m_AnimationRoot.AnchorPosTo(m_AnimationRoot.anchoredPosition.y + 4, 0.3f, Axis.Y).Ease(Curve.CubeOut);
                yield return m_AnimationRoot.AnchorPosTo(m_OriginalAnimState.AnchoredPos.y, 0.5f, Axis.Y).Ease(Curve.BounceOut);
                yield return 2f;
            }
        }

        #endregion // Animation
    }
}