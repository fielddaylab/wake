using UnityEngine;
using BeauUtil;
using BeauUtil.Blocks;
using System.Collections.Generic;
using System.Collections;
using BeauUtil.Tags;
using System.IO;
using BeauUtil.IO;
using Aqua;

namespace ProtoAqua.Observation
{
    public class ScanDataPackage : IDataBlockPackage<ScanData>
    {
        private readonly Dictionary<StringHash32, ScanData> m_Data = new Dictionary<StringHash32, ScanData>(32);

        private string m_Name;
        [BlockMeta("basePath")] private string m_RootPath = string.Empty;

        private IHotReloadable m_HotReload;
        private ScanDataMgr m_Mgr;

        public ScanDataPackage(string inName)
        {
            m_Name = inName;
            m_RootPath = inName;
        }

        public string Name { get { return m_Name; } }

        #region Reload

        public void BindAsset(TextAsset inAsset)
        {
            if (m_HotReload != null)
            {
                ReloadableAssetCache.Remove(m_HotReload);
                Ref.TryDispose(ref m_HotReload);
            }

            if (inAsset != null)
            {
                m_HotReload = new HotReloadableAssetProxy<TextAsset>(inAsset, "ScanDataPackage", Reload);
                ReloadableAssetCache.Add(m_HotReload);
            }
        }

        public void UnbindAsset()
        {
            if (m_HotReload != null)
            {
                ReloadableAssetCache.Remove(m_HotReload);
                Ref.TryDispose(ref m_HotReload);
            }
        }

        private void Reload(TextAsset inAsset, HotReloadOperation inOperation)
        {
            var mgr = m_Mgr;
            if (mgr != null)
            {
                mgr.Unload(this);
            }

            m_Data.Clear();
            m_RootPath = string.Empty;

            if (inOperation == HotReloadOperation.Modified)
            {
                ScanDataPackage self = this;
                BlockParser.Parse(ref self, null, inAsset.text, Parsing.Block, Generator.Instance);

                if (mgr != null)
                {
                    mgr.Load(this);
                }
            }
        }

        #endregion // Reload

        #region Manager

        public void BindManager(ScanDataMgr inMgr)
        {
            m_Mgr = inMgr;
        }

        #endregion // Manager

        #region ICollection

        public int Count { get { return m_Data.Count; } }

        public IEnumerator<ScanData> GetEnumerator()
        {
            return m_Data.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion // ICollection

        #region Generator

        public class Generator : AbstractBlockGenerator<ScanData, ScanDataPackage>
        {
            static public readonly Generator Instance = new Generator();

            public override ScanDataPackage CreatePackage(string inFileName)
            {
                return new ScanDataPackage(inFileName);
            }

            public override bool TryCreateBlock(IBlockParserUtil inUtil, ScanDataPackage inPackage, TagData inId, out ScanData outBlock)
            {
                inUtil.TempBuilder.Length = 0;
                inUtil.TempBuilder.Append(inPackage.m_RootPath);
                if (!inPackage.m_RootPath.EndsWith("."))
                    inUtil.TempBuilder.Append('.');
                inUtil.TempBuilder.AppendSlice(inId.Id);
                string fullId = inUtil.TempBuilder.Flush();
                outBlock = new ScanData(fullId);
                inPackage.m_Data.Add(fullId, outBlock);
                return true;
            }
        }

        #endregion // Generator

        #if UNITY_EDITOR

        [UnityEditor.Experimental.AssetImporters.ScriptedImporter(1, "scan")]
        private class Importer : UnityEditor.Experimental.AssetImporters.ScriptedImporter
        {
            public override void OnImportAsset(UnityEditor.Experimental.AssetImporters.AssetImportContext ctx)
            {
                TextAsset txtAsset = new TextAsset(File.ReadAllText(ctx.assetPath));
                ctx.AddObjectToAsset("Main Object", txtAsset);
                ctx.SetMainObject(txtAsset);
            }
        }

        #endif // UNITY_EDITOR
    }
}