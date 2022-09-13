using UnityEngine;
using ScriptableBake;
using EasyAssetStreaming;
using BeauUtil;
using System.Collections.Generic;
using UnityEditor;
using BeauUtil.Debugger;
using UnityEditor.SceneManagement;
using System.Text;
using BeauData;

namespace Aqua.Editor
{
    static public class SceneManifestUtility {
        public const string PreloadManifestPath = "Assets/_Content/Maps/PreloadManifest_Scenes.json";

        static public SceneManifestBuilder GetManifestForCurrentScene(out SceneBinding scene) {
            return GetManifestForScene(scene = EditorSceneManager.GetActiveScene());
        }

        static public SceneManifestBuilder GetManifestForScene(SceneBinding scene) {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            GameObject bootstrap = GameObject.Find("Bootstrap");
            if (bootstrap != null) {
                Undo.DestroyObjectImmediate(bootstrap);
            }

            SceneManifestBuilder builder = new SceneManifestBuilder();
            List<ISceneManifestElement> manifestElements = new List<ISceneManifestElement>(32);
            List<IStreamingComponent> streamingComponents = new List<IStreamingComponent>(32);

            scene.Scene.GetAllComponents<ISceneManifestElement>(true, manifestElements);
            scene.Scene.GetAllComponents<IStreamingComponent>(true, streamingComponents);

            foreach(var manifestElement in manifestElements) {
                manifestElement.BuildManifest(builder);
            }

            foreach(var streaming in streamingComponents) {
                BuildStreaming(streaming, builder);
            }

            Undo.RevertAllInCurrentGroup();

            return builder;
        }

        static private void BuildStreaming(IStreamingComponent streamingComponent, SceneManifestBuilder manifest) {
            string url = streamingComponent.Path;
            if (!string.IsNullOrEmpty(url)) {
                manifest.Paths.Add(url);
            }
        }
    
        [MenuItem("Aqualab/DEBUG/Generate Scene Manifest")]
        static private void BuildSceneManifest() {
            var manifestBuilder = GetManifestForCurrentScene(out SceneBinding scene);
            StringBuilder sb = new StringBuilder(1024);
            sb.Append("[SceneManifestUtility] Gathered manifest elements for scene ").Append(scene.Name);
            foreach(var path in manifestBuilder.Paths) {
                sb.Append("\n - path: ").Append(path);
            }
            foreach(var asset in manifestBuilder.Assets) {
                sb.Append("\n - asset: ").Append(asset.name).Append(" (").Append(asset.GetType().Name).Append(")");
            }
            Log.Msg(sb.Flush());
        }

        [MenuItem("Aqualab/Collect Preload List")]
        static internal void BuildPreloadManifest()
        {
            string currentPath = EditorSceneManager.GetActiveScene().path;
            List<SceneBinding> allScenes = new List<SceneBinding>(SceneHelper.AllBuildScenes(true));
            PreloadManifest package = new PreloadManifest();
            List<PreloadGroup> groups = new List<PreloadGroup>();
            try
            {
                Log.Msg("[SceneManifestUtility] Building scene manifest");
                for(int i = 0; i < allScenes.Count; i++)
                {
                    SceneBinding scene = allScenes[i];
                    EditorUtility.DisplayProgressBar("Building scene manifest", string.Format("{0} ({1}/{2})", scene.Name, i + 1, allScenes.Count), (float) i / allScenes.Count);
                    Log.Msg("[SceneManifestUtility] Loading '{0}'", scene.Path);
                    EditorSceneManager.OpenScene(scene.Path, OpenSceneMode.Single);
                    SceneProcessor.DEBUGBakeScene();
                    var manifestForScene = GetManifestForCurrentScene(out var _);
                    if (manifestForScene.Paths.Count > 0) {
                        var groupForScene = new PreloadGroup();
                        groupForScene.Id = "Scene/" + scene.Name;
                        groupForScene.Paths = new string[manifestForScene.Paths.Count];
                        manifestForScene.Paths.CopyTo(groupForScene.Paths);
                        groups.Add(groupForScene);
                    }
                }

                package.Groups = groups.ToArray();
                Serializer.WriteFile(package, PreloadManifestPath, OutputOptions.PrettyPrint, Serializer.Format.JSON);
                if (!UnityEditorInternal.InternalEditorUtility.inBatchMode && !BuildPipeline.isBuildingPlayer) {
                    EditorUtility.OpenWithDefaultApp(PreloadManifestPath);
                }
                AssetDatabase.ImportAsset(PreloadManifestPath);
            }
            finally
            {
                if (!UnityEditorInternal.InternalEditorUtility.inBatchMode) {
                    EditorSceneManager.OpenScene(currentPath, OpenSceneMode.Single);
                }
                EditorUtility.ClearProgressBar();
            }
        }
    }
}