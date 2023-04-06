using System.Collections;
using System.IO;
using BeauData;
using BeauUtil;
using ScriptableBake;
using UnityEngine;
using System;
using Aqua.Compression;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Localization Manifest")]
    public class LocManifest : ScriptableObject, IEditorOnlyData, IBaked
    {
        #region Inspector

        public FourCC LanguageId;
        public LocPackage[] Packages;
        public LayoutPrefabPackage JournalLayout;
        [HideInInspector] public byte[] Binary = Array.Empty<byte>();

        #endregion // Inspector

        #if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context)
        {
            if (Packages.Length > 0) {
                Binary = LocPackage.Compress(Packages);
                if (UnityEditorInternal.InternalEditorUtility.isHumanControllingUs) {
                    Directory.CreateDirectory("Temp/LanguageExport");
                    File.WriteAllBytes("Temp/LanguageExport/" + name + ".bin", Binary);
                }
                return true;
            } else {
                Binary = Array.Empty<byte>();
            }
            return true;
        }

        void IEditorOnlyData.ClearEditorOnlyData()
        {
            #if !PRESERVE_DEBUG_SYMBOLS && !DEVELOPMENT
            Packages = null;
            #endif // !PRESERVE_DEBUG_SYMBOLS && !DEVELOPMENT
        }

        [CustomEditor(typeof(LocManifest), true)]
        private class Inspector : UnityEditor.Editor
        {
            [NonSerialized] private GUIStyle m_Style;

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                long size = 0;
                foreach(LocManifest manifest in targets) {
                    size += manifest.Binary.Length;
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Binary Size", EditorUtility.FormatBytes(size));
            }
        }

        #endif // UNITY_EDITOR
    }
}