using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using Aqua;
using System.Collections;
using System;
using BeauUtil.Variants;
using BeauUtil.UI;

namespace Aqua.Portable
{
    public class PortableMenu : SharedPanel
    {
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
        [SerializeField, Required] private PortableAppButton[] m_AppButtons = null;

        #endregion // Inspector

        [NonSerialized] private BaseInputLayer m_Input;
        [NonSerialized] private IPortableRequest m_Request;
        [NonSerialized] private VariantTable m_Table;

        #region Unity Events

        protected override void Awake()
        {
            base.Awake();
            m_Input = BaseInputLayer.Find(this);

            m_CloseButton.onClick.AddListener(() => Hide());
            m_Fader.EnsureComponent<PointerListener>().onClick.AddListener((p) => Hide());

            m_Table = new VariantTable("portable");
            Services.Data.BindTable("portable", m_Table);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Services.Data?.UnbindTable("portable");
        }

        #endregion // Unity Events

        #region Requests

        public void Open(IPortableRequest inRequest = null)
        {
            m_Request = inRequest;
            
            Show();
            OnRequest();
        }

        private void OnRequest()
        {
            if (m_Request != null)
            {
                m_AppNavigationGroup.interactable = m_Request.CanNavigateApps();
                m_CloseButton.interactable = m_Request.CanClose();
                for(int i = 0; i < m_AppButtons.Length; ++i)
                {
                    var button = m_AppButtons[i];
                    if (button.Id() == m_Request.AppId())
                    {
                        button.Toggle.isOn = true;
                        button.App.TryHandle(m_Request);
                    }
                    else
                    {
                        button.Toggle.isOn = false;
                    }
                }
            }
            else
            {
                m_AppNavigationGroup.interactable = true;
                m_CloseButton.interactable = true;
                m_AppButtons[0].Toggle.group.SetAllTogglesOff(true);
            }

            Services.Events.Dispatch(GameEvents.PortableOpened, m_Request);
        }

        #endregion // Requests

        #region BasePanel

        protected override void OnShow(bool inbInstant)
        {
            Services.Data.SetVariable("portable:open", true);

            if(m_Request != null && Services.UI.IsLetterboxed() && m_Request.ForceInputEnabled()) 
            {
                m_Input.Override = true;
                BringToFront();
            }
            else
            {
                m_Input.Override = null;
            }

            m_Canvas.enabled = true;
            m_Input.PushPriority();

            base.OnShow(inbInstant);
        }

        protected override void OnHide(bool inbInstant)
        {
            Services.Data?.SetVariable("portable:open", false);

            m_Input.PopPriority();
            m_Input.Override = null;

            m_Request = null;
            m_CloseButton.interactable = true;
            m_AppNavigationGroup.interactable = true;

            Services.Events?.Dispatch(GameEvents.PortableClosed);

            base.OnHide(inbInstant);
        }

        protected override void OnHideComplete(bool inbInstant)
        {
            m_Canvas.enabled = false;
            m_Input.Override = false;
            base.OnHideComplete(inbInstant);
        }

        protected override IEnumerator TransitionToShow()
        {
            if (!m_RootTransform.gameObject.activeSelf)
            {
                m_RootTransform.SetAnchorPos(m_OffPosition, Axis.X);
                m_RootTransform.gameObject.SetActive(true);

                m_Fader.alpha = 0;
                m_Fader.gameObject.SetActive(true);
            }

            yield return Routine.Combine(
                m_RootTransform.AnchorPosTo(m_OnPosition, m_ToOnAnimSettings, Axis.X),
                m_Fader.FadeTo(1, m_ToOnAnimSettings.Time)
            );
        }

        protected override void InstantTransitionToShow()
        {
            m_Fader.alpha = 1;
            m_Fader.gameObject.SetActive(true);
            m_RootTransform.SetAnchorPos(m_OnPosition, Axis.X);
            m_RootTransform.gameObject.SetActive(true);
        }

        protected override IEnumerator TransitionToHide()
        {
            yield return Routine.Combine(
                m_RootTransform.AnchorPosTo(m_OffPosition, m_ToOffAnimSettings, Axis.X),
                m_Fader.FadeTo(0, m_ToOffAnimSettings.Time)
            );
            m_RootTransform.gameObject.SetActive(false);
            m_Fader.gameObject.SetActive(false);
        }

        protected override void InstantTransitionToHide()
        {
            m_Fader.gameObject.SetActive(false);
            m_RootTransform.gameObject.SetActive(false);
            m_RootTransform.SetAnchorPos(m_OffPosition, Axis.X);
        }
    
        #endregion // BasePanel
    }
}