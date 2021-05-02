using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using BeauPools;
using Aqua;

namespace ProtoAqua.Observation
{
    public class ScannableRegion : MonoBehaviour, IPoolAllocHandler
    {
        #region Inspector

        [SerializeField] private string m_ScanId = null;
        [SerializeField] private Transform m_AttachedTo = null;
        [SerializeField] private bool m_LockToCursor = false;
        
        [Header("Collisions")]
        [SerializeField, Required] private Collider2D m_Collider = null;

        [Header("Transforms")]
        [SerializeField, HideIfField("m_LockToCursor")] private Transform m_RootTransform = null;
        [SerializeField, HideIfField("m_LockToCursor")] private Transform m_StateTransform = null;

        [Header("Icon")]
        [SerializeField, HideIfField("m_LockToCursor")] private ColorGroup m_IconGroup = null;
        [SerializeField, HideIfField("m_LockToCursor")] private ColorGroup m_Progress = null;

        #endregion // Inspector

        [NonSerialized] private Routine m_TickRoutine;
        [NonSerialized] private TriggerListener2D m_ScanRadiusListener;
        
        [NonSerialized] private Routine m_RootAnim;
        [NonSerialized] private Routine m_ScanAnim;

        [NonSerialized] private bool m_ScannerOn;
        [NonSerialized] private bool m_Showing;
        [NonSerialized] private bool m_ScanFinished;
        [NonSerialized] private float m_ProgressScale;

        #region Unity Events

        private void Awake()
        {
            m_ScanRadiusListener = m_Collider.gameObject.AddComponent<TriggerListener2D>();
            m_ScanRadiusListener.TagFilter.Add("ScanRadius");
            m_ScanRadiusListener.onTriggerEnter.AddListener(OnRadiusEnter);
            m_ScanRadiusListener.onTriggerExit.AddListener(OnRadiusExit);

            m_ProgressScale = m_Progress.transform.localScale.x;

            if (m_RootTransform)
            {
                m_RootTransform.gameObject.SetActive(false);
            }

            m_Collider.gameObject.SetActive(false);

            if (m_Progress)
            {
                m_Progress.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            m_TickRoutine = Routine.StartLoop(this, Tick).SetPhase(RoutinePhase.LateUpdate);

            Services.Events.Register(ObservationEvents.ScannerOn, OnScannerOn, this)
                .Register(ObservationEvents.ScannerOff, OnScannerOff, this)
                .Register(GameEvents.ScanLogUpdated, OnScanComplete, this)
                .Register(GameEvents.SceneLoaded, OnSceneLoad, this);

            // TODO: Fix this so we can detect whether or not the scanner is on when we are enabled

            if (!Services.State.IsLoadingScene())
                UpdateData();
        }

        private void OnDisable()
        {
            m_TickRoutine.Stop();
            m_ScannerOn = false;

            Services.Events?.DeregisterAll(this);
        }

        #endregion // Unity Events

        #region Scanning

        public string ScanId() { return m_ScanId; }

        public bool LockToCursor() { return m_LockToCursor; }

        public bool ChangeScanId(string inScanId)
        {
            if (m_ScanId != inScanId)
            {
                m_ScanId = inScanId;
                UpdateData();
                return true;
            }

            return false;
        }

        public bool CanScan()
        {
            return m_Showing;
        }

        public bool IsCompleted()
        {
            return m_ScanFinished;
        }

        private ScanData GetScanData()
        {
            var mgr = Services.Tweaks.Get<ScanDataMgr>();
            ScanData data = null;
            if (string.IsNullOrEmpty(m_ScanId) || !mgr.TryGetScanData(m_ScanId, out data))
            {
                Debug.LogWarningFormat("[ScannableRegion] Unable to locate ScanData with id '{0}'", m_ScanId);
            }
            return data;
        }

        private void OnSceneLoad()
        {
            UpdateData();
        }

        private ScanData UpdateData()
        {
            var mgr = Services.Tweaks.Get<ScanDataMgr>();
            ScanData data = GetScanData();
            m_ScanFinished = mgr.WasScanned(m_ScanId);
            UpdateColor(data);
            return data;
        }

        public void StartScan()
        {
            if (m_Progress)
            {
                m_Progress.transform.SetScale(0);
                m_Progress.gameObject.SetActive(true);
            }

            if (m_StateTransform)
            {
                m_StateTransform.SetScale(1.1f);
            }
        }

        public void UpdateScan(float inProgress)
        {
            if (m_Progress)
            {
                m_Progress.SetAlpha(inProgress);
                m_Progress.transform.SetScale(inProgress * m_ProgressScale);
            }
        }

        public void CancelScan()
        {
            if (m_Progress)
            {
                m_Progress.transform.SetScale(0);
                m_Progress.gameObject.SetActive(false);
            }

            if (m_StateTransform)
            {
                m_StateTransform.SetScale(1f);
            }
        }

        public ScanResult CompleteScan()
        {
            if (!m_ScanFinished)
            {
                m_ScanFinished = true;

                ScanResult result = ScanResult.NoChange;

                ScanData data = GetScanData();
                var mgr = Services.Tweaks.Get<ScanDataMgr>();
                if (data != null)
                {
                    result = mgr.RegisterScanned(data);
                }

                UpdateColor(data);
                return result;
            }

            return ScanResult.NoChange;
        }

        #endregion // Scanning

        #region State

        private void Tick()
        {
            m_ScanRadiusListener.ProcessOccupants();
            if (m_AttachedTo)
            {
                transform.position = m_AttachedTo.position;
            }
            
            if (m_ScannerOn)
            {
                m_Collider.transform.position = ObservationServices.Camera.GameplayPlanePosition(transform);
            }
        }

        private void Show()
        {
            if (m_Showing)
                return;

            m_Showing = true;
            if (m_RootTransform)
            {
                m_RootAnim.Replace(this, ShowAnim());
            }
        }

        private void Hide()
        {
            if (!m_Showing)
                return;

            m_Showing = false;
            if (m_RootTransform)
            {
                m_RootAnim.Replace(this, HideAnim());
            }
        }

        private void UpdateColor(ScanData inData)
        {
            if (m_IconGroup)
            {
                var mgr = Services.Tweaks.Get<ScanDataMgr>();
                var config = mgr.GetConfig(inData != null ? inData.Flags() : 0);
                m_IconGroup.Color = m_ScanFinished ? config.Node.ScannedColor : config.Node.UnscannedColor;
            }
        }

        #endregion // State
        
        #region Animations

        private IEnumerator ShowAnim()
        {
            if (!m_RootTransform.gameObject.activeSelf)
            {
                m_RootTransform.SetScale(0.5f);
                m_RootTransform.gameObject.SetActive(true);
                m_IconGroup.SetAlpha(0);
            }

            yield return Routine.Combine(
                m_RootTransform.ScaleTo(1, 0.25f).Ease(Curve.BackOut),
                Tween.Float(m_IconGroup.GetAlpha(), 1f, m_IconGroup.SetAlpha, 0.25f)
            );
        }

        private IEnumerator HideAnim()
        {
            yield return Routine.Combine(
                m_RootTransform.ScaleTo(0.5f, 0.25f).Ease(Curve.CubeInOut),
                Tween.Float(m_IconGroup.GetAlpha(), 0f, m_IconGroup.SetAlpha, 0.25f)
            );

            m_RootTransform.gameObject.SetActive(false);
        }

        #endregion // Animations

        #region Handlers

        private void OnRadiusEnter(Collider2D inCollider)
        {
            Show();
        }

        private void OnRadiusExit(Collider2D inCollider)
        {
            Hide();
        }

        private void OnScannerOn()
        {
            m_ScannerOn = true;
            m_Collider.gameObject.SetActive(true);
        }

        private void OnScannerOff()
        {
            m_ScannerOn = false;
            m_Collider.gameObject.SetActive(false);
        }

        private void OnScanComplete(object inScanId)
        {
            if (m_ScanFinished)
                return;

            StringHash32 scanId = (StringHash32) inScanId;
            if (m_ScanId == scanId)
            {
                m_ScanFinished = true;
                ScanData data = GetScanData();
                UpdateColor(data);
            }
        }

        #endregion // Handlers

        #region IPooledObject

        void IPoolAllocHandler.OnAlloc()
        {
        }

        void IPoolAllocHandler.OnFree()
        {
            
        }

        #endregion // IPooledObject
    }
}