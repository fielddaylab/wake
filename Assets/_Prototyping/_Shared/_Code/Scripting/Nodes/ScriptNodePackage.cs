using System;
using BeauUtil;
using BeauUtil.Blocks;
using System.Collections.Generic;
using System.Collections;
using BeauUtil.Tags;
using UnityEngine;
using BeauUtil.IO;
using System.IO;

namespace ProtoAqua.Scripting
{
    public class ScriptNodePackage : IDataBlockPackage<ScriptNode>
    {
        private readonly Dictionary<StringHash32, ScriptNode> m_Nodes = new Dictionary<StringHash32, ScriptNode>(32);

        private string m_Name;
        [BlockMeta("basePath")] private string m_RootPath = string.Empty;

        private IHotReloadable m_HotReload;

        public ScriptNodePackage(string inName)
        {
            m_Name = inName;
            m_RootPath = inName;
        }

        /// <summary>
        /// Name of this package.
        /// </summary>
        public string Name() { return m_Name; }

        /// <summary>
        /// Attempts to retrieve the node with the given id.
        /// </summary>
        public bool TryGetNode(StringHash32 inId, out ScriptNode outNode)
        {
            return m_Nodes.TryGetValue(inId, out outNode);
        }

        /// <summary>
        /// Attempts to retrieve the entrypoint with the given id.
        /// </summary>
        public bool TryGetEntrypoint(StringHash32 inId, out ScriptNode outNode)
        {
            ScriptNode node;
            if (m_Nodes.TryGetValue(inId, out node))
            {
                if ((node.Flags() & ScriptNodeFlags.Entrypoint) == ScriptNodeFlags.Entrypoint)
                {
                    outNode = node;
                    return true;
                }
            }

            outNode = null;
            return false;
        }

        /// <summary>
        /// Returns all entrypoints.
        /// </summary>
        public IEnumerable<ScriptNode> Entrypoints()
        {
            foreach(var node in m_Nodes.Values)
            {
                if ((node.Flags() & ScriptNodeFlags.Entrypoint) != 0)
                {
                    yield return node;
                }
            }
        }

        /// <summary>
        /// Returns all responses.
        /// </summary>
        public IEnumerable<ScriptNode> Responses()
        {
            foreach(var node in m_Nodes.Values)
            {
                if ((node.Flags() & ScriptNodeFlags.TriggerResponse) != 0)
                {
                    yield return node;
                }
            }
        }

        #region Reload

        /// <summary>
        /// Binds a source asset for hot reloading.
        /// </summary>
        public void BindAsset(TextAsset inAsset)
        {
            if (m_HotReload != null)
            {
                ReloadableAssetCache.Remove(m_HotReload);
                Ref.TryDispose(ref m_HotReload);
            }

            if (inAsset != null)
            {
                m_HotReload = new HotReloadableAssetProxy<TextAsset>(inAsset, "ScriptNodePackage", ReloadFromAsset);
                ReloadableAssetCache.Add(m_HotReload);
            }
        }

        /// <summary>
        /// Binds a source asset for hot reloading.
        /// </summary>
        public void BindAsset(string inFilePath)
        {
            if (m_HotReload != null)
            {
                ReloadableAssetCache.Remove(m_HotReload);
                Ref.TryDispose(ref m_HotReload);
            }

            if (!string.IsNullOrEmpty(inFilePath))
            {
                m_HotReload = new HotReloadableFileProxy(inFilePath, "ScriptNodePackage", ReloadFromFilePath);
                ReloadableAssetCache.Add(m_HotReload);
            }
        }

        private void ReloadFromAsset(TextAsset inAsset, HotReloadOperation inOperation)
        {
            var mgr = Services.Script;
            if (mgr != null)
            {
                mgr.Unload(this);
            }

            m_Nodes.Clear();
            m_RootPath = string.Empty;

            if (inOperation == HotReloadOperation.Modified)
            {
                var self = this;
                BlockParser.Parse(ref self, null, inAsset.text, Parsing.Block, Generator.Instance);

                if (mgr != null)
                {
                    mgr.Load(this);
                }
            }
        }

        private void ReloadFromFilePath(string inFilePath, HotReloadOperation inOperation)
        {
            var mgr = Services.Script;
            if (mgr != null)
            {
                mgr.Unload(this);
            }

            m_Nodes.Clear();
            m_RootPath = string.Empty;

            if (inOperation == HotReloadOperation.Modified)
            {
                var self = this;
                BlockParser.Parse(ref self, null, File.ReadAllText(inFilePath), Parsing.Block, Generator.Instance);

                if (mgr != null)
                {
                    mgr.Load(this);
                }
            }
        }

        /// <summary>
        /// Unbinds the source asset.
        /// </summary>
        public void UnbindAsset()
        {
            if (m_HotReload != null)
            {
                ReloadableAssetCache.Remove(m_HotReload);
                Ref.TryDispose(ref m_HotReload);
            }
        }

        #endregion // Reload

        #region ICollection

        public int Count { get { return m_Nodes.Count; } }

        public IEnumerator<ScriptNode> GetEnumerator()
        {
            return m_Nodes.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion // ICollection

        #region Generator

        public class Generator : AbstractBlockGenerator<ScriptNode, ScriptNodePackage>
        {
            static public readonly Generator Instance = new Generator();

            public override ScriptNodePackage CreatePackage(string inFileName)
            {
                return new ScriptNodePackage(inFileName);
            }

            public override bool TryCreateBlock(IBlockParserUtil inUtil, ScriptNodePackage inPackage, TagData inId, out ScriptNode outBlock)
            {
                inUtil.TempBuilder.Length = 0;
                inUtil.TempBuilder.Append(inPackage.m_RootPath);
                if (!inPackage.m_RootPath.EndsWith("."))
                    inUtil.TempBuilder.Append('.');
                inUtil.TempBuilder.AppendSlice(inId.Id);
                string fullId = inUtil.TempBuilder.Flush();
                outBlock = new ScriptNode(inPackage, fullId);
                inPackage.m_Nodes.Add(outBlock.Id(), outBlock);
                return true;
            }
        }

        #endregion // Generator
    }
}