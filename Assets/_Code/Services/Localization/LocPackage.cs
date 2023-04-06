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
using System.Text;

namespace Aqua
{
    public class LocPackage : ScriptableDataBlockPackage<LocNode>
    {
        private const int MaxCompressedSize = 1024 * 1024 * 4;

        private readonly Dictionary<StringHash32, string> m_Nodes = new Dictionary<StringHash32, string>(512);
        private readonly HashSet<StringHash32> m_IdsWithEvents = Collections.NewSet<StringHash32>(128);

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

        public Dictionary<StringHash32, string>.KeyCollection AllKeys
        {
            [MethodImpl(256)] get { return m_Nodes.Keys; }
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

        private unsafe class BinaryReadState
        {
            public byte* Buffer;
            public int Length;
            public Unsafe.PinnedArrayHandle<byte> Handle;

            internal BinaryReadState(byte[] bytes) {
                Handle = Unsafe.PinArray(bytes);
                Buffer = Handle.Address;
                Length = Handle.Length;
            }
        }

        static public IEnumerator ReadFromBinary(LocPackage ioPackage, byte[] inBytes)
        {
            var pinned = Unsafe.PinArray<byte>(inBytes);
            try {
                ushort nodeCount = ReadNodeCount(ref pinned);
                while(nodeCount-- > 0) {
                    ReadNode(ref pinned, ioPackage);
                    if (nodeCount % 32 == 0) {
                        yield return null;
                    }
                }
                nodeCount = ReadNodeCount(ref pinned);
                Collections.Initialize(ioPackage.m_IdsWithEvents, nodeCount);
                while(nodeCount-- > 0) {
                    ioPackage.m_IdsWithEvents.Add(ReadNodeId(ref pinned));
                    if (nodeCount % 64 == 0) {
                        yield return null;
                    }
                }
            } finally {
                pinned.Dispose();
            }
        }

        static private unsafe ushort ReadNodeCount(ref Unsafe.PinnedArrayHandle<byte> bytes) {
            return UnsafeExt.Read<ushort>(ref bytes.Address, ref bytes.Length);
        }

        static private unsafe void ReadNode(ref Unsafe.PinnedArrayHandle<byte> bytes, LocPackage package) {
            StringHash32 id = ReadNodeId(ref bytes);
            Log.Msg("Reading '{0}'...", id.ToDebugString());
            string text = UnsafeExt.ReadString(ref bytes.Address, ref bytes.Length);
            package.m_Nodes.Add(id, text);
        }

        static private unsafe StringHash32 ReadNodeId(ref Unsafe.PinnedArrayHandle<byte> bytes) {
            return UnsafeExt.Read<StringHash32>(ref bytes.Address, ref bytes.Length);
        }

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

        static internal unsafe byte[] Compress(LocPackage[] inPackages)
        {
            LocPackage tmpPkg = ScriptableObject.CreateInstance<LocPackage>();
            byte* buffer = Unsafe.AllocArray<byte>(MaxCompressedSize);
            byte* head = buffer;
            int bufferLength = 0;
            try {
                foreach(var pkg in inPackages) {
                    BlockParser.Parse(ref tmpPkg, pkg, Parsing.Block, Generator.Instance);
                }

                UnsafeExt.Write(&head, &bufferLength, (ushort) tmpPkg.m_Nodes.Count);
                foreach(var kv in tmpPkg.m_Nodes) {
                    UnsafeExt.Write(&head, &bufferLength, kv.Key);
                    UnsafeExt.WriteString(&head, &bufferLength, kv.Value);
                }

                UnsafeExt.Write(&head, &bufferLength, (ushort) tmpPkg.m_IdsWithEvents.Count);
                foreach(var v in tmpPkg.m_IdsWithEvents) {
                    UnsafeExt.Write(&head, &bufferLength, v);
                }

                byte[] written = new byte[bufferLength];
                Unsafe.CopyArray(buffer, bufferLength, written);
                return written;
            } finally {
                DestroyImmediate(tmpPkg);
                Unsafe.Free(buffer);
            }
        }

        #endif // UNITY_EDITOR
    }
}