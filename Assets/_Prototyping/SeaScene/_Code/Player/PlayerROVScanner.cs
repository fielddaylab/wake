using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using UnityEngine.SceneManagement;
using Aqua;

namespace ProtoAqua.Observation
{
    public class PlayerROVScanner : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private CircleCollider2D m_RangeCollider = null;
        [SerializeField] private Transform m_RangeVisuals = null;
        [SerializeField] private ParticleSystem[] m_RangeParticleSystems = null;
        [SerializeField] private float m_MaxRange = 8;
        [SerializeField] private float m_ScanRange = 0.5f;

        #endregion // Inspector

        [NonSerialized] private bool m_ScannerOn = false;
        [NonSerialized] private ScannableRegion m_TargetScannable = null;
        [NonSerialized] private StringHash32 m_TargetScanId = StringHash32.Null;
        [NonSerialized] private float m_CurrentRange;

        [NonSerialized] private Routine m_ScanEnableRoutine;
        [NonSerialized] private Routine m_ScanRoutine;

        #region Unity Events

        private void Awake()
        {
            SetRange(0);
        }

        #endregion // Unity Events

        #region State

        public void Enable()
        {
            if (m_ScannerOn)
                return;

            m_ScannerOn = true;
            Services.Events.Dispatch(ObservationEvents.ScannerOn);
            m_ScanEnableRoutine.Replace(this, TurnOnAnim());
        }

        public void Disable()
        {
            if (!m_ScannerOn)
                return;

            CancelScan();
            ObservationServices.SceneUI.Scanner().Hide();
            m_ScannerOn = false;
            Services.Events.Dispatch(ObservationEvents.ScannerOff);
            m_ScanEnableRoutine.Replace(this, TurnOffAnim());
        }

        #endregion // State

        #region Scanning

        public void UpdateScan(in PlayerROV.InputData inInput)
        {
            if (!m_TargetScannable.IsReferenceNull())
            {
                if (!inInput.UseHold || !m_TargetScannable || !m_TargetScannable.CanScan() || !m_TargetScannable.isActiveAndEnabled || m_TargetScannable.ScanId() != m_TargetScanId)
                {
                    CancelScan();
                }
            }
            else
            {
                if (inInput.UseHold && inInput.Target.HasValue)
                {
                    Collider2D scannableCollider = Physics2D.OverlapCircle(inInput.Target.Value, m_ScanRange, GameLayers.Scannable_Mask);
                    bool bFound = false;
                    if (scannableCollider != null)
                    {
                        ScannableRegion scannable = scannableCollider.GetComponentInParent<ScannableRegion>();
                        if (scannable != null && scannable.CanScan())
                        {
                            bFound = true;
                            StartScan(scannable);
                        }
                    }
                    if (!bFound)
                    {
                        ObservationServices.SceneUI.Scanner().Hide();
                    }
                }
            }
        }

        public ScannableRegion CurrentTarget()
        {
            return m_TargetScannable;
        }

        private void StartScan(ScannableRegion inRegion)
        {
            CancelScan();

            if (inRegion != null)
            {
                m_TargetScannable = inRegion;
                m_TargetScanId = inRegion.ScanId();

                m_ScanRoutine.Replace(this, ScanRoutine());
                m_TargetScannable.StartScan();
                m_TargetScannable.UpdateScan(0);

                Services.Audio.PostEvent("scan_start");
            }
        }

        public void CancelScan()
        {
            if (!m_TargetScannable.IsReferenceNull())
            {
                if (m_TargetScannable)
                {
                    m_TargetScannable.CancelScan();
                }

                ObservationServices.SceneUI.Scanner().CancelIfProgress();

                m_TargetScannable = null;
                m_TargetScanId = null;

                m_ScanRoutine.Stop();
            }
        }

        private IEnumerator ScanRoutine()
        {
            var mgr = Services.Tweaks.Get<ScanDataMgr>();
            var scanUI = ObservationServices.SceneUI.Scanner();

            ScanData data;
            mgr.TryGetScanData(m_TargetScanId, out data);

            float progress = 0;
            float duration = mgr.GetScanDuration(data);
            float increment = 1f / duration;

            while(progress < 1)
            {
                progress += increment * Routine.DeltaTime;
                if (progress > 1)
                    progress = 1;
                scanUI.ShowProgress(progress);
                m_TargetScannable.UpdateScan(progress);
                yield return null;
            }

            if (m_TargetScannable.CompleteScan())
            {
                if (data != null && !string.IsNullOrEmpty(data.LogbookId()))
                {
                    Services.Audio.PostEvent("scan_logbook");
                }
                else
                {
                    Services.Audio.PostEvent("scan_complete");
                }
            }
            scanUI.ShowScan(data);
        }

        #endregion // Scanning

        #region Animation

        private IEnumerator TurnOnAnim()
        {
            yield return Tween.Float(m_CurrentRange, m_MaxRange, SetRange, 0.25f).Ease(Curve.CubeOut);
        }

        private IEnumerator TurnOffAnim()
        {
            yield return Tween.Float(m_CurrentRange, 0, SetRange, 0.1f).Ease(Curve.CubeOut);
        }

        private void SetRange(float inRange)
        {
            m_CurrentRange = inRange;

            if (inRange > 0)
            {
                m_RangeCollider.gameObject.SetActive(true);
                m_RangeCollider.radius = inRange;

                m_RangeVisuals.gameObject.SetActive(true);
                m_RangeVisuals.SetScale(inRange * 2, Axis.XY);

                foreach(var ps in m_RangeParticleSystems)
                {
                    var shape = ps.shape;
                    shape.radius = inRange;

                    if (m_ScannerOn && !ps.isEmitting)
                    {
                        ps.Play();
                    }
                    else if (!m_ScannerOn)
                    {
                        ps.Stop();
                    }
                }
            }
            else
            {
                m_RangeCollider.gameObject.SetActive(false);
                m_RangeVisuals.gameObject.SetActive(false);
                foreach(var ps in m_RangeParticleSystems)
                {
                    ps.Stop();
                }
            }
        }

        #endregion // Animation
    }
}