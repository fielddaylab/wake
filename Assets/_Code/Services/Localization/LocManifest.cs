using System.Collections;
using System.IO;
using BeauData;
using BeauUtil;
using ScriptableBake;
using UnityEngine;
using System;

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
        [HideInInspector] public byte[] Compressed = Array.Empty<byte>();

        #endregion // Inspector

        #if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context)
        {
            Compressed = LocPackage.Compress(Packages);
            if (UnityEditorInternal.InternalEditorUtility.isHumanControllingUs) {
                Directory.CreateDirectory("Temp/LanguageExport");
                File.WriteAllBytes("Temp/LanguageExport/" + name + ".bin", Compressed);
            }
            return false;
        }

        void IEditorOnlyData.ClearEditorOnlyData()
        {
            #if !PRESERVE_DEBUG_SYMBOLS && !DEVELOPMENT
            Packages = null;
            #endif
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
                    size += manifest.Compressed.Length;
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Compressed Size", EditorUtility.FormatBytes(size));
            }
        }

        #endif // UNITY_EDITOR
    }
}