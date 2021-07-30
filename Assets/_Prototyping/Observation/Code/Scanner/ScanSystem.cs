using System;
using UnityEngine;
using BeauRoutine;
using System.Collections;
using UnityEngine.UI;
using Aqua;
using BeauUtil;
using BeauPools;
using System.Collections.Generic;
using Aqua.Debugging;
using Aqua.Cameras;

namespace ProtoAqua.Observation
{
    [DefaultExecutionOrder(-100)]
    public class ScanSystem : SharedManager
    {
        #region Types

        [Serializable] private class ScanIconPool : SerializablePool<ScanIcon> { }

        [Serializable]
        public class ScanTypeConfig
        {
            public Color BackgroundColor = ColorBank.White;
            public Color HeaderColor = ColorBank.Yellow;
            public Color TextColor = ColorBank.White;

            public SerializedHash32 OpenSound;
            public ScanNodeConfig NodeConfig;
        }

        [Serializable]
        public class ScanNodeConfig
        {
            public Color UnscannedLineColor;
            public Color UnscannedFillColor;
            [Space]
            public Color ScannedLineColor;
            public Color ScannedFillColor;
        }

        #endregion // Types

        #region Inspector

        [SerializeField] private ScanIconPool m_IconPool = null;

        [Space]
        [SerializeField] private ScanDataPackage[] m_DefaultScanAssets = null;

        [Header("Scan Colors")]
        [SerializeField] private ScanTypeConfig m_DefaultScanConfig = null;
        [SerializeField] private ScanTypeConfig m_ImportantScanConfig = null;

        [Header("Scan Durations")]
        [SerializeField] private float m_BaseScanDuration = 0.75f;
        [SerializeField] private float m_CompletedScanDuration = 0.2f;

        #endregion // Inspector

        private readonly HashSet<ScanDataPackage> m_LoadedPackages = new HashSet<ScanDataPackage>();
        private readonly Dictionary<StringHash32, ScanData> m_ScanDataMap = new Dictionary<StringHash32, ScanData>();

        private readonly RingBuffer<ScannableRegion> m_AllRegions = new RingBuffer<ScannableRegion>(64, RingBufferMode.Expand);
        private readonly RingBuffer<ScannableRegion> m_RegionsInRange = new RingBuffer<ScannableRegion>(16, RingBufferMode.Expand);

        [NonSerialized] private Collider2D m_Range;
        [NonSerialized] private TriggerListener2D m_Listener;

        #region Events

        protected override void Awake()
        {
            base.Awake();

            foreach(var asset in m_DefaultScanAssets)
            {
                Load(asset);
            }
        }

        protected override void OnDestroy()
        {
            foreach(var package in m_LoadedPackages)
            {
                package.Clear();
            }
            m_LoadedPackages.Clear();
            m_ScanDataMap.Clear();

            base.OnDestroy();
        }

        private void LateUpdate()
        {
            if (Services.Pause.IsPaused() || Services.State.IsLoadingScene())
                return;

            if (m_Listener == null || !m_Listener.isActiveAndEnabled)
                return;
            
            m_Listener.ProcessOccupants();

            CameraService cameraService = Services.Camera;
            ScannableRegion region;
            for(int i = m_AllRegions.Count - 1; i >= 0; i--)
            {
                region = m_AllRegions[i];
                region.Collider.transform.position = cameraService.GameplayPlanePosition(region.transform);
            }

            for(int i = m_RegionsInRange.Count - 1; i >= 0; i--)
            {
                region = m_RegionsInRange[i];
                region.CurrentIcon.transform.position = region.transform.position;
            }
        }

        #endregion // Events

        #region Scan Data Packages

        public void Load(ScanDataPackage inPackage)
        {
            if (m_LoadedPackages.Add(inPackage))
            {
                inPackage.Parse(ScanDataPackage.Generator.Instance);
                AddPackage(inPackage);
            }
        }

        public void Unload(ScanDataPackage inPackage)
        {
            if (m_LoadedPackages.Remove(inPackage))
            {
                RemovePackage(inPackage);
                inPackage.Clear();
            }
        }

        private void AddPackage(ScanDataPackage inPackage)
        {
            foreach(var node in inPackage)
            {
                m_ScanDataMap.Add(node.Id(), node);
            }

            DebugService.Log(LogMask.Observation | LogMask.Loading, "[ScanSystem] Loaded scan data package '{0}' with {1} nodes", inPackage.name, inPackage.Count);
        }

        private void RemovePackage(ScanDataPackage inPackage)
        {
            foreach(var node in inPackage)
            {
                m_ScanDataMap.Remove(node.Id());
            }
            
            DebugService.Log(LogMask.Observation | LogMask.Loading, "[ScanSystem] Unloaded scan data package '{0}'", inPackage.name);
        }

        #endregion // Scan Data Packages

        #region ScanData

        public bool TryGetScanData(StringHash32 inId, out ScanData outData)
        {
            return m_ScanDataMap.TryGetValue(inId, out outData);
        }

        public bool WasScanned(StringHash32 inId) { return Services.Data.Profile.Inventory.WasScanned(inId); }

        public ScanResult RegisterScanned(ScanData inData)
        {
            if (Services.Data.Profile.Inventory.RegisterScanned(inData.Id()))
            {
                ScanResult result = ScanResult.NewScan;

                StringHash32 bestiaryId = inData.BestiaryId();
                if (!bestiaryId.IsEmpty && Services.Data.Profile.Bestiary.RegisterEntity(bestiaryId))
                {
                    result |= ScanResult.NewBestiary;
                }

                // TODO: Logbook

                foreach(var factId in inData.FactIds())
                {
                    if (Services.Data.Profile.Bestiary.RegisterFact(factId, false))
                    {
                        result |= ScanResult.NewBestiary;
                    }
                }

                var config = GetConfig(inData.Flags());
                foreach(var scannable in m_RegionsInRange)
                {
                    if (scannable.ScanId == inData.Id())
                    {
                        scannable.CurrentIcon.SetColor(config.NodeConfig.ScannedLineColor, config.NodeConfig.ScannedFillColor);
                    }
                }
                
                return result;
            }

            return ScanResult.NoChange;
        }

        public ScanTypeConfig GetConfig(ScanDataFlags inFlags)
        {
            if ((inFlags & ScanDataFlags.Important) != 0)
                return m_ImportantScanConfig;

            return m_DefaultScanConfig;
        }

        public float GetScanDuration(ScanData inData)
        {
            if (inData == null)
                return m_BaseScanDuration;

            if (WasScanned(inData.Id()))
                return m_CompletedScanDuration;
            
            return m_BaseScanDuration * inData.ScanSpeed();
        }

        #endregion // ScanData

        #region Scannable Regions

        public void Register(ScannableRegion inRegion)
        {
            m_AllRegions.PushBack(inRegion);
            TryGetScanData(inRegion.ScanId, out inRegion.ScanData);
        }

        public void Deregister(ScannableRegion inRegion)
        {
            m_AllRegions.FastRemove(inRegion);
            if (m_RegionsInRange.FastRemove(inRegion))
            {
                inRegion.InRange = false;
                inRegion.CurrentIcon.Hide();
                inRegion.CurrentIcon = null;
            }
        }

        #endregion // Scannable Regions
    
        #region Scan Range

        public void SetDetector(Collider2D inCollider)
        {
            if (m_Range == inCollider)
                return;

            if (m_Listener != null)
            {
                m_Listener.onTriggerEnter.RemoveListener(OnScannableEnterRegion);
                m_Listener.onTriggerExit.RemoveListener(OnScannableExitRegion);
            }

            m_Range = inCollider;

            if (inCollider != null)
            {
                m_Listener = inCollider.EnsureComponent<TriggerListener2D>();
                m_Listener.LayerFilter = GameLayers.Scannable_Mask;
                m_Listener.SetOccupantTracking(true);

                m_Listener.onTriggerEnter.AddListener(OnScannableEnterRegion);
                m_Listener.onTriggerExit.AddListener(OnScannableExitRegion);
            }
            else
            {
                m_Listener = null;
            }
        }

        #endregion // Scan Range

        #region Callbacks

        private void OnScannableEnterRegion(Collider2D inCollider)
        {
            ScannableRegion region = inCollider.GetComponentInParent<ScannableRegion>();
            if (region != null)
            {
                m_RegionsInRange.PushBack(region);
                region.CurrentIcon = m_IconPool.Alloc();
                region.CurrentIcon.Show();
                region.InRange = true;

                var config = GetConfig(region.ScanData == null ? 0 : region.ScanData.Flags());
                if (WasScanned(region.ScanId))
                {
                    region.CurrentIcon.SetColor(config.NodeConfig.ScannedLineColor, config.NodeConfig.ScannedFillColor);
                }
                else
                {
                    region.CurrentIcon.SetColor(config.NodeConfig.UnscannedLineColor, config.NodeConfig.UnscannedFillColor);
                }
            }
        }

        private void OnScannableExitRegion(Collider2D inCollider)
        {
            if (!inCollider)
                return;
            
            ScannableRegion region = inCollider.GetComponentInParent<ScannableRegion>();
            if (region != null)
            {
                m_RegionsInRange.FastRemove(region);
                region.CurrentIcon.Hide();
                region.CurrentIcon = null;
                region.InRange = false;
            }
        }

        #endregion // Callbacks
    }

    [Flags]
    public enum ScanResult : byte
    {
        NoChange =  0x0,
        NewScan =   0x1,
        NewLogbook = 0x2,
        NewBestiary = 0x4
    }
}