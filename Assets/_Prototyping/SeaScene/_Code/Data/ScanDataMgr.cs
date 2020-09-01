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

        private ScanDataPackage m_MasterPackage = null;
        private ScanDataPackage.Generator m_Generator = new ScanDataPackage.Generator();

        // TODO: Move this out of here!
        private HashSet<string> m_ScannedIds = new HashSet<string>(StringComparer.Ordinal);

        public bool TryGetScanData(string inId, out ScanData outData)
        {
            return m_MasterPackage.TryGetScanData(inId, out outData);
        }

        public bool WasScanned(string inId) { return m_ScannedIds.Contains(inId); }

        public bool RegisterScanned(string inId)
        {
            if (m_ScannedIds.Add(inId))
            {
                Services.Events.Dispatch(ObservationEvents.ScannableComplete, inId);
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

            if (m_ScannedIds.Contains(inData.Id()))
                return m_CompletedScanDuration;
            
            return m_BaseScanDuration * inData.ScanSpeed();
        }

        #region TweakAsset

        protected override void Apply()
        {
            m_MasterPackage = new ScanDataPackage("MasterPackage");
            foreach(var asset in m_DefaultAssets)
            {
                BlockParser.Parse(ref m_MasterPackage, asset.name, asset.text, BlockParsingRules.Default, m_Generator);
            }

            Debug.LogFormat("[ScanDataMgr] Loaded {0} scan datas", m_MasterPackage.Count);
        }

        protected override void Remove()
        {
            m_MasterPackage = null;
            m_ScannedIds.Clear();
        }

        #endregion // TweakAsset
    }
}