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
using Leaf.Runtime;
using BeauUtil.Debugger;
using Aqua.Scripting;

namespace ProtoAqua.Observation
{
    [DefaultExecutionOrder(-100)]
    public class ScanSystem : SharedManager
    {
        static public readonly StringHash32 Trigger_NewScan = "ScannedNewObject";
        static public readonly StringHash32 Trigger_Scan = "ScannedObject";

        #region Types

        [Serializable] private class ScanIconPool : SerializablePool<ScanIcon> { }

        [Serializable]
        public class ScanTypeConfig
        {
            public Color BackgroundColor = ColorBank.White;
            public Color HeaderColor = ColorBank.Yellow;
            public Color TextColor = ColorBank.White;

            public Sprite Icon;
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
        [SerializeField] private ScanTypeConfig m_ToolScanConfig = null;
        [SerializeField] private ScanTypeConfig m_ImportantScanConfig = null;

        [Header("Scan Durations")]
        [SerializeField] private float m_BaseScanDuration = 0.75f;
        [SerializeField] private float m_CompletedScanDuration = 0.2f;

        #endregion // Inspector

        private readonly HashSet<ScanDataPackage> m_LoadedPackages = new HashSet<ScanDataPackage>();
        private readonly Dictionary<StringHash32, ScanData> m_ScanDataMap = new Dictionary<StringHash32, ScanData>();

        private readonly RingBuffer<ScannableRegion> m_AllRegions = new RingBuffer<ScannableRegion>(64, RingBufferMode.Expand);
        private readonly RingBuffer<ScannableRegion> m_RegionsInRange = new RingBuffer<ScannableRegion>(16, RingBufferMode.Expand);

        [NonSerialized] private bool m_Loaded = false;
        [NonSerialized] private Collider2D m_Range;
        [NonSerialized] private TriggerListener2D m_Listener;
        [NonSerialized] private float m_DeactivateRangeSq;
        [NonSerialized] private bool m_ActiveState = false;

        #region Events

        protected override void Awake()
        {
            base.Awake();

            foreach(var asset in m_DefaultScanAssets)
            {
                Load(asset);
            }

            Services.Events.Register(GameEvents.SceneLoaded, OnSceneLoaded, this)
                .Register(GameEvents.BestiaryUpdated, AttemptRefreshAllData, this)
                .Register(GameEvents.InventoryUpdated, AttemptRefreshAllData, this)
                .Register(GameEvents.VariableSet, AttemptRefreshAllData, this);
        }

        protected override void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);

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
            {
                if (m_ActiveState)
                {
                    m_ActiveState = false;
                    DeactivateAllColliders();
                }
                return;
            }
            
            m_ActiveState = true;
            m_Listener.ProcessOccupants();

            CameraService.PlanePositionHelper positionHelper = Services.Camera.GetPositionHelper();
            ScannableRegion region;
            Vector3 gameplayPlanePos;
            Vector3 gameplayPlaneDist;
            Vector2 listenerPos = m_Range.transform.position;
            for(int i = m_AllRegions.Count - 1; i >= 0; i--)
            {
                region = m_AllRegions[i];
                gameplayPlanePos = positionHelper.CastToPlane(region.TrackTransform);
                gameplayPlaneDist = (Vector2) gameplayPlanePos - listenerPos;
                if (gameplayPlaneDist.sqrMagnitude < m_DeactivateRangeSq) {
                    region.Collider.enabled = true;
                    region.Collider.transform.position = gameplayPlanePos;
                } else {
                    region.Collider.enabled = false;
                }
            }

            for(int i = m_RegionsInRange.Count - 1; i >= 0; i--)
            {
                region = m_RegionsInRange[i];
                region.CurrentIcon.transform.position = region.TrackTransform.position;
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
        
        public bool TryGetScanDataWithFallbacks(StringHash32 inId, out ScanData outData)
        {
            StringHash32 id = inId;
            bool bFound = false;
            outData = null;
            
            while(!bFound)
            {
                bFound = m_ScanDataMap.TryGetValue(id, out outData);
                if (outData != null)
                {
                    StringSlice requirement = outData.Requirements();
                    if (!requirement.IsEmpty && !Services.Data.CheckConditions(requirement))
                    {
                        id = outData.FallbackId();
                        bFound = false;
                    }
                }
                else
                {
                    Log.Error("[ScanSystem] Missing Scan with Id '{0}'", id);
                    outData = ScanData.Error;
                    break;
                }
            }

            return bFound;
        }

        public bool WasScanned(StringHash32 inId) { return Save.Inventory.WasScanned(inId); }

        public ScanResult RegisterScanned(ScanData inData, List<StringHash32> outNewFacts)
        {
            ScriptThreadHandle responseHandle = default;
            ScanResult result = ScanResult.NoChange;

            using(var table = TempVarTable.Alloc())
            {
                table.Set("scanId", inData.Id());

                if (Save.Inventory.RegisterScanned(inData.Id()))
                {
                    result |= ScanResult.NewScan;

                    StringHash32 bestiaryId = inData.BestiaryId();
                    if (!bestiaryId.IsEmpty && Save.Bestiary.RegisterEntity(bestiaryId))
                    {
                        result |= ScanResult.NewBestiary;
                    }

                    // TODO: Logbook

                    if (!bestiaryId.IsEmpty && (inData.Flags() & ScanDataFlags.DynamicFactType) != 0)
                    {
                        foreach(var fact in Assets.Bestiary(bestiaryId).FactsOfType(inData.DynamicFactType()))
                        {
                            if (Save.Bestiary.RegisterFact(fact.Id, false))
                            {
                                result |= ScanResult.NewFacts;
                                outNewFacts.Add(fact.Id);
                            }
                        }
                    }

                    foreach(var factId in inData.FactIds())
                    {
                        if (Save.Bestiary.RegisterFact(factId, false))
                        {
                            result |= ScanResult.NewFacts;
                            outNewFacts.Add(factId);
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

                    responseHandle = Services.Script.TriggerResponse(Trigger_NewScan, table);
                }

                if (!responseHandle.IsRunning())
                {
                    responseHandle = Services.Script.TriggerResponse(Trigger_Scan, table);
                }
            }

            return result;
        }

        public ScanTypeConfig GetConfig(ScanDataFlags inFlags)
        {
            if ((inFlags & ScanDataFlags.Important) != 0)
                return m_ImportantScanConfig;
            if ((inFlags & ScanDataFlags.Tool) != 0)
                return m_ToolScanConfig;
            return m_DefaultScanConfig;
        }

        public float GetScanDuration(ScanData inData)
        {
            if (inData == null)
                return m_BaseScanDuration;

            if (WasScanned(inData.Id()))
                return m_CompletedScanDuration;
            
            return m_BaseScanDuration * inData.ScanDuration();
        }

        #endregion // ScanData

        #region Scannable Regions

        public void Register(ScannableRegion inRegion)
        {
            m_AllRegions.PushBack(inRegion);
            if (m_Loaded)
            {
                RefreshData(inRegion);
                if (inRegion.ToolView != null)
                    inRegion.ToolView.gameObject.SetActive(false);

                if (inRegion.InsideToolView)
                    EnterRange(inRegion);
            }
            else
            {
                inRegion.Collider.enabled = false;
            }
        }

        public void Deregister(ScannableRegion inRegion)
        {
            m_AllRegions.FastRemove(inRegion);
            ExitRange(inRegion);
        }

        public ScanData RefreshData(ScannableRegion inRegion)
        {
            TryGetScanDataWithFallbacks(inRegion.ScanId, out inRegion.ScanData);
            if (inRegion.CurrentIcon != null)
            {
                RefreshIcon(inRegion);
            }
            return inRegion.ScanData;
        }

        private void DeactivateAllColliders()
        {
            ScannableRegion region;
            for(int i = 0, len = m_AllRegions.Count; i < len; i++)
            {
                region = m_AllRegions[i];
                region.Collider.enabled = false;
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

                m_DeactivateRangeSq = PhysicsUtils.GetRadius(inCollider) + 1;
                m_DeactivateRangeSq *= m_DeactivateRangeSq;
            }
            else
            {
                m_Listener = null;
            }
        }

        private void EnterRange(ScannableRegion inRegion)
        {
            m_RegionsInRange.PushBack(inRegion);
            inRegion.CurrentIcon = m_IconPool.Alloc();
            inRegion.CurrentIcon.Show();
            inRegion.InRange = true;

            RefreshIcon(inRegion);
        }

        private void ExitRange(ScannableRegion inRegion)
        {
            if (m_RegionsInRange.FastRemove(inRegion))
            {
                inRegion.CurrentIcon.Hide();
                inRegion.CurrentIcon = null;
                inRegion.InRange = false;
            }
        }

        #endregion // Scan Range

        #region Callbacks

        private void OnScannableEnterRegion(Collider2D inCollider)
        {
            ScannableRegion region = inCollider.GetComponentInParent<ScannableRegion>();
            if (region != null && !region.InsideToolView)
            {
                EnterRange(region);
            }
        }

        private void OnScannableExitRegion(Collider2D inCollider)
        {
            if (!inCollider)
                return;
            
            ScannableRegion region = inCollider.GetComponentInParent<ScannableRegion>();
            if (region != null && !region.InsideToolView)
            {
                ExitRange(region);
            }
        }

        private void OnSceneLoaded()
        {
            m_Loaded = true;

            using(PooledList<ToolView> toolViews = PooledList<ToolView>.Create())
            {
                ScannableRegion region;
                for(int i = m_AllRegions.Count - 1; i >= 0; i--)
                {
                    region = m_AllRegions[i];
                    if (region.ToolView)
                        toolViews.Add(region.ToolView);
                }

                foreach(var toolView in toolViews)
                    toolView.gameObject.SetActive(false);
            }

            RefreshAllData();
        }

        private void RefreshAllData()
        {
            for(int i = m_AllRegions.Count - 1; i >= 0; i--)
                RefreshData(m_AllRegions[i]);
        }

        private void AttemptRefreshAllData()
        {
            if (m_Loaded)
                RefreshAllData();
        }

        private void RefreshIcon(ScannableRegion inRegion)
        {
            var config = GetConfig(inRegion.ScanData.Flags());
            inRegion.CurrentIcon.SetIcon(config.Icon);
            if (WasScanned(inRegion.ScanData.Id()))
            {
                inRegion.CurrentIcon.SetColor(config.NodeConfig.ScannedLineColor, config.NodeConfig.ScannedFillColor);
            }
            else
            {
                inRegion.CurrentIcon.SetColor(config.NodeConfig.UnscannedLineColor, config.NodeConfig.UnscannedFillColor);
            }
        }

        #endregion // Callbacks

        #region Leaf

        [LeafMember("SetScanId"), UnityEngine.Scripting.Preserve]
        static public void SetScanId(ScriptObject inObject, StringHash32 inScanId)
        {
            ScanSystem scanSystem = ScanSystem.Find<ScanSystem>();
            if (scanSystem != null)
            {
                Log.Error("[ScanSystem] No ScanSystem present in scene");
                return;
            }

            ScannableRegion region = inObject.GetComponent<ScannableRegion>();
            if (region != null && Ref.Replace(ref region.ScanId, inScanId))
            {
                scanSystem.RefreshData(region);
            }
        }

        #endregion // Leaf
    }

    [Flags]
    public enum ScanResult : byte
    {
        NoChange =  0x0,
        NewScan =   0x1,
        NewLogbook = 0x2,
        NewBestiary = 0x4,
        NewFacts = 0x08
    }
}