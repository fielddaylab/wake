using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;

namespace ProtoAqua.Observation
{
    public class ScannableRegion : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private string m_ScanId = null;
        
        [Header("Collisions")]
        [SerializeField] private Collider2D m_Collider = null;

        [Header("Transforms")]
        [SerializeField] private Transform m_RootTransform = null;
        [SerializeField] private Transform m_StateTransform = null;
        [SerializeField] private ColorGroup m_StateGroup = null;

        [Header("Icon")]
        [SerializeField] private Transform m_IconTransform = null;
        [SerializeField] private ColorGroup m_IconGroup = null;
        [SerializeField] private Transform m_Icon = null;
        [SerializeField] private ColorGroup m_Progress = null;

        #endregion // Inspector

        [NonSerialized] private Routine m_TickRoutine;
        [NonSerialized] private TriggerListener2D m_ScanRadiusListener;
        
        [NonSerialized] private Routine m_RootAnim;
        [NonSerialized] private Routine m_ScanAnim;

        [NonSerialized] private bool m_ScannerOn;
        [NonSerialized] private bool m_Showing;
        [NonSerialized] private bool m_ScanFinished;

        [NonSerialized] private ScanData m_ScanData;

        #region Unity Events

        private void Awake()
        {
            m_ScanRadiusListener = m_Collider.gameObject.AddComponent<TriggerListener2D>();
            m_ScanRadiusListener.TagFilter.Add("ScanRadius");
            m_ScanRadiusListener.onTriggerEnter.AddListener(OnRadiusEnter);
            m_ScanRadiusListener.onTriggerExit.AddListener(OnRadiusExit);

            m_RootTransform.gameObject.SetActive(false);
            m_Collider.gameObject.SetActive(false);

            m_Progress.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            m_TickRoutine = Routine.StartLoop(this, Tick).SetPhase(RoutinePhase.LateUpdate);

            Services.Events.Register(ObservationEvents.ScannerOn, OnScannerOn, this)
                .Register(ObservationEvents.ScannerOff, OnScannerOff, this)
                .Register(ObservationEvents.ScannableComplete, OnScanComplete, this);

            UpdateData();
        }

        private void OnDisable()
        {
            m_TickRoutine.Stop();

            Services.Events?.Deregister(ObservationEvents.ScannerOn, OnScannerOn)
                .Deregister(ObservationEvents.ScannerOff, OnScannerOff);
        }

        #endregion // Unity Events

        #region Scanning

        public string ScanId() { return m_ScanId; }
        public ScanData ScanData() { return m_ScanData; }

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

        private void UpdateData()
        {
            var mgr = Services.Tweaks.Get<ScanDataMgr>();
            ScanDataFlags flags;
            if (string.IsNullOrEmpty(m_ScanId) || !mgr.TryGetScanData(m_ScanId, out m_ScanData))
            {
                Debug.LogWarningFormat("[ScannableRegion] Unable to locate ScanData with id '{0}'", m_ScanId);
                m_ScanFinished = false;
                flags = 0;
            }
            else
            {
                m_ScanFinished = mgr.WasScanned(m_ScanId);
                flags = m_ScanData.Flags();
            }

            UpdateColor();
        }

        public void StartScan()
        {
            m_Progress.transform.SetScale(0);
            m_Progress.gameObject.SetActive(true);

            m_StateTransform.SetScale(1.1f);
        }

        public void UpdateScan(float inProgress)
        {
            m_Progress.SetAlpha(inProgress);
            m_Progress.transform.SetScale(inProgress);
        }

        public void CancelScan()
        {
            m_Progress.transform.SetScale(0);
            m_Progress.gameObject.SetActive(false);

            m_StateTransform.SetScale(1f);
        }

        public bool CompleteScan()
        {
            if (!m_ScanFinished)
            {
                m_ScanFinished = true;

                var mgr = Services.Tweaks.Get<ScanDataMgr>();
                if (m_ScanData != null)
                {
                    mgr.RegisterScanned(m_ScanData);
                }

                UpdateColor();

                return true;
            }

            return false;
        }

        #endregion // Scanning

        #region State

        private void Tick()
        {
            m_ScanRadiusListener.ProcessOccupants();
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
            m_RootAnim.Replace(this, ShowAnim());
        }

        private void Hide()
        {
            if (!m_Showing)
                return;

            m_Showing = false;
            m_RootAnim.Replace(this, HideAnim());
        }

        private void UpdateColor()
        {
            var mgr = Services.Tweaks.Get<ScanDataMgr>();
            var config = mgr.GetConfig(m_ScanData != null ? m_ScanData.Flags() : 0);
            m_IconGroup.Color = m_ScanFinished ? config.Node.ScannedColor : config.Node.UnscannedColor;
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
            StringHash scanId = (StringHash) inScanId;
            if (m_ScanId == scanId)
            {

            }
        }

        #endregion // Handlers
    }
}