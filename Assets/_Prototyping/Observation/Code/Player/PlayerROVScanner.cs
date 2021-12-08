using System;
using UnityEngine;
using BeauUtil;
using BeauRoutine;
using System.Collections;
using Aqua;
using BeauPools;

namespace ProtoAqua.Observation
{
    public class PlayerROVScanner : MonoBehaviour, PlayerROV.ITool
    {
        private const ScanResult PopupResultMask = ScanResult.NewBestiary | ScanResult.NewLogbook | ScanResult.NewFacts;

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

        [NonSerialized] private Collider2D[] m_ColliderBuffer = new Collider2D[8];

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
                    Vector2 mousePos = inInput.Mouse.Target.Value;
                    int overlappingColliders = Physics2D.OverlapCircleNonAlloc(mousePos, m_ScanRange, m_ColliderBuffer, GameLayers.Scannable_Mask);
                    Collider2D closest = ScoringUtils.GetMinElement(m_ColliderBuffer, 0, overlappingColliders, (c) => {
                        Vector3 pos = c.transform.position;
                        return Vector2.SqrMagnitude((Vector2) pos - mousePos) + pos.z;
                    });
                    Array.Clear(m_ColliderBuffer, 0, overlappingColliders);

                    bool bFound = false;
                    if (closest != null)
                    {
                        ScannableRegion scannable = closest.GetComponentInParent<ScannableRegion>();
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

            ScanResult result;

            using(PooledList<StringHash32> newFactIds = PooledList<StringHash32>.Create())
            using(PooledList<BFBase> newFacts = PooledList<BFBase>.Create())
            {
                result = m_ScanSystem.RegisterScanned(data, newFactIds);
                if (result != 0)
                {
                    foreach(var id in newFactIds)
                    {
                        newFacts.Add(Assets.Fact(id));
                    }

                    if ((result & ScanResult.NewBestiary) != 0)
                    {
                        Services.Audio.PostEvent("scan_bestiary");
                        var bestiary = Assets.Bestiary(data.BestiaryId());
                        Script.PopupNewEntity(bestiary, data.Text(), newFacts);
                    }
                    else if ((result & ScanResult.NewFacts) != 0)
                    {
                        var bestiary = Assets.Bestiary(data.BestiaryId());
                        Script.PopupNewFacts(newFacts, default, bestiary);
                    }
                    else if ((result & ScanResult.NewLogbook) != 0)
                    {
                        Services.Audio.PostEvent("scan_logbook");
                    }
                    else if ((result & ScanResult.NewScan) != 0)
                    {
                        Services.Audio.PostEvent("scan_complete");
                    }
                }
            }

            ScanDataFlags flags = data.Flags();
            if ((flags & ScanDataFlags.ActivateTool) != 0)
            {
                m_CurrentToolView = m_TargetScannable.ToolView;
                ShowTool(transform.parent.position, m_TargetScannable.transform.position, m_CurrentToolView);
            }

            if ((result & PopupResultMask) == 0)
            {
                scanUI.ShowScan(data, result);
            }
            else
            {
                scanUI.Hide();
            }
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