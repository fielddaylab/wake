using BeauUtil;
using BeauUtil.Blocks;
using System.Collections.Generic;
using System.Collections;
using BeauUtil.Tags;
using UnityEngine;
using System.IO;
using UnityEngine.Scripting;
using BeauUtil.Debugger;
using System.Runtime.CompilerServices;

namespace Aqua
{
    public class LocPackage : ScriptableDataBlockPackage<LocNode>
    {
        private readonly Dictionary<StringHash32, string> m_Nodes = new Dictionary<StringHash32, string>(512);
        private readonly HashSet<StringHash32> m_IdsWithEvents = new HashSet<StringHash32>();

        [BlockMeta("basePath"), Preserve] private string m_RootPath = string.Empty;

        private LocNode m_CachedNode = new LocNode();

        #region Retrieve

        [MethodImpl(256)]
        public bool TryGetContent(StringHash32 inId, out string outString)
        {
            return m_Nodes.TryGetValue(inId, out outString);
        }

        [MethodImpl(256)]
        public bool HasEvents(StringHash32 inId)
        {
            return m_IdsWithEvents.Contains(inId);
        }

        #endregion // Retrieve

        #region IDataBlockPackage

        public override int Count { get { return m_Nodes.Count; } }

        public override IEnumerator<LocNode> GetEnumerator()
        {
            return null;
        }

        public override void Clear()
        {
            m_Nodes.Clear();
            m_IdsWithEvents.Clear();
        }

        #endregion // IDataBlockPackage

        #region Generator

        public class Generator : GeneratorBase<LocPackage>
        {
            static public readonly Generator Instance = new Generator();

            public override bool TryCreateBlock(IBlockParserUtil inUtil, LocPackage inPackage, TagData inId, out LocNode outBlock)
            {
                inUtil.TempBuilder.Length = 0;
                inUtil.TempBuilder.Append(inPackage.m_RootPath);
                if (!inPackage.m_RootPath.EndsWith(".") && !inId.Id.StartsWith('.'))
                    inUtil.TempBuilder.Append('.');
                inUtil.TempBuilder.AppendSlice(inId.Id);
                string fullId = inUtil.TempBuilder.Flush();
                outBlock = inPackage.m_CachedNode;
                outBlock.Id = fullId;
                outBlock.Content = string.Empty;
                // Log.Msg("adding loc entry {0} ({1})", fullId, ((TextId) fullId).Hash().HashValue);
                return true;
            }

            public override void CompleteBlock(IBlockParserUtil inUtil, LocPackage inPackage, LocNode inBlock, bool inbError) {
                Assert.False(inPackage.m_Nodes.ContainsKey(inBlock.Id), "Duplicate localization key {0}", inBlock.Id);
                inPackage.m_Nodes.Add(inBlock.Id, inBlock.Content);
                if (inBlock.Content.IndexOf('{') >= 0)
                {
                    inPackage.m_IdsWithEvents.Add(inBlock.Id);
                }
            }
        }

        #endregion // Generator

        #if UNITY_EDITOR

        [ScriptedExtension(1, "aqloc")]
        private class Importer : ImporterBase<LocPackage> { }

        static internal IEnumerable<KeyValuePair<StringHash32, string>> GatherStrings(LocPackage inPackage)
        {
            inPackage.Parse(Generator.Instance);
            foreach(var kv in inPackage.m_Nodes)
            {
                yield return new KeyValuePair<StringHash32, string>(kv.Key, kv.Value);
            }
        }

        #endif // UNITY_EDITOR
    }
}