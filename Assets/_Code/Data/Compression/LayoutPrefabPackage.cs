using System;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;
using System.IO;
using System.Reflection;
using BeauUtil.Debugger;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua.Compression {

    [CreateAssetMenu(menuName = "Aqualab/Layout Prefab Package")]
    public class LayoutPrefabPackage : ScriptableObject, IEditorOnlyData {
        #region Type
        
        [Serializable]
        private struct PrefabEntry {
            public SerializedHash32 Id;
            public uint Offset;
            public uint Length;
        }

        #endregion // Type

        #region Data

        [Header("Data")]
        [SerializeField] private PackageBank m_Bank = new PackageBank();
        [SerializeField] private PrefabEntry[] m_TOC = new PrefabEntry[0];
        [SerializeField] private byte[] m_CompressedData = new byte[0];
        [SerializeField] private bool m_AllowLZCompression = false;

        #endregion // Data

        /// <summary>
        /// Decompresses a prefab.
        /// </summary>
        public GameObject Decompress(StringHash32 id, PrefabDecompressor decompressor) {
            decompressor.RectTransformBounds = CompressedRectTransformBounds.Default;
            foreach(var entry in m_TOC) {
                if (entry.Id == id) {
                    return CompressiblePrefab.Decompress(m_CompressedData, (int) entry.Offset, (int) entry.Length, m_Bank, decompressor);
                }
            }
            Log.Warn("No prefab for entry '{0}'", id);
            return null;
        }

        #region Bake

        #if UNITY_EDITOR

        [ContextMenu("Compress")]
        private void Bake() {
            GameObject[] allPrefabs = ValidationUtils.FindAllAssets<GameObject>(PrefabPredicate, Path.GetDirectoryName(AssetDatabase.GetAssetPath(this)));
            PackageBuilder compressor = new PackageBuilder();
            List<PrefabEntry> toc = new List<PrefabEntry>();
            List<byte> allData = new List<byte>(4096);
            GameObject root = new GameObject("temp");
            root.SetActive(false);

            try {
                using(Profiling.Time("compressing prefabs")) {
                    int idx = 0;
                    foreach(var prefab in allPrefabs) {
                        EditorUtility.DisplayProgressBar("Compressing Prefabs...", string.Format("Compressing '{0}' ({1}/{2})", prefab.name, idx + 1, allPrefabs.Length), (idx + 1) / (float) allPrefabs.Length);
                        idx++;
                        GameObject instantiated = GameObject.Instantiate(prefab, root.transform, false);
                        instantiated.name = prefab.name;
                        TRS trs = new TRS(prefab.transform);
                        trs.CopyTo(instantiated.transform);
                        try {
                            Log.Msg("[LayoutPrefabPackage] Encoding '{0}'", prefab.name);
                            byte[] compressed = instantiated.GetComponent<CompressiblePrefab>().Compress(compressor, CompressedRectTransformBounds.Default);
                            if (m_AllowLZCompression && compressed.Length >= 0x80) {
                                Log.Msg("[LayoutPrefabPackage] Compressing '{0}'...", prefab.name);
                                byte[] uncompressed = (byte[]) compressed.Clone();
                                compressed = UnsafeExt.Compress(compressed);
                                Log.Msg("[LayoutPrefabPackage] Compression Ratio: {0}", (float) uncompressed.Length / compressed.Length);
                                byte[] decompressed;
                                if (!UnsafeExt.Decompress(compressed, out decompressed)) {
                                    Log.Error("[LayoutPrefabPackage] Compressed data unable to be decompressed!");
                                } else if (!ArrayUtils.ContentEquals(uncompressed, decompressed)) {
                                    Log.Error("[LayoutPrefabPackage] Compressed data, when uncompressed, is not identical");
                                }
                            }
                            PrefabEntry entry = new PrefabEntry() {
                                Id = prefab.name,
                                Offset = (uint) allData.Count,
                                Length = (uint) compressed.Length
                            };
                            toc.Add(entry);
                            allData.AddRange(compressed);
                        } finally {
                            GameObject.DestroyImmediate(instantiated);
                        }
                    }
                    m_Bank = new PackageBank(compressor);
                    m_TOC = toc.ToArray();
                    m_CompressedData = allData.ToArray();
                }

                EditorUtility.SetDirty(this);
            } finally {
                EditorUtility.ClearProgressBar();
                GameObject.DestroyImmediate(root);
            }
        }

        static private Predicate<GameObject> PrefabPredicate = (go) => {
            return go.GetComponent<CompressiblePrefab>();
        };

        #endif // UNITY_EDITOR

        #endregion // Bake

        #region Editor

        #if UNITY_EDITOR

        void IEditorOnlyData.ClearEditorOnlyData() {
            for(int i = 0; i < m_TOC.Length; i++) {
                ValidationUtils.StripDebugInfo(ref m_TOC[i].Id);
            }
        }

        [CustomEditor(typeof(LayoutPrefabPackage), true)]
        private class Inspector : UnityEditor.Editor
        {
            [NonSerialized] private GUIStyle m_Style;
            [NonSerialized] private SerializedProperty m_AllowLZCompression;

            protected void OnEnable()
            {
                GetType().GetProperty("alwaysAllowExpansion", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(this, true);
                m_AllowLZCompression = serializedObject.FindProperty("m_AllowLZCompression");
            }

            public override void OnInspectorGUI()
            {
                if (m_Style == null)
                {
                    m_Style = new GUIStyle("ScriptText");
                }

                serializedObject.UpdateIfRequiredOrScript();

                LayoutPrefabPackage prefabPackage = (LayoutPrefabPackage) target;

                if (GUILayout.Button("Build")) {
                    prefabPackage.Bake();
                }
                EditorGUILayout.PropertyField(m_AllowLZCompression);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);

                EditorGUILayout.LabelField("Total Size", EditorUtility.FormatBytes(prefabPackage.m_CompressedData.Length));
                EditorGUILayout.LabelField("Prefabs", prefabPackage.m_TOC.Length.ToString());
                if (prefabPackage.m_TOC.Length > 0) {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        foreach(var entry in prefabPackage.m_TOC) {
                            EditorGUILayout.LabelField(entry.Id.Source(), EditorUtility.FormatBytes(entry.Length));
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.LabelField("Strings", prefabPackage.m_Bank.StringBank.Length.ToString());
                if (prefabPackage.m_Bank.StringBank.Length > 0) {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        using(new EditorGUI.DisabledScope(true)) {
                            foreach(var entry in prefabPackage.m_Bank.StringBank) {
                                EditorGUILayout.TextField(entry);
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.LabelField("References", prefabPackage.m_Bank.AssetBank.Length.ToString());
                if (prefabPackage.m_Bank.AssetBank.Length > 0) {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        using(new EditorGUI.DisabledScope(true)) {
                            foreach(var entry in prefabPackage.m_Bank.AssetBank) {
                                EditorGUILayout.ObjectField(entry, typeof(UnityEngine.Object), false);
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                }

                serializedObject.ApplyModifiedProperties();
            }
        }

        #endif // UNITY_EDITOR

        #endregion // Editor
    }
}