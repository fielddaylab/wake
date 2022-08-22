using UnityEngine;
using BeauUtil;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using BeauUtil.Debugger;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif // UNITY_EDITOR

namespace Aqua
{
    static public class LightUtils
    {
        public struct SceneSettings
        {
            public bool fog;
            public float fogStartDistance;
            public float fogEndDistance;
            public FogMode fogMode;
            public Color fogColor;
            public float fogDensity;
            public AmbientMode ambientMode;
            public float ambientIntensity;
            public Color ambientLight;
            public LightmapData[] lightmaps;
            public LightmapsMode lightmapsMode;
            public LightProbes lightProbes;

            public void Read()
            {
                fog = RenderSettings.fog;
                fogStartDistance = RenderSettings.fogStartDistance;
                fogEndDistance = RenderSettings.fogEndDistance;
                fogMode = RenderSettings.fogMode;
                fogColor = RenderSettings.fogColor;
                fogDensity = RenderSettings.fogDensity;
                ambientMode = RenderSettings.ambientMode;
                ambientIntensity = RenderSettings.ambientIntensity;
                ambientLight = RenderSettings.ambientLight;
                lightmaps = ArrayUtils.CreateFrom(LightmapSettings.lightmaps);
                lightmapsMode = LightmapSettings.lightmapsMode;
                lightProbes = LightmapSettings.lightProbes;
            }

            public void Write()
            {
                RenderSettings.fog = fog;
                RenderSettings.fogStartDistance = fogStartDistance;
                RenderSettings.fogEndDistance = fogEndDistance;
                RenderSettings.fogMode = fogMode;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogDensity = fogDensity;
                RenderSettings.ambientMode = ambientMode;
                RenderSettings.ambientIntensity = ambientIntensity;
                RenderSettings.ambientLight = ambientLight;
                LightmapSettings.lightmaps = lightmaps;
                LightmapSettings.lightmapsMode = lightmapsMode;
                LightmapSettings.lightProbes = lightProbes;
            }
        }

        static public void CopySettings(SceneBinding inSource, SceneBinding inTarget)
        {
            SceneBinding currentActive = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(inSource);
            SceneSettings settings = default;
            settings.Read();
            SceneManager.SetActiveScene(inTarget);
            settings.Write();
            SceneManager.SetActiveScene(currentActive);
        }

        #if UNITY_EDITOR

        static private SceneSettings? s_CopyBuffer;

        [MenuItem("Aqualab/Lighting/Copy Current Settings")]
        static private void CopyCurrentSettings() {
            SceneSettings settings = default(SceneSettings);
            settings.Read();
            s_CopyBuffer = settings;
            Log.Msg("[LightUtils] Copied lighting settings from current scene '{0}'", EditorSceneManager.GetActiveScene().path);
        }

        [MenuItem("Aqualab/Lighting/Paste Current Settings", false)]
        static private void PasteCurrentSettings() {
            s_CopyBuffer.Value.Write();
            Log.Msg("[LightUtils] Pasted lighting settings into current scene '{0}'", EditorSceneManager.GetActiveScene().path);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        [MenuItem("Aqualab/Lighting/Paste Current Settings", true)]
        static private bool PasteCurrentSettings_Validate() {
            return s_CopyBuffer.HasValue;
        }

        #endif // UNITY_EDITOR
    }
}