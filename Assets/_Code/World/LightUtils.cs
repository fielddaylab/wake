using UnityEngine;
using BeauUtil;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

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
    }
}