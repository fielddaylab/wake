using UnityEngine;
using ScriptableBake;
using EasyAssetStreaming;
using BeauUtil;
using System.Collections.Generic;
using UnityEditor;
using BeauUtil.Debugger;
using UnityEditor.SceneManagement;
using System.Text;

namespace Aqua.Editor
{
    static public class SceneManifestUtility {
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
    }
}