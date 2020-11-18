using BeauUtil;
using BeauUtil.Blocks;
using System.Collections.Generic;
using UnityEngine;
using BeauUtil.IO;
using System.IO;
using Leaf;
using Leaf.Compiler;
using Leaf.Runtime;

namespace Aqua.Scripting
{
    public class ScriptNodePackage : LeafNodePackage<ScriptNode>
    {
        private IHotReloadable m_HotReload;

        public ScriptNodePackage(string inName)
            : base(inName)
        {
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
        public void BindAsset(LeafAsset inAsset)
        {
            if (m_HotReload != null)
            {
                ReloadableAssetCache.Remove(m_HotReload);
                Ref.TryDispose(ref m_HotReload);
            }

            if (inAsset != null)
            {
                m_HotReload = new HotReloadableAssetProxy<LeafAsset>(inAsset, "ScriptNodePackage", ReloadFromAsset);
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

        private void ReloadFromAsset(LeafAsset inAsset, HotReloadOperation inOperation)
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
                BlockParser.Parse(ref self, m_Name, inAsset.Source(), Parsing.Block, Generator.Instance);

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

            Clear();

            m_RootPath = string.Empty;

            if (inOperation == HotReloadOperation.Modified)
            {
                var self = this;
                BlockParser.Parse(ref self, m_Name, File.ReadAllText(inFilePath), Parsing.Block, Generator.Instance);

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

        #region Generator

        public class Generator : LeafParser<ScriptNode, ScriptNodePackage>
        {
            static public readonly Generator Instance = new Generator();

            public override ILeafExpression<ScriptNode> CompileExpression(StringSlice inExpression)
            {
                return new ScriptExpression(inExpression.ToString());
            }

            public override bool IsVerbose
            {
                get { return false; }
            }

            public override ScriptNodePackage CreatePackage(string inFileName)
            {
                return new ScriptNodePackage(inFileName);
            }

            protected override ScriptNode CreateNode(string inFullId, StringSlice inExtraData, ScriptNodePackage inPackage)
            {
                return new ScriptNode(inPackage, inFullId);
            }
        }

        #endregion // Generator
    }
}