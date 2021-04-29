using System;
using UnityEngine;
using UnityEditor;
using BeauUtil.IO;
using BeauUtil.Blocks;
using System.Collections.Generic;
using System.Collections;
using BeauUtil.Tags;
using BeauUtil;

namespace Aqua.Editor
{
    public class LocEditor : EditorWindow
    {
        #region Types

        [Serializable]
        private class PackageRecord : IDataBlockPackage<TextRecord>
        {
            public string Name;
            public List<TextRecord> Records = new List<TextRecord>();
            [BlockMeta("basePath")] public string BasePath = string.Empty;

            public PackageRecord(string inName)
            {
                Name = inName;
            }

            public int Count { get { return Records.Count; } }

            public IEnumerator<TextRecord> GetEnumerator()
            {
                return Records.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public LocPackage Source;
            public HotReloadableAssetProxy<LocPackage> Reloadable;
        }

        [Serializable]
        private class TextRecord : IDataBlock
        {
            public string Id;
            public string Content;

            public TextRecord(string inId)
            {
                Id = inId;
            }
        }

        #endregion // Types

        #region Generator

        private class PackageGenerator : AbstractBlockGenerator<TextRecord, PackageRecord>
        {
            public override PackageRecord CreatePackage(string inFileName)
            {
                return new PackageRecord(inFileName);
            }

            public override void OnStart(IBlockParserUtil inUtil, PackageRecord inPackage)
            {
                base.OnStart(inUtil, inPackage);
                inPackage.Records.Clear();
            }

            public override bool TryCreateBlock(IBlockParserUtil inUtil, PackageRecord inPackage, TagData inId, out TextRecord outBlock)
            {
                inUtil.TempBuilder.Length = 0;
                inUtil.TempBuilder.Append(inPackage.BasePath);
                if (!inPackage.BasePath.EndsWith("."))
                    inUtil.TempBuilder.Append('.');
                inUtil.TempBuilder.AppendSlice(inId.Id);
                string fullId = inUtil.TempBuilder.Flush();
                outBlock = new TextRecord(fullId);
                inPackage.Records.Add(outBlock);
                return true;
            }
        }

        #endregion // Generator
    }
}