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
    public class LayoutPrefabPackage : ScriptableObject {
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
                        GameObject instantiated = GameObject.Instantiate(prefab, root.transform);
                        instantiated.name = prefab.name;
                        try {
                            byte[] compressed = instantiated.GetComponent<CompressiblePrefab>().Compress(compressor, CompressedRectTransformBounds.Default);
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

        [CustomEditor(typeof(LayoutPrefabPackage), true)]
        private class Inspector : UnityEditor.Editor
        {
            [NonSerialized] private GUIStyle m_Style;

            protected void OnEnable()
            {
                GetType().GetProperty("alwaysAllowExpansion", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(this, true);
            }

            public override void OnInspectorGUI()
            {
                if (m_Style == null)
                {
                    m_Style = new GUIStyle("ScriptText");
                }

                LayoutPrefabPackage prefabPackage = (LayoutPrefabPackage) target;

                if (GUILayout.Button("Build")) {
                    prefabPackage.Bake();
                }

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
            }
        }

        #endif // UNITY_EDITOR

        #endregion // Editor
    }
}