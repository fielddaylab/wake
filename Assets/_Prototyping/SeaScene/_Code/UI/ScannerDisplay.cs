using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using BeauRoutine.Extensions;
using TMPro;
using UnityEngine.UI;
using Aqua;

namespace ProtoAqua.Observation
{
    public class ScannerDisplay : BasePanel
    {
        #region Inspector

        [Header("Scanner")]

        [SerializeField] private Image m_Background = null;

        [Header("Scanning")]
        [SerializeField] private CanvasGroup m_ScanningGroup = null;
        [SerializeField] private Image m_ScanProgressBar = null;

        [Header("Data")]
        [SerializeField] private CanvasGroup m_DataGroup = null;
        [SerializeField] private TMP_Text m_HeaderText = null;
        [SerializeField] private TMP_Text m_DescriptionText = null;

        #endregion // Inspector

        [NonSerialized] private ScanData m_CurrentScanData;
        [NonSerialized] private Routine m_BounceRoutine;
        [NonSerialized] private Routine m_TypeRoutine;

        [NonSerialized] private Color m_DefaultBackgroundColor;
        [NonSerialized] private Color m_DefaultHeaderColor;
        [NonSerialized] private Color m_DefaultTextColor;
        [NonSerialized] private Vector4 m_DefaultTextMargins;

        protected override void Awake()
        {
            base.Awake();

            m_DefaultBackgroundColor = m_Background.color;
            m_DefaultHeaderColor = m_HeaderText.color;
            m_DefaultTextColor = m_DescriptionText.color;
            m_DefaultTextMargins = m_DescriptionText.margin;
        }

        #region Scanning

        public void ShowProgress(float inProgress)
        {
            Show();

            m_CurrentScanData = null;

            if (m_RootTransform.gameObject.activeSelf)
            {
                m_BounceRoutine.Replace(this, BounceAnim());
            }

            m_DataGroup.gameObject.SetActive(false);

            if (!m_ScanningGroup.gameObject.activeSelf)
            {
                m_Background.SetColor(m_DefaultBackgroundColor);
                m_HeaderText.SetColor(m_DefaultHeaderColor);
                m_DescriptionText.SetColor(m_DefaultBackgroundColor);

                m_ScanningGroup.gameObject.SetActive(true);

                LayoutRebuilder.MarkLayoutForRebuild(m_RootTransform);
            }

            m_ScanProgressBar.fillAmount = inProgress;
        }

        public void ShowScan(ScanData inData)
        {
            Show();

            var mgr = Services.Tweaks.Get<ScanDataMgr>();
            var config = mgr.GetConfig(inData == null ? 0 : inData.Flags());

            m_CurrentScanData = inData;
            m_BounceRoutine.Replace(this, BounceAnim());

            m_ScanningGroup.gameObject.SetActive(false);
            m_TypeRoutine.Stop();

            m_DataGroup.gameObject.SetActive(true);
            if (inData == null)
            {
                m_HeaderText.SetText("Missing Scan");
                m_DescriptionText.SetText("This scan data was either not set or not loaded.");
            }
            else
            {
                string header = inData.Header();
                if (string.IsNullOrEmpty(header))
                {
                    m_HeaderText.SetText(string.Empty);
                    m_HeaderText.gameObject.SetActive(false);

                    m_DescriptionText.margin = Vector4.zero;
                }
                else
                {
                    m_HeaderText.SetText(header);
                    m_HeaderText.gameObject.SetActive(true);

                    m_DescriptionText.margin = m_DefaultTextMargins;
                }

                string text = inData.Text();
                if (string.IsNullOrEmpty(text))
                {
                    m_DescriptionText.SetText(string.Empty);
                    m_DescriptionText.gameObject.SetActive(false);
                }
                else
                {
                    m_DescriptionText.SetText(text);
                    m_DescriptionText.gameObject.SetActive(true);

                    m_DescriptionText.maxVisibleCharacters = 0;

                    m_TypeRoutine.Replace(this, TypeOut());
                }
            }

            m_Background.SetColor(config.BackgroundColor);
            m_HeaderText.SetColor(config.HeaderColor);
            m_DescriptionText.SetColor(config.TextColor);

            LayoutRebuilder.ForceRebuildLayoutImmediate(m_RootTransform);

            Services.Audio.PostEvent(config.OpenSound);
        }

        public void CancelIfProgress()
        {
            if (m_CurrentScanData == null)
            {
                Hide();
            }
        }

        #endregion // Scanning

        #region Animations
        
        protected override void InstantTransitionToShow()
        {
            m_RootTransform.gameObject.SetActive(true);
            m_RootTransform.SetScale(1);
            m_RootGroup.alpha = 1;
        }

        protected override IEnumerator TransitionToShow()
        {
            if (!m_RootTransform.gameObject.activeSelf)
            {
                m_RootGroup.alpha = 0;
                m_RootTransform.SetScale(new Vector3(0.25f, 0.5f), Axis.XY);
                m_RootTransform.gameObject.SetActive(true);
            }

            yield return Routine.Combine(
                m_RootGroup.FadeTo(1, 0.25f),
                m_RootTransform.ScaleTo(1, 0.25f, Axis.XY).Ease(Curve.BackOut)
            );
        }

        protected override void InstantTransitionToHide()
        {
            m_RootTransform.gameObject.SetActive(false);
            m_RootTransform.SetScale(1);
            m_RootGroup.alpha = 0;
        }

        protected override IEnumerator TransitionToHide()
        {
            if (m_RootTransform.gameObject.activeSelf)
            {
                yield return Routine.Combine(
                    m_RootGroup.FadeTo(0, 0.1f),
                    m_RootTransform.ScaleTo(new Vector2(0.25f, 0.5f), 0.1f, Axis.XY).Ease(Curve.CubeIn)
                );

                m_RootTransform.gameObject.SetActive(false);
            }
        }

        private IEnumerator BounceAnim()
        {
            yield return m_RootTransform.AnchorPosTo(-15, 0.1f, Axis.Y).Ease(Curve.BackOut).From().ForceOnCancel();
        }

        private IEnumerator TypeOut()
        {
            yield return Tween.Int(0, m_DescriptionText.textInfo.characterCount, (c) => m_DescriptionText.maxVisibleCharacters = c, 0.8f);
        }

        #endregion // Animations

        #region Callbacks

        protected override void OnHide(bool inbInstant)
        {
            m_TypeRoutine.Stop();
            m_CurrentScanData = null;
            m_BounceRoutine.Stop();
        }

        protected override void OnHideComplete(bool inbInstant)
        {
            m_HeaderText.SetText(string.Empty);
            m_DescriptionText.SetText(string.Empty);
        }

        #endregion // Callbacks
    }
}