using BeauUtil;
using UnityEngine;
using System.Collections;
using BeauUtil.Debugger;
using Leaf;
using System.IO;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif // UNITY_EDITOR

namespace Aqua.Scripting
{
    [AddComponentMenu("Aqualab/Scripting/Script Loader")]
    public class ScriptLoader : MonoBehaviour, IScenePreloader, ISceneUnloadHandler, ISceneManifestElement
    {
        #region Inspector

        [SerializeField, Required] private LeafAsset[] m_Scripts = null;

        #endregion // Inspector

        public IEnumerator OnPreloadScene(SceneBinding inScene, object inContext)
        {
            for(int i = 0; i < m_Scripts.Length; ++i)
            {
                LeafAsset file = m_Scripts[i];
                Services.Script.LoadScript(file);
            }

            return null;
        }

        public void OnSceneUnload(SceneBinding inScene, object inContext)
        {
            for(int i = 0; i < m_Scripts.Length; ++i)
            {
                LeafAsset file = m_Scripts[i];
                Services.Script.UnloadScript(file);
            }
        }

        #if UNITY_EDITOR

        public void BuildManifest(SceneManifestBuilder builder) {
            foreach(var script in m_Scripts) {
                builder.Assets.Add(script);
            }
        }

        [CustomEditor(typeof(ScriptLoader), true), CanEditMultipleObjects]
        private class Inspector : UnityEditor.Editor {
            public override void OnInspectorGUI() {
                base.OnInspectorGUI();

                if (targets.Length > 1) {
                    return;
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Add New Script")) {
                    string scenePath = EditorSceneManager.GetActiveScene().path;
                    if (!string.IsNullOrEmpty(scenePath)) {
                        scenePath = Path.GetDirectoryName(scenePath);
                    }
                    string newPath = EditorUtility.SaveFilePanel("Select Leaf File Path", scenePath, "NewScript", "leaf");
                    if (!string.IsNullOrEmpty(newPath)) {
                        newPath = newPath.Replace('\\', '/');
                        File.WriteAllText(newPath, "# basePath");
                        string relativePath = Environment.CurrentDirectory.Replace('\\', '/');
                        newPath = newPath.Replace(relativePath, "").TrimStart('/');
                        AssetDatabase.ImportAsset(newPath, ImportAssetOptions.ForceSynchronousImport);
                        LeafAsset asset = AssetDatabase.LoadAssetAtPath<LeafAsset>(newPath);
                        if (asset != null) {
                            ScriptLoader loader = (ScriptLoader) target;
                            Undo.RecordObject(loader, "Adding new script");
                            ArrayUtils.Add(ref loader.m_Scripts, asset);
                            EditorUtility.SetDirty(loader);
                        }
                    }
                }
            }
        }

        #endif // UNITY_EDITOR
    }
}