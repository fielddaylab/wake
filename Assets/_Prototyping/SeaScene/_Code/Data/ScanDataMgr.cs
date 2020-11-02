using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Blocks;
using UnityEngine;

namespace ProtoAqua.Observation
{
    [CreateAssetMenu(menuName = "Prototype/Observation/Scan Data Manager")]
    public class ScanDataMgr : TweakAsset
    {
        #region Types

        [Serializable]
        public class ScanTypeConfig
        {
            public Color BackgroundColor = ColorBank.White;
            public Color HeaderColor = ColorBank.Yellow;
            public Color TextColor = ColorBank.White;

            public string OpenSound;

            public ScanNodeConfig Node;
        }

        [Serializable]
        public class ScanNodeConfig
        {
            public Color UnscannedColor;
            public Color ScannedColor;
        }

        #endregion // Types

        #region Inspector

        [SerializeField] private TextAsset[] m_DefaultAssets = null;
        [SerializeField] private ScanTypeConfig m_DefaultScanConfig = null;
        [SerializeField] private ScanTypeConfig m_ImportantScanConfig = null;
        [SerializeField] private float m_BaseScanDuration = 0.75f;
        [SerializeField] private float m_CompletedScanDuration = 0.2f;

        #endregion // Inspector

        private HashSet<ScanDataPackage> m_Packages = new HashSet<ScanDataPackage>();
        private Dictionary<StringHash32, ScanData> m_MasterMap = new Dictionary<StringHash32, ScanData>();

        public bool TryGetScanData(StringHash32 inId, out ScanData outData)
        {
            return m_MasterMap.TryGetValue(inId, out outData);
        }

        public bool WasScanned(StringHash32 inId) { return Services.Data.Profile.Inventory.WasScanned(inId); }

        public bool RegisterScanned(ScanData inData)
        {
            if (Services.Data.Profile.Inventory.RegisterScanned(inData.Id()))
            {
                Services.Events.Dispatch(ObservationEvents.ScannableComplete, inData.Id());

                // apply variables?
                var scanModifications = inData.OnScanModifications();
                if (scanModifications != null && scanModifications.Length > 0)
                {
                    for(int i = 0; i < scanModifications.Length; ++i)
                        scanModifications[i].Execute(Services.Data.VariableResolver, inData);
                }
                
                return true;
            }

            return false;
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

        #region Register/Unregister

        public void Load(ScanDataPackage inPackage)
        {
            if (m_Packages.Add(inPackage))
            {
                inPackage.BindManager(this);
                foreach(var node in inPackage)
                {
                    m_MasterMap.Add(node.Id(), node);
                }

                Debug.LogFormat("[ScanDataMgr] Loaded scan data package '{0}' with {1} nodes", inPackage.Name, inPackage.Count);
            }
        }

        public void Unload(ScanDataPackage inPackage)
        {
            if (m_Packages.Remove(inPackage))
            {
                inPackage.BindManager(null);
                foreach(var node in inPackage)
                {
                    m_MasterMap.Remove(node.Id());
                }
                Debug.LogFormat("[ScanDataMgr] Unloaded scan data package '{0}'", inPackage.Name);
            }
        }

        #endregion // Register/Unregister

        #region TweakAsset

        protected override void Apply()
        {
            foreach(var asset in m_DefaultAssets)
            {
                ScanDataPackage package = BlockParser.Parse(asset.name, asset.text, Parsing.Block, ScanDataPackage.Generator.Instance);
                package.BindAsset(asset);
                Load(package);
            }
        }

        protected override void Remove()
        {
            foreach(var package in m_Packages)
            {
                package.UnbindAsset();
                package.BindManager(null);
            }
            m_Packages.Clear();
            m_MasterMap.Clear();
        }

        #endregion // TweakAsset
    }
}