using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aqua
{
    public interface ISceneSubsceneSelector
    {
        IEnumerable<SceneImportSettings> GetAdditionalScenesNames(SceneBinding inNew, object inContext);
    }

    public struct SceneImportSettings {
        public string ScenePath;
        public bool ImportLighting;
        public TRS? Transform;
        public Matrix4x4 TransformMatrix;

        public SceneImportSettings(SceneBinding scene, bool importLighting, TRS? transform) {
            ScenePath = scene.Path;
            ImportLighting = importLighting;
            Transform = transform;
            TransformMatrix = Transform?.Matrix ?? Matrix4x4.identity;
        }

        public SceneImportSettings(SceneReference sceneRef, bool importLighting, TRS? transform) {
            ScenePath = sceneRef.Path;
            ImportLighting = importLighting;
            Transform = transform;
            TransformMatrix = Transform?.Matrix ?? Matrix4x4.identity;
        }

        static public implicit operator SceneImportSettings(string sceneName) {
            return new SceneImportSettings(SceneHelper.FindSceneByName(sceneName, SceneCategories.AllBuild), false, null);
        }

        #if UNITY_EDITOR

        static public implicit operator SceneImportSettings(SubScene subScene) {
            return new SceneImportSettings(
                subScene.Scene,
                subScene.ImportLighting,
                subScene.Transform ? new TRS(subScene.Transform) : (TRS?) null);
        }

        #endif // UNITY_EDITOR

        static public void TransformRoot(GameObject root, in SceneImportSettings transform) {
            if (!transform.Transform.HasValue) {
                return;
            }

            TRS trs = new TRS(root.transform);
            trs.Position = transform.TransformMatrix.MultiplyPoint3x4(trs.Position);
            trs.Scale = transform.TransformMatrix.MultiplyVector(trs.Scale);
            trs.Rotation = transform.Transform.Value.Rotation * trs.Rotation;
            trs.CopyTo(root.transform);
        }
    }
}