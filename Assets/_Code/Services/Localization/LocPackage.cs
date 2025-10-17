using BeauUtil.Blocks;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using BeauUtil;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine.Scripting;
using UnityEngine;

#if UNITY_EDITOR
using static Aqua.ScriptingService;
#endif // UNITY_EDITOR

namespace Aqua
{
    public class LocPackage : ScriptableDataBlockPackage<LocNode>
    {
        private const int MaxCompressedSize = 1024 * 1024 * 8;

        private readonly Dictionary<StringHash32, string> m_Nodes = Collections.NewDictionary<StringHash32, string>(512);
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
            base.Clear();
            m_RootPath = string.Empty;
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
                if (inPackage.m_RootPath.Length > 0 && !inPackage.m_RootPath.EndsWith(".") && !inId.Id.StartsWith('.'))
                    inUtil.TempBuilder.Append('.');
                inUtil.TempBuilder.AppendSlice(inId.Id);
                string fullId = inUtil.TempBuilder.Flush();
                if (fullId.IndexOf("__") != -1) {
                    fullId = fullId.Replace("__", "|");
                }
                if (fullId.IndexOf("_c_") != -1) {
                    fullId = fullId.Replace("_c_", ":");
                }
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

        static private unsafe StringHash32 ReadNodeId(ref Unsafe.PinnedArrayHandle<byte> bytes)
        {
            return UnsafeExt.Read<StringHash32>(ref bytes.Address, ref bytes.Length);
        }

        #if UNITY_EDITOR

        [ScriptedExtension(1, "aqloc")]
        private class Importer : ImporterBase<LocPackage> { }

        static private CustomTagParserConfig CreateConstParser() {
            CustomTagParserConfig config = new CustomTagParserConfig();
            config.AddReplace("n", "\n").WithAliases("newline");
            config.AddReplace("highlight", "<color=yellow>").WithAliases("h").CloseWith("</color>");
            config.AddReplace("property-name", "<" + ColorTags.PropertyColorString + ">").CloseWith("</color>");
            config.AddReplace("critter-name", "<" + ColorTags.CritterColorString + ">").CloseWith("</color>");
            config.AddReplace("!", "<" + ColorTags.AlertColorString + ">").CloseWith("</color>");
            config.AddReplace("env-name", "<" + ColorTags.EnvColorString + ">").CloseWith("</color>");
            config.AddReplace("item", "<" + ColorTags.ItemColorString + ">").CloseWith("</color>");
            config.AddReplace("item-name", "<" + ColorTags.ItemColorString + ">").CloseWith("</color>");
            config.AddReplace("map-name", "<" + ColorTags.MapColorString + ">").CloseWith("</color>");
            config.AddReplace("m", "<" + ColorTags.MapColorString + ">").CloseWith("</color>");
            config.AddReplace("cash", "<" + ColorTags.CashColorString + ">").CloseWith("</color><sprite name=\"cash\">");
            config.AddReplace("exp", "<" + ColorTags.ExpColorString + ">").CloseWith("</color><sprite name=\"exp\">");
            config.AddReplace("player-name", "O");
            config.AddReplace("icon", ReplaceIcon);
            config.AddReplace("nameof", TryReplaceNameOf);
            config.AddReplace("pluralnameof", TryReplacePluralNameOf);
            config.AddReplace("fullnameof", TryReplaceFullNameOf);
            config.AddReplace("formalnameof", TryReplaceFormalShortNameOf);
            config.AddReplace('|', "{wait 0.25}");

            // Extra Replace Tags (with embedded events)

            config.AddReplace("slow", "{wait 0.05}{speed 0.5}").CloseWith("{/speed}{wait 0.05}");
            config.AddReplace("reallySlow", "{wait 0.05}{speed 0.25}").CloseWith("{/speed}{wait 0.05}");
            config.AddReplace("fast", "{wait 0.05}{speed 1.25}").CloseWith("{/speed}{wait 0.05}");

            return config;
        }

        static private void ResetIdsWithEvents(LocPackage package) {
            package.m_IdsWithEvents.Clear();
        }

        static private void MarkIdWithEvent(LocPackage package, StringHash32 id, string data) {
            if (data.IndexOf('{') >= 0) {
                package.m_IdsWithEvents.Add(id);
            }
        }

        static private string ReplaceIcon(TagData inTag, object inContext) {
            return string.Format("<sprite name=\"{0}\">", inTag.Data.ToString());
        }

        static private bool IsTagConst(StringSlice inData) {
            return inData.Length < 2 || inData[0] != '$';
        }

        static private bool TryReplaceNameOf(TagData tag, object context, out string result) {
            if (!IsTagConst(tag.Data)) {
                result = null;
                return false;
            }

            LocPackage pkg = (LocPackage)context;

            if (tag.Data.StartsWith('@')) {
                StringHash32 characterId = tag.Data.Substring(1);
                ScriptCharacterDef inlineCharDef = Assets.Character(characterId);
                if (!inlineCharDef) {
                    Log.Error("[ScriptingService] Unknown character: '{0}'", characterId.ToDebugString());
                    result = null;
                    return false;
                }
                return pkg.TryGetContent(inlineCharDef.ShortNameId(), out result);
            }

            ScriptableObject obj = Assets.Find(tag.Data);

            BestiaryDesc bestiary = obj as BestiaryDesc;
            if (!bestiary.IsReferenceNull()) {
                switch (bestiary.Category()) {
                    case BestiaryDescCategory.Critter: {
                        bool found = pkg.TryGetContent(bestiary.CommonName(), out result);
                        result = "<" + ColorTags.CritterColorString + ">" + result + "</color>";
                        return found;
                    }
                    case BestiaryDescCategory.Environment: {
                        bool found = pkg.TryGetContent(bestiary.CommonName(), out result);
                        result = "<" + ColorTags.EnvColorString + ">" + result + "</color>";
                        return found;
                    }
                    default: {
                        return pkg.TryGetContent(bestiary.CommonName(), out result);
                    }
                }
            }

            InvItem item = obj as InvItem;
            if (!item.IsReferenceNull()) {
                if (item.Id() == ItemIds.Cash) {
                    bool found = pkg.TryGetContent(item.NameTextId(), out result);
                    result = "<" + ColorTags.CashColorString + ">" + result + "</color><sprite name=\"cash\">";
                    return found;
                } else if (item.Id() == ItemIds.Exp) {
                    bool found = pkg.TryGetContent(item.NameTextId(), out result);
                    result = "<" + ColorTags.ExpColorString + ">" + result + "</color><sprite name=\"exp\">";
                    return found;
                } else {
                    bool found = pkg.TryGetContent(item.NameTextId(), out result);
                    result = "<" + ColorTags.ItemColorString + ">" + result + "</color>";
                    return found;
                }
            }

            WaterPropertyDesc property = obj as WaterPropertyDesc;
            if (!property.IsReferenceNull()) {
                bool found = pkg.TryGetContent(property.LabelId(), out result);
                result = "<" + ColorTags.PropertyColorString + ">" + result + "</color>";
                return found;
            }

            MapDesc map = obj as MapDesc;
            if (!map.IsReferenceNull()) {
                bool found = pkg.TryGetContent(map.ProperNameId(), out result);
                result = "<" + ColorTags.MapColorString+ ">" + result + "</color>";
                return found;
            }

            ScriptCharacterDef charDef = obj as ScriptCharacterDef;
            if (!charDef.IsReferenceNull()) {
                return pkg.TryGetContent(charDef.ShortNameId(), out result);
            }

            JobDesc jobDef = obj as JobDesc;
            if (!jobDef.IsReferenceNull()) {
                bool found = pkg.TryGetContent(jobDef.NameId(), out result);
                result = "<" + ColorTags.JobColorString + ">" + result + "</color>";
                return found;
            }

            Log.Error("[ScriptingService] Unknown symbol to get name of: '{0}'", tag.Data);
            result = null;
            return false;
        }

        static private bool TryReplacePluralNameOf(TagData tag, object context, out string result) {
            if (!IsTagConst(tag.Data)) {
                result = null;
                return false;
            }

            LocPackage pkg = (LocPackage)context;

            if (tag.Data.StartsWith('@')) {
                StringHash32 characterId = tag.Data.Substring(1);
                ScriptCharacterDef inlineCharDef = Assets.Character(characterId);
                if (!inlineCharDef) {
                    Log.Error("[ScriptingService] Unknown character: '{0}'", characterId.ToDebugString());
                    result = null;
                    return false;
                }
                return pkg.TryGetContent(inlineCharDef.ShortNameId(), out result);
            }

            ScriptableObject obj = Assets.Find(tag.Data);

            BestiaryDesc bestiary = obj as BestiaryDesc;
            if (!bestiary.IsReferenceNull()) {
                switch (bestiary.Category()) {
                    case BestiaryDescCategory.Critter: {
                        bool found = pkg.TryGetContent(bestiary.PluralCommonName(), out result);
                        result = "<" + ColorTags.CritterColorString + ">" + result + "</color>";
                        return found;
                    }
                    case BestiaryDescCategory.Environment: {
                        bool found = pkg.TryGetContent(bestiary.PluralCommonName(), out result);
                        result = "<" + ColorTags.EnvColorString + ">" + result + "</color>";
                        return found;
                    }
                    default: {
                        return pkg.TryGetContent(bestiary.PluralCommonName(), out result);
                    }
                }
            }

            InvItem item = obj as InvItem;
            if (!item.IsReferenceNull()) {
                if (item.Id() == ItemIds.Cash) {
                    bool found = pkg.TryGetContent(item.PluralNameTextId(), out result);
                    result = "<" + ColorTags.CashColorString + ">" + result + "</color><sprite name=\"cash\">";
                    return found;
                } else if (item.Id() == ItemIds.Exp) {
                    bool found = pkg.TryGetContent(item.PluralNameTextId(), out result);
                    result = "<" + ColorTags.ExpColorString + ">" + result + "</color><sprite name=\"exp\">";
                    return found;
                } else {
                    bool found = pkg.TryGetContent(item.PluralNameTextId(), out result);
                    result = "<" + ColorTags.ItemColorString + ">" + result + "</color>";
                    return found;
                }
            }

            return TryReplaceNameOf(tag, context, out result);
        }

        static private bool TryReplaceFullNameOf(TagData tag, object context, out string result) {
            if (!IsTagConst(tag.Data)) {
                result = null;
                return false;
            }

            LocPackage pkg = (LocPackage)context;

            if (tag.Data.StartsWith('@')) {
                StringHash32 characterId = tag.Data.Substring(1);
                ScriptCharacterDef inlineCharDef = Assets.Character(characterId);
                if (!inlineCharDef) {
                    Log.Error("[ScriptingService] Unknown character: '{0}'", characterId.ToDebugString());
                    result = null;
                    return false;
                }
                return pkg.TryGetContent(inlineCharDef.NameId(), out result);
            }

            ScriptableObject obj = Assets.Find(tag.Data);

            ScriptCharacterDef charDef = obj as ScriptCharacterDef;
            if (!charDef.IsReferenceNull()) {
                return pkg.TryGetContent(charDef.NameId(), out result);
            }

            MapDesc map = obj as MapDesc;
            if (!map.IsReferenceNull()) {
                bool found = pkg.TryGetContent(map.LabelId(), out result);
                result = "<" + ColorTags.MapColorString + ">" + result + "</color>";
                return found;
            }

            return TryReplaceNameOf(tag, context, out result);
        }

        static private bool TryReplaceFormalShortNameOf(TagData tag, object context, out string result) {
            if (!IsTagConst(tag.Data)) {
                result = null;
                return false;
            }

            LocPackage pkg = (LocPackage)context;

            if (tag.Data.StartsWith('@')) {
                StringHash32 characterId = tag.Data.Substring(1);
                ScriptCharacterDef inlineCharDef = Assets.Character(characterId);
                if (!inlineCharDef) {
                    Log.Error("[ScriptingService] Unknown character: '{0}'", characterId.ToDebugString());
                    result = null;
                    return false;
                }
                return pkg.TryGetContent(inlineCharDef.FormalShortNameId(), out result);
            }

            ScriptableObject obj = Assets.Find(tag.Data);

            ScriptCharacterDef charDef = obj as ScriptCharacterDef;
            if (!charDef.IsReferenceNull()) {
                return pkg.TryGetContent(charDef.FormalShortNameId(), out result);
            }

            return TryReplaceNameOf(tag, context, out result);
        }

        static internal IEnumerable<KeyValuePair<StringHash32, string>> GatherStrings(LocPackage inPackage)
        {
            inPackage.Parse(Generator.Instance);
            foreach(var kv in inPackage.m_Nodes)
            {
                yield return new KeyValuePair<StringHash32, string>(kv.Key, kv.Value);
            }
        }

        static internal unsafe byte[] Compress(LocPackage[] inPackages, bool collapseConsts)
        {
            LocPackage tmpPkg = ScriptableObject.CreateInstance<LocPackage>();
            bool isHumanControlling = UnityEditorInternal.InternalEditorUtility.isHumanControllingUs && !UnityEditor.BuildPipeline.isBuildingPlayer;

            var constParser = new TagStringParser();
            if (collapseConsts) {
                var constConfig = CreateConstParser();
                constParser.ReplaceProcessor = constConfig;
                constParser.EventProcessor = null;
                constParser.Delimiters = Parsing.InlineEvent;
            }
            TagString constTagString = new TagString();
            int collapsedCount = 0;
            
            byte* buffer = Unsafe.AllocArray<byte>(MaxCompressedSize);
            byte* head = buffer;
            int bufferLength = 0;
            try {
                foreach(var pkg in inPackages) {
                    BlockParser.Parse(ref tmpPkg, pkg, Parsing.Block, Generator.Instance);
                }

                Log.Msg("{0} nodes in package", tmpPkg.m_Nodes.Count);
                Log.Msg("{0} nodes with events originally in package", tmpPkg.m_IdsWithEvents.Count);

                int originalEventNodeCount = tmpPkg.m_IdsWithEvents.Count;

                ResetIdsWithEvents(tmpPkg);

                UnsafeExt.Write(&head, &bufferLength, MaxCompressedSize, (ushort) tmpPkg.m_Nodes.Count);
                foreach(var kv in tmpPkg.m_Nodes) {
                    UnsafeExt.Write(&head, &bufferLength, MaxCompressedSize, kv.Key);

                    string text = kv.Value;

                    if (collapseConsts) {
                        constParser.Parse(ref constTagString, text, tmpPkg);
                        if (constTagString.EventCount == 0) {
                            string newText = constTagString.RichText;
                            if (newText != text) {
                                if (isHumanControlling) {
                                    string msg = "Collapsed text for " + kv.Key.ToDebugString() + ":\nOriginal: '" + text + "'\nNew: '" + newText + "'";
                                    Debug.Log(msg);
                                }
                                text = newText;
                                collapsedCount++;
                            }
                        }
                    }
                    UnsafeExt.WriteString(&head, &bufferLength, MaxCompressedSize, text);
                    MarkIdWithEvent(tmpPkg, kv.Key, text);
                }

                Log.Msg("{0} nodes collapsed", collapsedCount);

                int eventTextsRemoved = originalEventNodeCount - tmpPkg.m_IdsWithEvents.Count;

                Log.Msg("{0} node with events in package (removed {1})", tmpPkg.m_IdsWithEvents.Count, eventTextsRemoved);

                UnsafeExt.Write(&head, &bufferLength, MaxCompressedSize, (ushort) tmpPkg.m_IdsWithEvents.Count);
                foreach(var v in tmpPkg.m_IdsWithEvents) {
                    UnsafeExt.Write(&head, &bufferLength, MaxCompressedSize, v);
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