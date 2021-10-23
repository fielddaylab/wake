using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Debugger;
using BeauUtil.Editor;
using BeauUtil.IO;
using BeauUtil.Tags;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Aqua.Editor {
    public class LocEditor : ScriptableObject {
        #region Instance

        static private LocEditor s_Instance;

        public const string EditorDatabasePath = "Assets/Editor/LocDatabase.asset";
        public const string EditorDatabaseExportPath = "Assets/_Content/Text/english.bytes";

        static private LocEditor GetInstance() {
            if (!s_Instance) {
                s_Instance = AssetDatabase.LoadAssetAtPath<LocEditor>(EditorDatabasePath);
                if (!s_Instance) {
                    s_Instance = ScriptableObject.CreateInstance<LocEditor>();
                    AssetDatabase.CreateAsset(s_Instance, EditorDatabasePath);
                    AssetDatabase.SaveAssets();
                }
            }

            return s_Instance;
        }

        #endregion // Instance

        #region Types

        [Serializable]
        private struct BasePathHeader {
            public string Path;
            public int Start;

            public BasePathHeader(string path, int start) {
                Path = path;
                Start = start;
            }
        }

        [Serializable]
        private class PackageRecord : IDataBlockPackage<TextRecord> {
            public string Name;
            [HideInInspector] public string FilePath;

            public LocPackage Asset;
            [HideInInspector] public uint LastLine;

            public List<TextRecord> Records = new List<TextRecord>();
            [HideInInspector] public string BasePath = string.Empty;
            public List<BasePathHeader> AllBasePaths = new List<BasePathHeader>();

            [HideInInspector] public bool NeedsExport;

            public PackageRecord(string inName) {
                Name = inName;
            }

            public int Count { get { return Records.Count; } }

            public IEnumerator<TextRecord> GetEnumerator() {
                return Records.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }

            [BlockMeta("basePath")]
            private void SetBasePath(string path) {
                BasePath = path;
                AllBasePaths.Add(new BasePathHeader(path, Records.Count));
            }
        }

        [Serializable]
        private class TextRecord : IDataBlock {
            public string Id;
            [Multiline][BlockContent] public string Content = null;

            [NonSerialized] public PackageRecord Parent;

            public TextRecord(string inId) {
                Id = inId;
            }
        }

        private class PackageGenerator : AbstractBlockGenerator<TextRecord, PackageRecord> {
            static public readonly PackageGenerator Instance = new PackageGenerator();

            public override PackageRecord CreatePackage(string inFileName) {
                return new PackageRecord(inFileName);
            }

            public override void OnStart(IBlockParserUtil inUtil, PackageRecord inPackage) {
                base.OnStart(inUtil, inPackage);
                inPackage.Records.Clear();
            }

            public override bool TryCreateBlock(IBlockParserUtil inUtil, PackageRecord inPackage, TagData inId, out TextRecord outBlock) {
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

            public override void OnEnd(IBlockParserUtil inUtil, PackageRecord inPackage, bool inbError) {
                inPackage.LastLine = inUtil.Position.LineNumber;
                base.OnEnd(inUtil, inPackage, inbError);
            }
        }

        #endregion // Types

        [SerializeField] private List<PackageRecord> m_PackageRecords = new List<PackageRecord>();

        private Dictionary<StringHash32, TextRecord> m_TextMap = new Dictionary<StringHash32, TextRecord>();
        [NonSerialized] private GenericMenu m_OpenPackageFileMenu;
        [NonSerialized] private bool m_FullyInitialized = false;

        #region Constructing Records

        [MenuItem("Aqualab/Localization/Force Rebuild Database")]
        static private void ForceRebuildFromMenu() {
            var instance = GetInstance();
            instance.ReloadPackages();
        }

        [MenuItem("Aqualab/Localization/Export Compressed Database")]
        static private void ExportLocDatabase() {
            var instance = GetInstance();
            instance.ReloadPackages();

            using(var writer = new BinaryWriter(File.Open(EditorDatabaseExportPath, FileMode.Create))) {
                foreach (var text in instance.m_TextMap) {
                    writer.Write(text.Key.HashValue);
                    writer.Write(text.Value.Content ?? string.Empty);
                }
            }
        }

        [MenuItem("Aqualab/Localization/Write Changes")]
        static private void WriteAnyChanges() {
            bool bChanges = false;
            foreach (var record in GetInstance().m_PackageRecords) {
                bChanges |= ReexportPackage(record, false);
            }
            if (!bChanges) {
                Log.Msg("[LocEditor] No changes to export");
            }
        }

        private void ReloadPackages() {
            Debug.LogFormat("[LocEditor] Rebuilding loc database...");

            m_PackageRecords.Clear();
            m_TextMap.Clear();
            m_FullyInitialized = false;

            LocPackage[] allPackages = AssetDBUtils.FindAssets<LocPackage>();
            try {
                LocPackage package;
                int packageCount = allPackages.Length;
                for (int i = 0; i < packageCount; i++) {
                    package = allPackages[i];
                    string assetPath = AssetDatabase.GetAssetPath(package);
                    string fileContents = File.ReadAllText(assetPath);

                    Debug.LogFormat("[LocEditor] Importing {0}...", assetPath);
                    EditorUtility.DisplayProgressBar("Updating Loc Database", string.Format("Importing {0}/{1}: {2}", i + 1, packageCount, assetPath), (float)i + 1 / packageCount);
                    PackageRecord record = BlockParser.Parse(package.name, fileContents, Parsing.Block, PackageGenerator.Instance);
                    record.FilePath = assetPath;
                    record.Asset = package;
                    m_PackageRecords.Add(record);
                }
            } finally {
                EnsureFullInitialize();
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.SetDirty(this);
        }

        #endregion // Constructing Records

        #region Map

        private void EnsureFullInitialize() {
            if (m_FullyInitialized)
                return;

            m_TextMap.Clear();
            m_OpenPackageFileMenu = new GenericMenu();

            m_OpenPackageFileMenu.AddDisabledItem(new GUIContent("Open File"));
            m_OpenPackageFileMenu.AddSeparator(string.Empty);

            foreach (var package in m_PackageRecords) {
                foreach (var text in package.Records) {
                    if (m_TextMap.ContainsKey(text.Id)) {
                        Log.Error("[LocEditor] Duplicate text id '{0}'", text.Id);
                    } else {
                        m_TextMap.Add(text.Id, text);
                        text.Parent = package;
                    }
                }

                AddOpenPackageShortcut(package);
            }

            m_FullyInitialized = true;
        }

        private void AddOpenPackageShortcut(PackageRecord inRecord) {
            m_OpenPackageFileMenu.AddItem(new GUIContent(inRecord.Name), false, () => OpenPackage(inRecord));
        }

        static private void OpenPackage(PackageRecord inRecord) {
            AssetDatabase.OpenAsset(inRecord.Asset, (int)inRecord.LastLine);
        }

        static private bool ReexportPackage(PackageRecord inPackage, bool inbForce) {
            if (!inbForce && !inPackage.NeedsExport) {
                return false;
            }

            using(var writer = new StreamWriter(File.Open(inPackage.FilePath, FileMode.Create))) {
                int recordIdx = 0, pathIdx = 0;
                int totalRecordCount = inPackage.Records.Count;
                int totalPathCount = inPackage.AllBasePaths.Count;
                BasePathHeader nextBasePath = totalPathCount > 0 ? inPackage.AllBasePaths[0] : default;
                TextRecord currentRecord;
                string currentBasePath = string.Empty;

                while (recordIdx < totalRecordCount) {
                    if (pathIdx < totalPathCount && recordIdx >= nextBasePath.Start) {
                        writer.Write("# basePath ");
                        writer.Write(nextBasePath.Path);
                        writer.Write("\n\n");
                        currentBasePath = nextBasePath.Path;
                        pathIdx++;
                        nextBasePath = pathIdx < totalPathCount ? inPackage.AllBasePaths[pathIdx] : default;
                    }

                    currentRecord = inPackage.Records[recordIdx];
                    writer.Write(":: ");

                    string recordId = currentRecord.Id;
                    if (!string.IsNullOrEmpty(currentBasePath)) {
                        recordId = recordId.Substring(currentBasePath.Length + 1);
                    }

                    writer.Write(recordId);
                    writer.Write('\n');
                    writer.Write(currentRecord.Content);
                    writer.Write("\n\n");
                    recordIdx++;
                }
            }

            inPackage.NeedsExport = false;
            LockImport = true;
            AssetDatabase.ImportAsset(inPackage.FilePath, ImportAssetOptions.ForceUpdate);
            LockImport = false;
            Log.Msg("[LocEditor] Re-exported '{0}' to '{1}' with new changes", inPackage.Name, inPackage.FilePath);
            return true;
        }

        static private int CompareByKey(TextRecord a, TextRecord b) {
            return string.Compare(a.Id, b.Id);
        }

        #endregion // Map

        #region Initialization

        [InitializeOnLoadMethod]
        static private void Initialize() {
            if (InternalEditorUtility.inBatchMode || !InternalEditorUtility.isHumanControllingUs)
                return;

            var instance = GetInstance();
            if (instance.m_PackageRecords.Count == 0)
                instance.ReloadPackages();
        }

        #endregion // Initialization

        #region Statics

        static public bool TryLookup(string inKey, out string outText) {
            var instance = GetInstance();
            instance.EnsureFullInitialize();
            TextRecord text;
            if (instance.m_TextMap.TryGetValue(inKey, out text)) {
                outText = text.Content;
                return true;
            } else {
                outText = null;
                return false;
            }
        }

        static public int Search(string inSearch, ICollection<string> outResults) {
            var instance = GetInstance();
            instance.EnsureFullInitialize();

            int count = 0;
            foreach (var record in instance.m_TextMap.Values) {
                if (record.Id.Contains(inSearch)) {
                    outResults.Add(record.Id);
                    count++;
                }
            }

            return count;
        }

        static public void AttemptOpenFile(string inKey) {
            var instance = GetInstance();
            instance.EnsureFullInitialize();

            GUIUtility.systemCopyBuffer = inKey;
            instance.m_OpenPackageFileMenu.ShowAsContext();
        }

        static public void OpenFile(string inKey) {
            var instance = GetInstance();
            instance.EnsureFullInitialize();

            TextRecord text;
            if (instance.m_TextMap.TryGetValue(inKey, out text)) {
                OpenPackage(text.Parent);
            }
        }

        static public void TrySet(string inKey, string inText) {
            var instance = GetInstance();
            instance.EnsureFullInitialize();

            TextRecord existingRecord;
            if (instance.m_TextMap.TryGetValue(inKey, out existingRecord)) {
                if (existingRecord.Content != inText) {
                    existingRecord.Content = inText;
                    existingRecord.Parent.NeedsExport = true;
                    EditorUtility.SetDirty(instance);
                }
            } else {
                PackageRecord blankPackage = null;
                TextRecord newRecord = null;
                bool bInserted = false;
                foreach (var package in instance.m_PackageRecords) {
                    if (package.AllBasePaths.Count == 0) {
                        blankPackage = package;
                        break;
                    } else {
                        BasePathHeader basePath;
                        for (int i = 0, totalPathCount = package.AllBasePaths.Count; i < totalPathCount; i++) {
                            basePath = package.AllBasePaths[i];
                            if (inKey.StartsWith(basePath.Path, StringComparison.InvariantCulture)) {
                                newRecord = InsertTextRecord(inKey, inText, package, i);
                                EditorUtility.SetDirty(instance);
                                bInserted = true;
                                break;
                            }
                        }
                    }
                }

                if (!bInserted && blankPackage != null) {
                    newRecord = InsertTextRecord(inKey, inText, blankPackage, 0);
                    EditorUtility.SetDirty(instance);
                    bInserted = true;
                }

                if (!bInserted) {
                    Log.Error("[LocEditor] No valid package located to insert text with id '{0}'", inKey);
                } else {
                    Log.Msg("[LocEditor] Successfully added text with key '{0}' to package '{1}'", inKey, newRecord.Parent.Name);
                    instance.m_TextMap[inKey] = newRecord;
                }
            }
        }

        static private TextRecord InsertTextRecord(string inKey, string inText, PackageRecord inPackage, int inSectionIdx) {
            TextRecord textRecord = new TextRecord(inKey);
            textRecord.Content = inText;
            textRecord.Parent = inPackage;

            if (inSectionIdx >= inPackage.AllBasePaths.Count - 1) {
                inPackage.Records.Add(textRecord);
            } else {
                BasePathHeader nextRecord = inPackage.AllBasePaths[inSectionIdx + 1];
                int insertIdx = nextRecord.Start;

                inPackage.Records.Insert(insertIdx, textRecord);

                for (int i = inSectionIdx + 1; i < inPackage.AllBasePaths.Count; i++) {
                    BasePathHeader revisedHeader = inPackage.AllBasePaths[i];
                    revisedHeader.Start++;
                    inPackage.AllBasePaths[i] = revisedHeader;
                }
            }

            inPackage.NeedsExport = true;
            return textRecord;
        }

        #endregion // Statics

        #region Asset Postprocessor

        static private bool LockImport = false;

        [UnityEditor.InitializeOnLoadMethod]
        static private void EditorInitialize() {
            UnityEditor.EditorApplication.playModeStateChanged += PlayModeStateChange;
        }

        static private void PlayModeStateChange(UnityEditor.PlayModeStateChange stateChange) {
            if (stateChange != UnityEditor.PlayModeStateChange.ExitingEditMode) {
                return;
            }

            if (EditorUtility.IsDirty(GetInstance())) {
                WriteAnyChanges();
            }
        }

        private class AssetSaveHook : UnityEditor.AssetModificationProcessor {
            static private string[] OnWillSaveAssets(string[] paths) {
                if (!LockImport) {
                    foreach (var path in paths) {
                        if (path == EditorDatabasePath) {
                            WriteAnyChanges();
                            break;
                        }
                    }
                }
                return paths;
            }
        }

        private class AssetImportHook : AssetPostprocessor {
            static private void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
                if (LockImport || Application.isPlaying || InternalEditorUtility.inBatchMode || !InternalEditorUtility.isHumanControllingUs)
                    return;

                if (AnyAreLocPackage(importedAssets) || AnyAreLocPackage(deletedAssets) || AnyAreLocPackage(movedAssets) || AnyAreLocPackage(movedFromAssetPaths)) {
                    EditorApplication.delayCall += () => GetInstance().ReloadPackages();
                }
            }

            static private bool AnyAreLocPackage(string[] assetNames) {
                if (assetNames == null || assetNames.Length == 0)
                    return false;

                foreach (var filePath in assetNames) {
                    if (filePath.EndsWith("LocEditor.cs"))
                        return true;

                    if (filePath.EndsWith(".aqloc")) {
                        StringSlice truncated = filePath.Substring(0, filePath.Length - 6);
                        if (truncated.Length > 3)
                            return truncated[truncated.Length - 3] != '.';
                        return true;
                    }
                }

                return false;
            }
        }

        #endregion // Asset Postprocessor
    }
}