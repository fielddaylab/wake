using System;
using UnityEngine;
using UnityEditor;
using BeauUtil.IO;
using BeauUtil.Blocks;
using System.Collections.Generic;
using System.Collections;
using BeauUtil.Tags;
using BeauUtil;
using BeauUtil.Editor;
using System.IO;
using UnityEditorInternal;

namespace Aqua.Editor
{
    public class LocEditor : ScriptableObject
    {
        #region Instance

        static private LocEditor s_Instance;

        public const string EditorDatabasePath = "Assets/Editor/LocDatabase.asset";

        static private LocEditor GetInstance()
        {
            if (!s_Instance)
            {
                s_Instance = AssetDatabase.LoadAssetAtPath<LocEditor>(EditorDatabasePath);
                if (!s_Instance)
                {
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
        private class PackageRecord : IDataBlockPackage<TextRecord>
        {
            public string Name;

            public LocPackage Asset;
            public uint LastLine;

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
        }

        [Serializable]
        private class TextRecord : IDataBlock
        {
            public string Id;
            [BlockContent] public string Content;

            [NonSerialized] public PackageRecord Parent;

            public TextRecord(string inId)
            {
                Id = inId;
            }
        }

        private class PackageGenerator : AbstractBlockGenerator<TextRecord, PackageRecord>
        {
            static public readonly PackageGenerator Instance = new PackageGenerator();

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

            public override void OnEnd(IBlockParserUtil inUtil, PackageRecord inPackage, bool inbError)
            {
                inPackage.LastLine = inUtil.Position.LineNumber;
                base.OnEnd(inUtil, inPackage, inbError);
            }
        }

        #endregion // Types

        [SerializeField] private List<PackageRecord> m_PackageRecords = new List<PackageRecord>();

        private Dictionary<StringHash32, TextRecord> m_TextMap = new Dictionary<StringHash32, TextRecord>();
        [NonSerialized] private NamedItemList<string> m_TextSelectableList = new NamedItemList<string>();
        [NonSerialized] private GenericMenu m_OpenPackageFileMenu;
        [NonSerialized] private bool m_FullyInitialized = false;

        #region Constructing Records

        [ContextMenu("Force Reload")]
        private void ReloadPackages()
        {
            Debug.LogFormat("[LocEditor] Rebuilding loc database...");

            m_PackageRecords.Clear();
            m_TextMap.Clear();
            m_FullyInitialized = false;
            
            LocPackage[] allPackages = AssetDBUtils.FindAssets<LocPackage>();
            try
            {
                LocPackage package;
                int packageCount = allPackages.Length;
                for(int i = 0; i < packageCount; i++)
                {
                    package = allPackages[i];
                    string assetPath = AssetDatabase.GetAssetPath(package);
                    string fileContents = File.ReadAllText(assetPath);
                    
                    Debug.LogFormat("[LocEditor] Importing {0}...", assetPath);
                    EditorUtility.DisplayProgressBar("Updating Loc Database", string.Format("Importing {0}/{1}: {2}", i + 1, packageCount, assetPath), (float) i + 1 / packageCount);
                    PackageRecord record = BlockParser.Parse(package.name, fileContents, Parsing.Block, PackageGenerator.Instance);
                    record.Asset = package;
                    m_PackageRecords.Add(record);
                }
            }
            finally
            {
                EnsureFullInitialize();
                EditorUtility.ClearProgressBar();
            }
        }

        #endregion // Constructing Records

        #region Map

        private void EnsureFullInitialize()
        {
            if (m_FullyInitialized)
                return;

            m_TextMap.Clear();
            m_TextSelectableList.Clear();
            m_TextSelectableList.Add("[empty]", string.Empty, -1);
            m_OpenPackageFileMenu = new GenericMenu();

            m_OpenPackageFileMenu.AddDisabledItem(new GUIContent("Open File"));
            m_OpenPackageFileMenu.AddSeparator(string.Empty);
            
            foreach(var package in m_PackageRecords)
            {
                foreach(var text in package.Records)
                {
                    if (m_TextMap.ContainsKey(text.Id))
                    {
                        Debug.LogErrorFormat("[LocEditor] Duplicate text id '{0}'", text.Id);
                    }
                    else
                    {
                        m_TextMap.Add(text.Id, text);
                        text.Parent = package;

                        string listKey = text.Id.Replace(".", "/");
                        m_TextSelectableList.Add(listKey, text.Id);
                    }
                }

                AddOpenPackageShortcut(package);
            }

            m_FullyInitialized = true;
        }

        private void AddOpenPackageShortcut(PackageRecord inRecord)
        {
            m_OpenPackageFileMenu.AddItem(new GUIContent(inRecord.Name), false, () => OpenPackage(inRecord));
        }

        static private void OpenPackage(PackageRecord inRecord)
        {
            AssetDatabase.OpenAsset(inRecord.Asset, (int) inRecord.LastLine);
        }

        #endregion // Map

        #region Initialization

        [InitializeOnLoadMethod]
        static private void Initialize()
        {
            if (InternalEditorUtility.inBatchMode || !InternalEditorUtility.isHumanControllingUs)
                return;
            
            var instance = GetInstance();
            instance.ReloadPackages();
        }

        #endregion // Initialization

        #region Statics

        static public bool TryLookup(string inKey, out string outText)
        {
            var instance = GetInstance();
            instance.EnsureFullInitialize();
            TextRecord text;
            if (instance.m_TextMap.TryGetValue(inKey, out text))
            {
                outText = text.Content;
                return true;
            }
            else
            {
                outText = null;
                return false;
            }
        }

        static public void AttemptOpenFile(string inKey)
        {
            var instance = GetInstance();
            GUIUtility.systemCopyBuffer = inKey;
            instance.m_OpenPackageFileMenu.ShowAsContext();
        }

        static public void OpenFile(string inKey)
        {
            var instance = GetInstance();
            TextRecord text;
            if (instance.m_TextMap.TryGetValue(inKey, out text))
            {
                OpenPackage(text.Parent);
            }
        }

        #endregion // Statics

        #region Asset Postprocessor

        private class AssetImportHook : AssetPostprocessor
        {
            static private void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                if (Application.isPlaying || InternalEditorUtility.inBatchMode || !InternalEditorUtility.isHumanControllingUs)
                    return;

                if (AnyAreLocPackage(importedAssets) || AnyAreLocPackage(deletedAssets) || AnyAreLocPackage(movedAssets) || AnyAreLocPackage(movedFromAssetPaths))
                {
                    EditorApplication.delayCall += () => GetInstance().ReloadPackages();
                }
            }

            static private bool AnyAreLocPackage(string[] assetNames)
            {
                if (assetNames == null || assetNames.Length == 0)
                    return false;
                
                foreach(var filePath in assetNames)
                {
                    if (filePath.EndsWith(".aqloc"))
                    {
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