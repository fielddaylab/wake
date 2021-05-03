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
    public class ScannerDisplay : SharedPanel
    {
        #region Inspector

        [Header("Scanner")]

        [SerializeField] private Image m_Background = null;

        [Header("Scanning")]
        [SerializeField] private CanvasGroup m_ScanningGroup = null;
        [SerializeField] private Image m_ScanProgressBar = null;

        [Header("Data")]
        [SerializeField] private CanvasGroup m_DataGroup = null;
        [SerializeField] private LocText m_HeaderText = null;
        [SerializeField] private LocText m_DescriptionText = null;

        #endregion // Inspector

        [NonSerialized] private ScanData m_CurrentScanData;
        [NonSerialized] private Routine m_BounceRoutine;
        [NonSerialized] private Routine m_TypeRoutine;

        [NonSerialized] private Color m_DefaultBackgroundColor;
        [NonSerialized] private Color m_DefaultHeaderColor;
        [NonSerialized] private Color m_DefaultTextColor;
        [NonSerialized] private Vector4 m_DefaultTextMargins;

        [NonSerialized] private RectTransform m_RectTransform;
        [NonSerialized] private float m_AnchorOffsetX;

        protected override void Awake()
        {
            base.Awake();

            m_DefaultBackgroundColor = m_Background.color;
            m_DefaultHeaderColor = m_HeaderText.Graphic.color;
            m_DefaultTextColor = m_DescriptionText.Graphic.color;
            m_DefaultTextMargins = m_DescriptionText.Graphic.margin;
            m_RectTransform = (RectTransform)transform;
            m_AnchorOffsetX = m_RectTransform.anchoredPosition.x;
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
                m_HeaderText.Graphic.SetColor(m_DefaultHeaderColor);
                m_DescriptionText.Graphic.SetColor(m_DefaultBackgroundColor);

                m_ScanningGroup.gameObject.SetActive(true);

                LayoutRebuilder.MarkLayoutForRebuild(m_RootTransform);
            }

            m_ScanProgressBar.fillAmount = inProgress;
        }

        public void ShowScan(ScanData inData, ScanResult inResult)
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

                    m_DescriptionText.Graphic.margin = Vector4.zero;
                }
                else
                {
                    m_HeaderText.SetText(header);
                    m_HeaderText.gameObject.SetActive(true);

                    m_DescriptionText.Graphic.margin = m_DefaultTextMargins;
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

                    m_DescriptionText.Graphic.maxVisibleCharacters = 0;

                    m_TypeRoutine.Replace(this, TypeOut());
                }
            }

            m_Background.SetColor(config.BackgroundColor);
            m_HeaderText.Graphic.SetColor(config.HeaderColor);
            m_DescriptionText.Graphic.SetColor(config.TextColor);

            LayoutRebuilder.ForceRebuildLayoutImmediate(m_RootTransform);

            Services.Audio.PostEvent(config.OpenSound);
        }


        public void AdjustForScannableVisibility(Vector2 inScannableObjectPosition, Vector2 inPlayerROVPosition)
        {
            if (inScannableObjectPosition.x < inPlayerROVPosition.x)
            {
                m_RectTransform.SetAnchorPos(-m_AnchorOffsetX, Axis.X);
                m_RectTransform.anchorMin = m_RectTransform.anchorMax = new Vector2(1f, 0.5f);
            }
            else
            {
                m_RectTransform.SetAnchorPos(m_AnchorOffsetX, Axis.X);
                m_RectTransform.anchorMin = m_RectTransform.anchorMax = new Vector2(0f, 0.5f);
            }
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
            return m_RootTransform.AnchorPosTo(-15, 0.1f, Axis.Y).Ease(Curve.BackOut).From().ForceOnCancel();
        }

        private IEnumerator TypeOut()
        {
            return Tween.Int(0, m_DescriptionText.CurrentText.VisibleText.Length, (c) => m_DescriptionText.Graphic.maxVisibleCharacters = c, 0.8f);
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