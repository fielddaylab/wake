using System;
using UnityEngine;
using BeauUtil;
using BeauRoutine;
using System.Collections;
using Aqua;

namespace ProtoAqua.Observation
{
    public class PlayerROVScanner : MonoBehaviour, PlayerROV.ITool
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

        [NonSerialized] private Collider2D[] m_ColliderBuffer = new Collider2D[1];

        #region Unity Events

        private void Start()
        {
            SetRange(0);
            ScanSystem.Find<ScanSystem>().SetDetector(m_RangeCollider);
        }

        #endregion // Unity Events

        #region State

        public void Enable()
        {
            if (m_ScannerOn)
                return;

            m_ScannerOn = true;
            m_ScanEnableRoutine.Replace(this, TurnOnAnim());
        }

        public void Disable()
        {
            if (!m_ScannerOn)
                return;

            CancelScan();
            Services.UI?.FindPanel<ScannerDisplay>()?.Hide();
            m_ScannerOn = false;
            m_ScanEnableRoutine.Replace(this, TurnOffAnim());
        }

        #endregion // State

        #region Scanning

        public bool UpdateTool(in PlayerROV.InputData inInput)
        {
            if (!m_TargetScannable.IsReferenceNull())
            {
                if (!inInput.UseHold || !m_TargetScannable || !m_TargetScannable.isActiveAndEnabled || m_TargetScannable.ScanId != m_TargetScanId || !m_TargetScannable.InRange)
                {
                    CancelScan();
                    return false;
                }

                return true;
            }
            else
            {
                if (inInput.UseHold && inInput.Target.HasValue)
                {
                    int overlappingColliders = Physics2D.OverlapCircleNonAlloc(inInput.Target.Value, m_ScanRange, m_ColliderBuffer, GameLayers.Scannable_Mask);
                    Collider2D scannableCollider = overlappingColliders > 0 ? m_ColliderBuffer[0] : null;
                    Array.Clear(m_ColliderBuffer, 0, overlappingColliders);

                    bool bFound = false;
                    if (scannableCollider != null)
                    {
                        ScannableRegion scannable = scannableCollider.GetComponentInParent<ScannableRegion>();
                        if (scannable != null && scannable.isActiveAndEnabled && scannable.InRange)
                        {
                            bFound = true;
                            StartScan(scannable, inInput.Target.Value);
                            return true;
                        }
                    }
                    if (!bFound)
                    {
                        Services.UI.FindPanel<ScannerDisplay>().Hide();
                    }
                }

                return false;
            }
        }

        public bool HasTarget()
        {
            return m_TargetScannable != null;
        }

        public Vector3? GetTargetPosition()
        {
            if (m_TargetScannable != null && m_TargetScannable.isActiveAndEnabled)
            {
                return m_TargetScannable.Collider.transform.position;
            }

            return null;
        }

        private void StartScan(ScannableRegion inRegion, Vector2 inStartPos)
        {
            CancelScan();

            if (inRegion != null)
            {
                m_TargetScannable = inRegion;
                m_TargetScanId = inRegion.ScanId;

                m_ScanRoutine.Replace(this, ScanRoutine());

                Services.Audio.PostEvent("scan_start");
            }
        }

        public void CancelScan()
        {
            if (!m_TargetScannable.IsReferenceNull())
            {
                Services.UI.FindPanel<ScannerDisplay>().CancelIfProgress();

                if (m_TargetScannable.CurrentIcon)
                {
                    m_TargetScannable.CurrentIcon.SetFill(0);
                    m_TargetScannable.CurrentIcon.SetSpinning(false);
                }

                m_TargetScannable = null;
                m_TargetScanId = null;

                m_ScanRoutine.Stop();
            }
        }

        private IEnumerator ScanRoutine()
        {
            var mgr = ScanSystem.Find<ScanSystem>();
            var scanUI = Services.UI.FindPanel<ScannerDisplay>();

            ScanData data;
            mgr.TryGetScanData(m_TargetScanId, out data);

            float progress = 0;
            float duration = mgr.GetScanDuration(data);
            float increment = 1f / duration;

            scanUI.AdjustForScannableVisibility(m_TargetScannable.Collider.transform.position, gameObject.transform.parent.position);

            m_TargetScannable.CurrentIcon.SetSpinning(true);

            while(progress < 1 && m_TargetScannable.InRange)
            {
                progress += increment * Routine.DeltaTime;
                if (progress > 1)
                    progress = 1;
                scanUI.ShowProgress(progress);
                m_TargetScannable.CurrentIcon.SetFill(progress);
                yield return null;
            }

            m_TargetScannable.CurrentIcon.SetSpinning(false);

            ScanResult result = mgr.RegisterScanned(data);
            if (result != 0)
            {
                if (data != null && !data.BestiaryId().IsEmpty)
                {
                    Services.Audio.PostEvent("scan_bestiary");
                }
                if (data != null && !data.LogbookId().IsEmpty)
                {
                    Services.Audio.PostEvent("scan_logbook");
                }
                else
                {
                    Services.Audio.PostEvent("scan_complete");
                }
            }
            scanUI.ShowScan(data, result);
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