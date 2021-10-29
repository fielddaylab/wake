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
    public class ScanDataPackage : ScriptableDataBlockPackage<ScanData>
    {
        private readonly Dictionary<StringHash32, ScanData> m_Data = new Dictionary<StringHash32, ScanData>(32);

        [BlockMeta("basePath"), UnityEngine.Scripting.Preserve] private string m_RootPath = string.Empty;

        #region ICollection

        public override int Count { get { return m_Data.Count; } }

        public override IEnumerator<ScanData> GetEnumerator()
        {
            return m_Data.Values.GetEnumerator();
        }

        public override void Clear()
        {
            base.Clear();

            m_Data.Clear();
        }

        #endregion // ICollection

        #region Generator

        public class Generator : GeneratorBase<ScanDataPackage>
        {
            static public readonly Generator Instance = new Generator();

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

        [ScriptedExtension(1, "scan")]
        private class Importer : ImporterBase<ScanDataPackage> { }

        #endif // UNITY_EDITOR
    }
}