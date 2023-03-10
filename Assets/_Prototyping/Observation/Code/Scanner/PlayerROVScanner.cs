using System;
using UnityEngine;
using BeauUtil;
using BeauRoutine;
using System.Collections;
using Aqua;
using BeauPools;
using Aqua.Entity;
using Aqua.Character;

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

        [Header("Movement Scaling")]
        [SerializeField] private float m_ScanReduceMovementSpeedThreshold = 3;
        [SerializeField] private float m_ScanReduceMovementSpeedMaxMultiplier = 0.6f;
        [SerializeField] private float m_PowerEngineScanDragIncrease = 2;

        #endregion // Inspector

        [NonSerialized] private PlayerBody m_Body;
        [NonSerialized] private ScanSystem m_ScanSystem;
        [NonSerialized] private ScannerDisplay m_ScanDisplay;
        [NonSerialized] private bool m_ScannerOn = false;
        [NonSerialized] private ScannableRegion m_TargetScannable = null;
        [NonSerialized] private ScanData m_TargetScanData = null;
        [NonSerialized] private float m_CurrentRange;
        [NonSerialized] private float m_LastKnownSpeed;

        [NonSerialized] private Routine m_ScanEnableRoutine;
        [NonSerialized] private Routine m_ScanRoutine;
        [NonSerialized] private float m_AdditionalDrag;
        [NonSerialized] private float m_ScanFreeze;

        [NonSerialized] private readonly Collider2D[] m_ColliderBuffer = new Collider2D[16];

        #region Unity Events

        private void Start()
        {
            SetRange(0);
            m_ScanSystem = ScanSystem.Find<ScanSystem>();
            m_ScanSystem.SetDetector(m_RangeCollider);
            m_ScanDisplay = Services.UI.FindPanel<ScannerDisplay>();
        }

        #endregion // Unity Events

        #region State

        public bool IsEnabled()
        {
            return m_ScannerOn;
        }

        public void Enable(PlayerBody inBody)
        {
            if (m_ScannerOn)
                return;

            m_ScannerOn = true;
            m_ScanEnableRoutine.Replace(this, TurnOnAnim());
            Visual2DSystem.Activate(GameLayers.Scannable_Mask);
            m_Body = inBody;
        }

        public void Disable()
        {
            if (!m_ScannerOn)
                return;

            CancelScan();
            m_ScanFreeze = 0;
            m_ScanDisplay?.Hide();
            m_ScannerOn = false;
            m_ScanEnableRoutine.Replace(this, TurnOffAnim());
            Visual2DSystem.Deactivate(GameLayers.Scannable_Mask);
        }

        #endregion // State

        #region Scanning

        public bool UpdateTool(float inDeltaTime, in PlayerROVInput.InputData inInput, Vector2 inVelocity, PlayerBody inBody)
        {
            m_LastKnownSpeed = inVelocity.magnitude;

            if (!m_TargetScannable.IsReferenceNull())
            {
                if (!inInput.UseHold)
                {
                    CancelScan();
                }
                else if (!m_TargetScannable || !m_TargetScannable.isActiveAndEnabled || m_TargetScannable.ScanData != m_TargetScanData || !m_TargetScannable.CanScan)
                {
                    CancelScan();
                    return false;
                }

                return true;
            }
            else
            {
                if (m_ScanFreeze > 0)
                {
                    return false;
                }

                if (inInput.UseHold && inInput.Mouse.Target.HasValue)
                {
                    Vector2 mousePos = inInput.Mouse.Target.Value;
                    Collider2D microscope = Physics2D.OverlapCircle(mousePos, m_ScanRange, GameLayers.ScanClickBlock_Mask);
                    bool inMicroscope = microscope;
                    int overlappingColliders = Physics2D.OverlapCircleNonAlloc(mousePos, m_ScanRange, m_ColliderBuffer, GameLayers.ScanClick_Mask);

                    ScannableRegion closestScan = null, scan;
                    float minScore = float.MaxValue, score;
                    Vector3 pos;
                    bool blockedByMicroscope;
                    for(int i = 0; i < overlappingColliders; i++) {
                        scan = m_ColliderBuffer[i].GetComponentInParent<ScannableRegion>();

                        blockedByMicroscope = scan && inMicroscope && !scan.InMicroscope;
                        if (!scan || !scan.isActiveAndEnabled || !scan.CanScan || blockedByMicroscope)  {
                            continue;
                        }

                        pos = m_ColliderBuffer[i].transform.position;
                        score = Vector2.SqrMagnitude((Vector2) pos - mousePos) + pos.z;

                        if (score < minScore) {
                            minScore = score;
                            closestScan = scan;
                        }
                    }

                    Array.Clear(m_ColliderBuffer, 0, overlappingColliders);

                    if (closestScan != null)
                    {
                        StartScan(closestScan, inInput.Mouse.Target.Value);
                        return true;
                    }

                    m_ScanDisplay.Hide();
                }

                if (inInput.Move)
                {
                    m_ScanDisplay.Hide();
                }

                return false;
            }
        }

        public void UpdateActive(float inDeltaTime, in PlayerROVInput.InputData inInput, Vector2 inVelocity, PlayerBody inBody) {
            if (m_ScanFreeze > 0) {
                m_ScanFreeze = Math.Max(0, m_ScanFreeze - inDeltaTime);
            }
        }

        public bool HasTarget()
        {
            return m_TargetScannable != null;
        }

        public PlayerROVAnimationFlags AnimFlags() {
            return 0;
        }

        public float MoveSpeedMultiplier() { return 1; }

        public void GetTargetPosition(bool inbOnGamePlane, out Vector3? outWorld, out Vector3? outCursor) {
            outWorld = outCursor = null;

            if (m_TargetScannable != null && m_TargetScannable.isActiveAndEnabled) {
                if (inbOnGamePlane)
                    outWorld = m_TargetScannable.Collider.transform.position;
                else
                    outWorld = m_TargetScannable.TrackTransform.position;

                outCursor = m_TargetScannable.Click.transform.position;
            }
        }

        private void StartScan(ScannableRegion inRegion, Vector2 inStartPos)
        {
            CancelScan();

            if (inRegion != null)
            {
                m_TargetScannable = inRegion;
                m_TargetScanData = m_ScanSystem.RefreshData(inRegion);

                m_ScanRoutine.Replace(this, ScanRoutine()).Tick();

                Services.Audio.PostEvent("scan_start");

                if ((m_Body.BodyStatus & PlayerBodyStatus.PowerEngineEngaged) != 0) {
                    m_AdditionalDrag = m_PowerEngineScanDragIncrease;
                    m_Body.Kinematics.AdditionalDrag += m_AdditionalDrag;
                }

                if (m_TargetScanData != null) {
                    m_TargetScannable.OnScanStart?.Invoke(m_ScanSystem.WasScanned(m_TargetScanData.Id()));
                } else {
                    m_TargetScannable.OnScanStart?.Invoke(false);
                }
            }
        }

        public void CancelScan()
        {
            if (!m_TargetScannable.IsReferenceNull())
            {
                m_ScanDisplay.CancelIfProgress();

                if (m_TargetScannable.CurrentIcon)
                {
                    m_TargetScannable.CurrentIcon.SetFill(0);
                    m_TargetScannable.CurrentIcon.SetSpinning(false);
                }

                m_TargetScannable = null;
                m_TargetScanData = null;

                m_ScanRoutine.Stop();

                m_Body.Kinematics.AdditionalDrag -= m_AdditionalDrag;
                m_AdditionalDrag = 0;
            }
        }

        private IEnumerator ScanRoutine()
        {
            ScanData data = m_TargetScanData;

            float progress = 0;
            float duration = m_ScanSystem.GetScanDuration(data);
            float increment = 1f / duration;

            m_TargetScannable.CurrentIcon.SetSpinning(true);

            while(progress < 1 && m_TargetScannable.CanScan)
            {
                progress += increment * Routine.DeltaTime * (1 - Mathf.Min(m_LastKnownSpeed / m_ScanReduceMovementSpeedThreshold, m_ScanReduceMovementSpeedMaxMultiplier));
                if (progress > 1)
                    progress = 1;
                m_ScanDisplay.ShowProgress(progress);
                m_TargetScannable.CurrentIcon.SetFill(progress);
                yield return null;
            }

            if (!m_TargetScannable.CanScan)
                yield break;

            m_TargetScannable.CurrentIcon.SetSpinning(false);

            ScanResult result;
            ScannableRegion region = m_TargetScannable;

            using(PooledList<StringHash32> newFactIds = PooledList<StringHash32>.Create())
            using(PooledList<BFBase> newFacts = PooledList<BFBase>.Create())
            {
                result = m_ScanSystem.RegisterScanned(data, newFactIds, region);
                region.OnScanComplete?.Invoke(result);
                
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
                        Script.PopupNewEntity(bestiary, data.Text(), newFacts, region.ScanImageOverride);
                    }
                    else if ((result & ScanResult.NewFacts) != 0)
                    {
                        var bestiary = Assets.Bestiary(data.BestiaryId());
                        Script.PopupNewFacts(newFacts, default, bestiary, data.Text());
                    }
                    else
                    {
                        Services.Audio.PostEvent("scan_complete");
                    }
                }
                else
                {
                    Services.Audio.PostEvent("scan_complete");
                }
            }

            if ((data.Flags() & ScanDataFlags.DoNotShow) == 0 && (result & PopupResultMask) == 0 && !Services.Script.IsCutscene())
            {
                m_ScanFreeze = data.FreezeDisplay();
                m_ScanDisplay.ShowScan(data, region.ScanImageOverride, result);
            }
            else
            {
                m_ScanDisplay.Hide();
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
    }
}