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

        [NonSerialized] private ScanSystem m_ScanSystem;
        [NonSerialized] private bool m_ScannerOn = false;
        [NonSerialized] private ScannableRegion m_TargetScannable = null;
        [NonSerialized] private ToolView m_CurrentToolView = null;
        [NonSerialized] private ScanData m_TargetScanData = null;
        [NonSerialized] private float m_CurrentRange;

        [NonSerialized] private Routine m_ScanEnableRoutine;
        [NonSerialized] private Routine m_ScanRoutine;

        [NonSerialized] private Collider2D[] m_ColliderBuffer = new Collider2D[1];

        #region Unity Events

        private void Start()
        {
            SetRange(0);
            m_ScanSystem = ScanSystem.Find<ScanSystem>();
            m_ScanSystem.SetDetector(m_RangeCollider);
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
            HideCurrentToolView();
            Services.UI?.FindPanel<ScannerDisplay>()?.Hide();
            m_ScannerOn = false;
            m_ScanEnableRoutine.Replace(this, TurnOffAnim());
        }

        #endregion // State

        #region Scanning

        public bool UpdateTool(in PlayerROVInput.InputData inInput)
        {
            if (!m_TargetScannable.IsReferenceNull())
            {
                if (!inInput.UseHold)
                {
                    CancelScan();
                }
                else if (!m_TargetScannable || !m_TargetScannable.isActiveAndEnabled || m_TargetScannable.ScanData != m_TargetScanData || !m_TargetScannable.InRange)
                {
                    CancelScan();
                    HideCurrentToolView();
                    return false;
                }

                return true;
            }
            else
            {
                if (inInput.UseHold && inInput.Mouse.Target.HasValue)
                {
                    int overlappingColliders = Physics2D.OverlapCircleNonAlloc(inInput.Mouse.Target.Value, m_ScanRange, m_ColliderBuffer, GameLayers.Scannable_Mask);
                    Collider2D scannableCollider = overlappingColliders > 0 ? m_ColliderBuffer[0] : null;
                    Array.Clear(m_ColliderBuffer, 0, overlappingColliders);

                    bool bFound = false;
                    if (scannableCollider != null)
                    {
                        ScannableRegion scannable = scannableCollider.GetComponentInParent<ScannableRegion>();
                        if (scannable != null && scannable.isActiveAndEnabled && scannable.InRange)
                        {
                            bFound = true;
                            StartScan(scannable, inInput.Mouse.Target.Value);
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
                if (!inRegion.InsideToolView && inRegion.ToolView != m_CurrentToolView)
                    HideCurrentToolView();

                m_TargetScannable = inRegion;
                m_TargetScanData = m_ScanSystem.RefreshData(inRegion);

                m_ScanRoutine.Replace(this, ScanRoutine()).TryManuallyUpdate(0);

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
                m_TargetScanData = null;

                m_ScanRoutine.Stop();
            }
        }

        private IEnumerator ScanRoutine()
        {
            var scanUI = Services.UI.FindPanel<ScannerDisplay>();

            ScanData data = m_TargetScanData;

            float progress = 0;
            float duration = m_ScanSystem.GetScanDuration(data);
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

            if (!m_TargetScannable.InRange)
                yield break;

            m_TargetScannable.CurrentIcon.SetSpinning(false);

            ScanResult result = m_ScanSystem.RegisterScanned(data);
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

            ScanDataFlags flags = data.Flags();
            if ((flags & ScanDataFlags.ActivateTool) != 0)
            {
                m_CurrentToolView = m_TargetScannable.ToolView;
                ShowTool(transform.parent.position, m_TargetScannable.transform.position, m_CurrentToolView);
            }

            scanUI.ShowScan(data, result);
        }

        public void HideCurrentToolView()
        {
            if (m_CurrentToolView != null)
            {
                HideTool(m_CurrentToolView);
                m_CurrentToolView = null;
            }
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
    
        #region Tools

        static private void ShowTool(Vector3 inPlayerPos, Vector3 inScannablePosition, ToolView inToolView)
        {
            switch(inToolView.Placement)
            {
                case ToolView.PlacementMode.Fixed:
                    {
                        break;
                    }

                case ToolView.PlacementMode.AwayFromPlayer:
                    {
                        Vector3 delta = inScannablePosition - inPlayerPos;
                        delta.z = 0;
                        delta.Normalize();

                        if (delta.x == 0 && delta.y == 0)
                            delta.x = 1;

                        inToolView.Root.position = inScannablePosition + delta * inToolView.DistanceAway;
                        break;
                    }
            }

            inToolView.Root.gameObject.SetActive(true);
            inToolView.Animation.Replace(inToolView, ShowToolAnimation(inToolView)).TryManuallyUpdate(0);
        }

        static private IEnumerator ShowToolAnimation(ToolView inTool)
        {
            inTool.Root.SetScale(0);
            yield return inTool.Root.ScaleTo(1, 0.2f).Ease(Curve.BackOut);
        }

        static private void HideTool(ToolView inToolView)
        {
            if (inToolView.Root.gameObject.activeSelf)
            {
                inToolView.Animation.Replace(inToolView, HideToolAnimation(inToolView)).TryManuallyUpdate(0);
            }
        }

        static private IEnumerator HideToolAnimation(ToolView inTool)
        {
            if (inTool.Root.gameObject.activeSelf)
            {
                yield return inTool.Root.ScaleTo(0, 0.2f).Ease(Curve.BackIn);
                inTool.Root.gameObject.SetActive(false);
            }
        }

        #endregion // Tools
    }
}