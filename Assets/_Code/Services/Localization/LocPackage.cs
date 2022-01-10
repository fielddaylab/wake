using BeauUtil;
using BeauUtil.Blocks;
using System.Collections.Generic;
using System.Collections;
using BeauUtil.Tags;
using UnityEngine;
using System.IO;
using UnityEngine.Scripting;
using BeauUtil.Debugger;

namespace Aqua
{
    public class LocPackage : ScriptableDataBlockPackage<LocNode>
    {
        private readonly Dictionary<StringHash32, LocNode> m_Nodes = new Dictionary<StringHash32, LocNode>(512);

        [BlockMeta("basePath"), Preserve] private string m_RootPath = string.Empty;

        #region Retrieve

        public bool TryGetNode(StringHash32 inId, out LocNode outNode)
        {
            return m_Nodes.TryGetValue(inId, out outNode);
        }

        public bool TryGetContent(StringHash32 inId, out string outString)
        {
            LocNode node;
            if (m_Nodes.TryGetValue(inId, out node))
            {
                outString = node.Content();
                return true;
            }

            outString = null;
            return false;
        }

        public string GetContent(StringHash32 inId, string inDefault = null)
        {
            string content;
            if (!TryGetContent(inId, out content))
            {
                content = inDefault;
            }
            return content;
        }

        public string this[StringHash32 inId]
        {
            get { return GetContent(inId); }
        }

        #endregion // Retrieve

        #region IDataBlockPackage

        public override int Count { get { return m_Nodes.Count; } }

        public override IEnumerator<LocNode> GetEnumerator()
        {
            return m_Nodes.Values.GetEnumerator();
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
                if (!inPackage.m_RootPath.EndsWith("."))
                    inUtil.TempBuilder.Append('.');
                inUtil.TempBuilder.AppendSlice(inId.Id);
                string fullId = inUtil.TempBuilder.Flush();
                outBlock = new LocNode(fullId);
                inPackage.m_Nodes.Add(fullId, outBlock);
                Log.Msg("adding loc entry {0} ({1})", fullId, ((TextId) fullId).Hash().HashValue);
                return true;
            }
        }

        #endregion // Generator

        #if UNITY_EDITOR

        [ScriptedExtension(1, "aqloc")]
        private class Importer : ImporterBase<LocPackage> { }

        static internal IEnumerable<KeyValuePair<StringHash32, string>> GatherStrings(LocPackage inPackage)
        {
            inPackage.Parse(Generator.Instance);
            foreach(var node in inPackage)
            {
                yield return new KeyValuePair<StringHash32, string>(node.Id(), node.Content());
            }
        }

        #endif // UNITY_EDITOR
    }
}